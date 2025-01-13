using System.Reflection.Metadata.Ecma335;
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
                    var results = await dbContext.SaveChangesAsync();
                    if (results < 1) throw new Exception("Failed to creating verification link");

                    var request = HttpContext.Request;
                    var verificationLink = $"{request.Scheme}://{request.Host}/api/auth/verifyemail?u={token}&t={userVerifyToken.Token}&s={(byte)userVerifyToken.VerificationType}";
                    var error = await _emailService.SendAsync(
                        currentUser.Email,
                        "Email Verification",
                        $"Please verify your email by clicking the following link:\n\n<a href=\"{verificationLink}\">Verify</a>"
                    );

                    return error == null ? Results.Ok(new ResponseData<object>("Request deleting user data has been sent")) : throw new Exception(error);
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

        [HttpGet("profile/{Username}")]
        public async Task<IResult> GetProfile(string Username)
        {
            try
            {
                User? currentUser = await dbContext.Users.Where(e => e.Username == Username).FirstOrDefaultAsync();
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
                        return Results.BadRequest(new ResponseData<object>("Profile already created"));
                    }
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
                        var results = await dbContext.SaveChangesAsync();
                        return results > 0 ? Results.Ok(new ResponseData<object>("Updated Successfully")) : throw new Exception("Failed to updating profile");
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
