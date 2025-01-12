using LanGeng.API.Data;
using LanGeng.API.Dtos;
using LanGeng.API.Entities;
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
        private readonly SocialMediaDatabaseContext dbContext;
        private readonly TokenService _tokenService;

        public UserController(ILogger<UserController> logger, TokenService tokenService, SocialMediaDatabaseContext context)
        {
            _logger = logger;
            dbContext = context;
            _tokenService = tokenService;
            dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
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
