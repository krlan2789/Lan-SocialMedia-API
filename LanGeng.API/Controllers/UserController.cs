using LanGeng.API.Dtos;
using LanGeng.API.Entities;
using LanGeng.API.Enums;
using LanGeng.API.Helper;
using LanGeng.API.Interfaces;
using LanGeng.API.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LanGeng.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;

        public UserController(ILogger<UserController> logger, ITokenService tokenService, IEmailService emailService, IUserService userService)
        {
            _logger = logger;
            _tokenService = tokenService;
            _emailService = emailService;
            _userService = userService;
        }

        [Authorize]
        [HttpDelete()]
        [EndpointSummary("Request Delete")]
        [EndpointDescription("Request delete and receive email verification.")]
        [ProducesResponseType<ResponseData<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status404NotFound)]
        public async Task<IResult> RequestDelete()
        {
            try
            {
                var currentUser = await _tokenService.GetUser(HttpContext);
                var token = _tokenService.GetToken(HttpContext);
                if (currentUser != null && !string.IsNullOrEmpty(token))
                {
                    var verifyToken = SlugHelper.Create(currentUser.Username);
                    var verifyType = VerificationTypeEnum.AccountDeletion;
                    var error = await _userService.CreateVerificationTokenAsync(currentUser.Id, verifyToken, verifyType);
                    if (!string.IsNullOrEmpty(error)) throw new Exception(error);
                    var request = HttpContext.Request;
                    var verificationLink = $"{request.Scheme}://{request.Host}/api/auth/verifyemail?u={token}&t={verifyToken}&s={(byte)verifyType}";
                    error = await _emailService.SendAsync(
                        currentUser.Email,
                        "Email Verification",
                        EmailHelper.CreateHtmlWithLink(currentUser.Fullname, "Email Verification", "Please verify your email by clicking the following link:", verificationLink)
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
        [EndpointSummary("Get User Profile")]
        [EndpointDescription("Get user profile by username.")]
        [ProducesResponseType<ResponseData<ResponseUserDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status404NotFound)]
        public async Task<IResult> GetProfile(string Username)
        {
            try
            {
                User? currentUser = await _userService.GetUserByUsernameAsync(Username);
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
        [EndpointSummary("Get My Profile")]
        [EndpointDescription("Get authenticated user profile.")]
        [ProducesResponseType<ResponseData<ResponseUserDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
        [EndpointSummary("Create User Profile")]
        [EndpointDescription("Create user profile.")]
        [ProducesResponseType<ResponseData<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IResult> CreateProfile([FromBody] CreateUserProfileDto dto)
        {
            try
            {
                User? currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    if (currentUser.Profile == null)
                    {
                        var error = await _userService.CreateProfileAsync(dto.ToEntity());
                        if (error != null) throw new Exception(error);
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
        [EndpointSummary("Update User Profile")]
        [EndpointDescription("Update user profile.")]
        [ProducesResponseType<ResponseData<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ResponseError<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            try
            {
                User? currentUser = await _tokenService.GetUser(HttpContext);
                if (currentUser != null)
                {
                    if (currentUser.Profile != null)
                    {
                        var results = await _userService.UpdateProfileAsync(currentUser.Username, dto.ToEntity());
                        return results == null ? Results.Ok(new ResponseData<object>("Update Successful")) : throw new Exception("Failed to updating profile");
                    }
                    else
                    {
                        return Results.BadRequest(new ResponseData<object>("User profile not found"));
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
    }
}
