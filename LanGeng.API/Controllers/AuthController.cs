using System.ComponentModel;
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

namespace LanGeng.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly TokenService _tokenService;
        private readonly EmailService _emailService;
        private readonly SocialMediaDatabaseContext dbContext;

        public AuthController(ILogger<AuthController> logger, TokenService tokenService, EmailService emailService, SocialMediaDatabaseContext context)
        {
            _logger = logger;
            _tokenService = tokenService;
            _emailService = emailService;
            dbContext = context;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [EndpointSummary("Login")]
        [EndpointDescription("Login to get token for credential.")]
        [ProducesResponseType<ResponseData<ResponseUserDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ResponseError<LoginUserDto>>(StatusCodes.Status404NotFound)]
        public async Task<IResult> Login(
            [FromBody, Description("Credential Form")] LoginUserDto dto
        )
        {
            try
            {
                User? currentUser = await dbContext.Users.Where(e => e.Username == dto.Username).FirstOrDefaultAsync();
                if (currentUser != null && currentUser.VerifyPassword(dto.Password))
                {
                    var token = _tokenService.GenerateToken(currentUser.Username, TimeSpan.FromDays(30));
                    await dbContext.UserTokens.Where(e => e.UserId == currentUser.Id).ExecuteDeleteAsync();
                    dbContext.UserTokens.Add(new UserToken
                    {
                        Token = token,
                        UserId = currentUser.Id,
                        ExpiresDate = DateTime.Now.Add(TimeSpan.FromDays(30))
                    });
                    await dbContext.SaveChangesAsync();
                    Response.Headers.Append("Authorization", $"Bearer {token}");
                    return Results.Ok(new ResponseData<ResponseUserDto>("Login Successfully", token));
                }
                else
                {
                    return Results.NotFound(new ResponseError<LoginUserDto>("Invalid username or password", dto));
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseError<object>(e.Message));
            }
        }

        [Authorize]
        [HttpDelete("logout")]
        [EndpointSummary("Logout")]
        [EndpointDescription("Logout, to clear active token.")]
        [ProducesResponseType<ResponseData<ResponseUserDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IResult> Logout()
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser == null) return Results.Unauthorized();
                UserToken? currentUserToken = await dbContext.UserTokens.Where(e => e.User != null && e.User.Username == currentUser.Username).FirstAsync();
                if (currentUserToken != null)
                {
                    await dbContext.UserTokens.Where(e => e.User != null && e.User.Username == currentUser.Username).ExecuteDeleteAsync();
                    Response.Headers.Remove("Authorization");
                    return Results.Ok(new ResponseData<object>("Logout Successfully"));
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

        [AllowAnonymous]
        [HttpPost("register")]
        [EndpointSummary("Register")]
        [EndpointDescription("Create new account to use authorized endpoints.")]
        [ProducesResponseType<ResponseData<ResponseUserDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        public async Task<IResult> Register([FromBody, Description("Registration Form")] RegisterUserDto userDto)
        {
            try
            {
                // Save User if doesn't exist
                if (await dbContext.Users.Where(e => e.Email == userDto.Email).AnyAsync()) return Results.Conflict();
                var currentUser = userDto.ToEntity();
                dbContext.Users.Add(currentUser);
                // await dbContext.SaveChangesAsync();

                // Save User verification
                var userVerifyToken = new UserVerificationToken
                {
                    UserId = currentUser.Id,
                    VerificationType = VerificationTypeEnum.Register,
                    Email = currentUser.Email,
                    Token = SlugHelper.Create(currentUser.Username)
                };
                dbContext.UserVerificationTokens.Add(userVerifyToken);

                // Save User Account Status
                var userStatus = new UserStatus
                {
                    UserId = currentUser.Id,
                    AccountStatus = AccountStatusEnum.Unverified
                };
                dbContext.UserStatuses.Add(userStatus);

                // Generate Token
                var userToken = new UserToken
                {
                    Token = _tokenService.GenerateToken(currentUser.Username, TimeSpan.FromDays(30)),
                    UserId = currentUser.Id,
                    ExpiresDate = DateTime.Now.Add(TimeSpan.FromDays(30))
                };
                dbContext.UserTokens.Add(userToken);
                var results = await dbContext.SaveChangesAsync();
                if (results < 1) throw new Exception("Registration failed");

                // Send verification code
                var request = HttpContext.Request;
                var verificationLink = $"{request.Scheme}://{request.Host}/api/auth/verify?u={userToken.Token}&t={userVerifyToken.Token}&s={(byte)userVerifyToken.VerificationType}";
                var error = await _emailService.SendAsync(
                    currentUser.Email,
                    "Email Verification",
                    $"Please verify your email by clicking the following link:\n\n<a href=\"{verificationLink}\">Verify</a>"
                );

                Response.Headers.Append("Authorization", $"Bearer {userToken.Token}");
                return error != null ? throw new Exception("Registration failed") : Results.Ok(
                    new ResponseData<ResponseUserDto>("Registration Successfully", userToken.Token)
                );
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [HttpGet("verify")]
        [EndpointSummary("Verify (Token URL)")]
        [EndpointDescription("Verify some task using verification token URL.")]
        [ProducesResponseType<ResponseData<ResponseUserDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        public async Task<IResult> VerifyToken([FromQuery, Description("Credential Params")] VerifyTokenDto dto)
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
                if (userVerification.ExpiresDate < VerifiedAt) throw new Exception("Verification Code Expired");
                dbContext.Entry(userVerification).CurrentValues.SetValues(new { VerifiedAt });

                UserStatus? userStatus = null;
                string message = "";
                switch (dto.S)
                {
                    case VerificationTypeEnum.Register:
                        // Update User Account Status
                        userStatus = await VerifiedUser(userToken.UserId) ?? throw new Exception("Invalid Account Status");
                        message = "Account Verified Successfully";
                        break;
                    case VerificationTypeEnum.AccountDeletion:
                        // Delete User Account
                        await DeleteUser(userToken.UserId, VerifiedAt);
                        message = "Account Deleted Successfully";
                        break;
                    case VerificationTypeEnum.AccountDeactivation:
                        // Deactivate User Account
                        userStatus = await DeactivateUser(userToken.UserId);
                        message = "Account Deacitvated Successfully";
                        break;
                    default:
                        throw new Exception("Invalid Verification Link");
                }
                var result = await dbContext.SaveChangesAsync();

                return result > 0 ? Results.Ok(
                    new ResponseData<ResponseUserDto>(message)
                ) : throw new Exception("Verify Account Failed");
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [Authorize]
        [HttpPatch("verify")]
        [EndpointSummary("Verify (Security Code)")]
        [EndpointDescription("Verify some task using security code (authorized user are needed).")]
        [ProducesResponseType<ResponseData<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        public async Task<IResult> VerifyCode([FromBody, Description("Verification Form")] VerifyCodeDto dto)
        {
            try
            {
                // Make sure the user is authenticated
                var VerifiedAt = DateTime.Now;
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser == null) return Results.Unauthorized();

                // Update Verification Status
                UserVerification? userVerification = await dbContext.UserVerifications.Where(e =>
                    e.UserId == currentUser.Id &&
                    e.Code == dto.VerificationCode &&
                    e.VerificationType == dto.VerificationType
                ).FirstAsync() ?? throw new Exception("Invalid Verification Code");
                if (userVerification.ExpiresDate < VerifiedAt) throw new Exception("Verification Code Expired");
                dbContext.Entry(userVerification).CurrentValues.SetValues(new { VerifiedAt });

                // Update User Account Status
                UserStatus? userStatus = null;
                string message = "";
                switch (userVerification.VerificationType)
                {
                    case VerificationTypeEnum.Register:
                        // Update User Account Status
                        userStatus = await VerifiedUser(userVerification.UserId) ?? throw new Exception("Invalid Account Status");
                        message = "Account Verified Successfully";
                        break;
                    case VerificationTypeEnum.AccountDeletion:
                        // Delete User Account
                        await DeleteUser(userVerification.UserId, VerifiedAt);
                        message = "Account Deleted Successfully";
                        break;
                    case VerificationTypeEnum.AccountDeactivation:
                        // Deactivate User Account
                        userStatus = await DeactivateUser(userVerification.UserId);
                        message = "Account Deacitvated Successfully";
                        break;
                    default:
                        throw new Exception("Invalid Verification Link");
                }
                var result = await dbContext.SaveChangesAsync();

                return result > 0 ? Results.Ok(
                    new ResponseData<object>(message)
                ) : throw new Exception("Verify Account Failed");
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        private async Task<bool> DeleteUser(int UserId, DateTime DeletedAt)
        {
            var user = await dbContext.Users.Where(e => e.Id == UserId).AsTracking().FirstOrDefaultAsync();
            if (user == null) return false;
            dbContext.Entry(user).CurrentValues.SetValues(new { DeletedAt });

            var status = await dbContext.UserStatuses.Where(e => e.UserId == UserId).AsTracking().FirstOrDefaultAsync();
            if (status == null) return false;
            dbContext.Entry(status).CurrentValues.SetValues(new { AccountStatus = AccountStatusEnum.Deleted });

            return (await dbContext.SaveChangesAsync()) > 0;
        }

        private async Task<UserStatus?> DeactivateUser(int UserId)
        {
            var userStatus = await dbContext.UserStatuses.Where(e => e.UserId == UserId).AsTracking().FirstOrDefaultAsync();
            dbContext.Entry(userStatus!).CurrentValues.SetValues(new { AccountStatus = AccountStatusEnum.Inactive });
            return userStatus;
        }

        private async Task<UserStatus?> VerifiedUser(int UserId)
        {
            var userStatus = await dbContext.UserStatuses.Where(e =>
                e.UserId == UserId
            ).FirstAsync();
            dbContext.Entry(userStatus).CurrentValues.SetValues(new { AcconutStatus = AccountStatusEnum.Verified });
            return userStatus;
        }
    }
}
