using LanGeng.API.Data;
using LanGeng.API.Dtos;
using LanGeng.API.Entities;
using LanGeng.API.Enums;
using LanGeng.API.Helper;
using LanGeng.API.Mapping;
using LanGeng.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1;

namespace LanGeng.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly TokenService _tokenService;
        private readonly EmailService _emailService;
        private readonly SocialMediaDatabaseContext dbContext;

        public UserController(ILogger<UserController> logger, TokenService tokenService, EmailService emailService, SocialMediaDatabaseContext context)
        {
            _logger = logger;
            _tokenService = tokenService;
            _emailService = emailService;
            dbContext = context;
            dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        [Authorize]
        [HttpDelete()]
        public async Task<IResult> RequestDelete()
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                var token = _tokenService.GetToken(HttpContext);
                if (currentUser != null)
                {
                    var userVerifyToken = new UserVerificationToken
                    {
                        UserId = currentUser.Id,
                        VerificationType = VerificationTypeEnum.AccountDeletion,
                        Email = currentUser.Email,
                        Token = SlugHelper.Create(currentUser.Username)
                    };
                    dbContext.UserVerificationTokens.Add(userVerifyToken);
                    await dbContext.SaveChangesAsync();

                    var request = HttpContext.Request;
                    var verificationLink = $"{request.Scheme}://{request.Host}/api/auth/verifyemail?u={token}&t={userVerifyToken.Token}&s={(byte)userVerifyToken.VerificationType}";
                    await _emailService.SendAsync(
                        currentUser.Email,
                        "Email Verification",
                        $"Please verify your email by clicking the following link:\n\n<a href=\"{verificationLink}\">Verify</a>"
                    );

                    return Results.Ok(new ResponseData<ResponseUserDto>("Success", currentUser.ToResponseDto()));
                }
                else
                {
                    return Results.NotFound(new ResponseData<object>("User not found"));
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [HttpGet("verify")]
        public async Task<IResult> VerifyToken([FromQuery] VerifyTokenDto dto)
        {
            try
            {
                // Make sure the user is authenticated
                var VerifiedAt = DateTime.Now;
                var userToken = await dbContext.UserTokens
                    .Include(e => e.User)
                    .Where(e => e.Token == dto.U)
                    .OrderByDescending(e => e.CreatedAt)
                    .AsTracking().FirstOrDefaultAsync()
                    ?? throw new Exception("Invalid Verification Link");

                // Update Verification Status
                UserVerificationToken? userVerification = await dbContext.UserVerificationTokens.Where(e =>
                    e.UserId == userToken.UserId &&
                    e.Token == dto.T &&
                    e.VerificationType == dto.S
                ).AsTracking().FirstOrDefaultAsync() ?? throw new Exception("Invalid Verification Link");
                bool status = userVerification.ExpiresDate > VerifiedAt;
                if (!status) throw new Exception("Verification Code Expired");
                dbContext.Entry(userVerification).CurrentValues.SetValues(new { VerifiedAt });
                var result = await dbContext.SaveChangesAsync();

                UserStatus? userStatus = null;
                string message = "";
                switch (dto.S)
                {
                    case VerificationTypeEnum.Register:
                        // Update User Account Status
                        userStatus = await dbContext.UserStatuses.Where(uv =>
                            uv.UserId == userToken.UserId
                        ).FirstAsync() ?? throw new Exception("Invalid Account Status");
                        dbContext.Entry(userStatus).CurrentValues.SetValues(new { AcconutStatus = AccountStatusEnum.Verified });
                        message = "Account Verified Successfully";
                        break;
                    case VerificationTypeEnum.AccountDeletion:
                        // Delete User Account
                        await dbContext.UserStatuses.Where(e => e.UserId == userToken.UserId).AsTracking().ExecuteDeleteAsync();
                        await dbContext.UserProfiles.Where(e => e.UserId == userToken.UserId).AsTracking().ExecuteDeleteAsync();
                        await dbContext.Users.Where(e => e.Id == userToken.UserId).AsTracking().ExecuteDeleteAsync();
                        message = "Account Deleted Successfully";
                        break;
                    case VerificationTypeEnum.AccountDeactivation:
                        // Deactivate User Account
                        userStatus = await dbContext.UserStatuses.Where(e => e.UserId == userToken.UserId).AsTracking().FirstOrDefaultAsync();
                        dbContext.Entry(userStatus!).CurrentValues.SetValues(new { AccountStatus = AccountStatusEnum.Inactive });
                        message = "Account Deacitvated Successfully";
                        break;
                    default:
                        throw new Exception("Invalid Verification Link");
                }
                result = await dbContext.SaveChangesAsync();

                return result > 0 ? throw new Exception("Verify Account Failed") : Results.Ok(
                    new ResponseData<ResponseUserDto>(message)
                );
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [Authorize]
        [HttpPatch("verify")]
        public async Task<IResult> VerifyCode([FromBody] VerifyCodeDto dto)
        {
            try
            {
                // Make sure the user is authenticated
                var verifietAt = DateTime.Now;
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser == null) return Results.Unauthorized();

                // Update Verification Status
                UserVerification? userVerification = await dbContext.UserVerifications.Where(uv =>
                    uv.Code == dto.VerificationCode &&
                    uv.UserId == currentUser.Id &&
                    uv.VerificationType == VerificationTypeEnum.Register
                ).FirstAsync() ?? throw new Exception("Invalid Verification Code");
                bool status = userVerification.ExpiresDate > verifietAt;
                if (!status) throw new Exception("Verification Code Expired");
                dbContext.Entry(userVerification).CurrentValues.SetValues(new { VerifiedAt = verifietAt });
                var result = await dbContext.SaveChangesAsync();

                // Update User Account Status
                UserStatus? userStatus = await dbContext.UserStatuses.Where(uv =>
                    uv.UserId == currentUser.Id
                ).FirstAsync() ?? throw new Exception("Invalid Account Status");
                dbContext.Entry(userStatus).CurrentValues.SetValues(new { AcconutStatus = AccountStatusEnum.Verified });
                result = await dbContext.SaveChangesAsync();

                return result > 0 ? throw new Exception("Verify Account Failed") : Results.Ok(
                    new ResponseData<ResponseUserDto>("Verified Account Successfully")
                );
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [HttpGet("profile/{Username}")]
        public async Task<IResult> GetProfile(string Username)
        {
            try
            {
                User? currentUser = await dbContext.Users.Where(user => user.Username == Username).FirstOrDefaultAsync();
                if (currentUser != null)
                {
                    return Results.Ok(new ResponseData<ResponseUserDto>("Success", currentUser.ToResponseDto()));
                }
                else
                {
                    return Results.NotFound(new ResponseData<object>("User not found"));
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IResult> GetProfileSelf()
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    return Results.Ok(new ResponseData<ResponseUserDto>("Success", currentUser.ToResponseDto()));
                }
                else
                {
                    return Results.Unauthorized();
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [Authorize]
        [HttpPost("profile")]
        public async Task<IResult> CreateProfile([FromBody] CreateUserProfileDto dto)
        {
            try
            {
                User? currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    if (currentUser.Profile == null)
                    {
                        var profile = dto.ToEntity();
                        dbContext.UserProfiles.Add(profile);
                        await dbContext.SaveChangesAsync();
                        return Results.Ok(new ResponseData<object>("Profile Created Successfully"));
                    }
                    else
                    {
                        return Results.BadRequest(new ResponseData<object>("Profile Already Created"));
                    }
                }
                else
                {
                    return Results.NotFound(new ResponseData<object>("User not found"));
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            try
            {
                User? currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    if (currentUser.Profile != null)
                    {
                        dbContext.Entry(currentUser.Profile).CurrentValues.SetValues(dto.ToEntity());
                        return Results.Ok(new ResponseData<object>("Updated Successfully"));
                    }
                    else
                    {
                        return Results.BadRequest(new ResponseData<object>("User profile not found"));
                    }
                }
                else
                {
                    return Results.NotFound(new ResponseData<object>("User not found"));
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }
    }
}
