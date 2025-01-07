using LanGeng.API.Data;
using LanGeng.API.Dtos;
using LanGeng.API.Entities;
using LanGeng.API.Enums;
using LanGeng.API.Mapping;
using LanGeng.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly SocialMediaDatabaseContext dbContext;
        private readonly TokenService _tokenService;

        public AuthController(ILogger<AuthController> logger, TokenService tokenService, SocialMediaDatabaseContext context)
        {
            _logger = logger;
            dbContext = context;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("login", Name = nameof(PostLogin))]
        public async Task<IResult> PostLogin(LoginUserDto dto)
        {
            try
            {
                User? currentUser = await dbContext.Users.Where(user => user.Username == dto.Username).FirstOrDefaultAsync();
                if (currentUser != null && currentUser.VerifyPassword(dto.Password))
                {
                    var token = _tokenService.GenerateToken(currentUser.Username, TimeSpan.FromDays(30));
                    await dbContext.UserTokens.Where(ut => ut.UserId == currentUser.Id).ExecuteDeleteAsync();
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
                    return Results.Unauthorized();
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [Authorize]
        [HttpPost("logout", Name = nameof(PostLogout))]
        public async Task<IResult> PostLogout()
        {
            try
            {
                string username = _tokenService.GetUserIdFromToken(HttpContext);
                UserToken? currentUserToken = await dbContext.UserTokens.Where(ut => ut.User != null && ut.User.Username == username).FirstAsync();
                if (currentUserToken != null)
                {
                    await dbContext.UserTokens.Where(ut => ut.User != null && ut.User.Username == username).ExecuteDeleteAsync();
                    Response.Headers.Remove("Authorization");
                    return Results.Ok(new ResponseData<ResponseUserDto>("Logout Successfully"));
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
        [HttpPost("register", Name = nameof(PostRegister))]
        public async Task<IResult> PostRegister([FromBody] RegisterUserDto userDto)
        {
            try
            {
                // Save User if doesn't exist
                if (await dbContext.Users.Where(user => user.Email == userDto.Email).AnyAsync()) return Results.Conflict();
                var currentUser = userDto.ToEntity();
                dbContext.Users.Add(currentUser);
                await dbContext.SaveChangesAsync();

                // Save User verification
                // User? currentUser = await dbContext.Users.Where(user => user.Email == userDto.Email).FirstAsync();
                dbContext.UserVerifications.Add(new UserVerification
                {
                    UserId = currentUser.Id,
                    VerificationType = VerificationTypeEnum.Register,
                    Email = currentUser.Email
                });

                // Save User Account Status
                dbContext.UserStatuses.Add(new UserStatus
                {
                    UserId = currentUser.Id,
                    AccountStatus = AccountStatusEnum.Unverified
                });
                var result = await dbContext.SaveChangesAsync();

                // Generate Token
                var token = _tokenService.GenerateToken(currentUser.Username, TimeSpan.FromDays(30));
                Response.Headers.Append("Authorization", $"Bearer {token}");
                return currentUser is null && result > 0 ? Results.NotFound() : Results.Ok(
                    new ResponseData<ResponseUserDto>("Registration Successfully", token)
                );
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        [Authorize]
        [HttpPatch("verify", Name = nameof(PatchVerifyAccount))]
        public async Task<IResult> PatchVerifyAccount([FromBody] VerifyAccountDto dto)
        {
            try
            {
                // Make sure the user is authenticated
                var verifietAt = DateTime.Now;
                string username = _tokenService.GetUserIdFromToken(HttpContext);
                User? currentUser = await dbContext.Users.Where(user => user.Username == username).FirstAsync();
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
    }
}
