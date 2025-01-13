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
        [HttpPost("logout")]
        public async Task<IResult> PostLogout()
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser == null) return Results.Unauthorized();
                UserToken? currentUserToken = await dbContext.UserTokens.Where(ut => ut.User != null && ut.User.Username == currentUser.Username).FirstAsync();
                if (currentUserToken != null)
                {
                    await dbContext.UserTokens.Where(ut => ut.User != null && ut.User.Username == currentUser.Username).ExecuteDeleteAsync();
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
        [HttpPost("register")]
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
                var userVerifyToken = new UserVerificationToken
                {
                    UserId = currentUser.Id,
                    VerificationType = VerificationTypeEnum.Register,
                    Email = currentUser.Email,
                    Token = SlugHelper.Create(currentUser.Username)
                };
                dbContext.UserVerificationTokens.Add(userVerifyToken);
                await dbContext.SaveChangesAsync();

                // Save User Account Status
                var userStatus = new UserStatus
                {
                    UserId = currentUser.Id,
                    AccountStatus = AccountStatusEnum.Unverified
                };
                dbContext.UserStatuses.Add(userStatus);
                await dbContext.SaveChangesAsync();

                // Generate Token
                var userToken = new UserToken
                {
                    Token = _tokenService.GenerateToken(currentUser.Username, TimeSpan.FromDays(30)),
                    UserId = currentUser.Id,
                    ExpiresDate = DateTime.Now.Add(TimeSpan.FromDays(30))
                };
                dbContext.UserTokens.Add(userToken);
                await dbContext.SaveChangesAsync();

                // Send verification code
                var request = HttpContext.Request;
                var verificationLink = $"{request.Scheme}://{request.Host}/api/auth/verify?u={userToken.Token}&t={userVerifyToken.Token}&s={(byte)userVerifyToken.VerificationType}";
                await _emailService.SendAsync(
                    currentUser.Email,
                    "Email Verification",
                    $"Please verify your email by clicking the following link:\n\n<a href=\"{verificationLink}\">Verify</a>"
                );

                Response.Headers.Append("Authorization", $"Bearer {userToken.Token}");
                return currentUser is null ? Results.NotFound() : Results.Ok(
                    new ResponseData<ResponseUserDto>("Registration Successfully", userToken.Token)
                );
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }
    }
}
