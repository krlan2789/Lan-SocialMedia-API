using System.ComponentModel;
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
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;

        public AuthController(ILogger<AuthController> logger, ITokenService tokenService, IEmailService emailService, IUserService userService)
        {
            _logger = logger;
            _tokenService = tokenService;
            _emailService = emailService;
            _userService = userService;
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
                User? currentUser = await _userService.GetUserByUsernameAsync(dto.Username);
                if (currentUser != null && currentUser.VerifyPassword(dto.Password))
                {
                    var token = _tokenService.GenerateToken(currentUser.Username, TimeSpan.FromDays(30));
                    var result = await _userService.CreateUserTokenAsync(currentUser.Id, token);
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
                if (currentUser != null)
                {
                    await _userService.DeleteUserTokenAsync(currentUser.Id);
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
        public async Task<IResult> Register(
            [FromBody, Description("Registration Form")] RegisterUserDto userDto
        )
        {
            try
            {
                // Save User if doesn't exist
                if (await _userService.HasEmailAsync(userDto.Email)) return Results.Conflict();
                var currentUser = userDto.ToEntity();
                var token = _tokenService.GenerateToken(currentUser.Username, TimeSpan.FromDays(30));
                currentUser = await _userService.CreateUserAsync(currentUser, token);
                if (currentUser == null) throw new Exception("Failed to create user");

                // Send verification code
                var userToken = currentUser.UserTokens.First();
                var verificationToken = currentUser.VerificationTokens.First();
                var request = HttpContext.Request;
                var verificationLink =
                    $"{request.Scheme}://{request.Host}/api/auth/verify?u={userToken.Token}&" +
                    $"t={verificationToken.Token}&s={(byte)verificationToken.VerificationType}";
                var error = await _emailService.SendAsync(
                        currentUser.Email,
                        "Email Verification",
                        EmailHelper.CreateHtmlWithLink(currentUser.Fullname, "Email Verification", "Please verify your email by clicking the following link:", verificationLink)
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
        public async Task<IResult> VerifyToken(
            [FromQuery, Description("Credential Params")] VerifyTokenDto dto
        )
        {
            try
            {
                // Make sure the user is authenticated
                var VerifiedAt = DateTime.Now;
                var userToken =
                    await _userService.GetUserTokenByTokenAsync(dto.U) ??
                    throw new Exception("Invalid Verification Link");

                // Update Verification Status
                var userVerification = await _userService
                    .GetVerificationTokenByTokenAsync(userToken.UserId, dto.T, dto.S) ??
                    throw new Exception("Invalid Verification Link");
                if (userVerification.ExpiresDate < VerifiedAt) throw new Exception("Verification link Expired");
                var result = await _userService.UpdateVerificationTokenAsync(userVerification, VerifiedAt);
                if (string.IsNullOrEmpty(result)) throw new Exception("Verification link Expired or Invalid");

                string? error = null;
                string message = "";
                switch (dto.S)
                {
                    case VerificationTypeEnum.Register:
                        // Update User Account Status
                        error = await VerifiedUser(userToken.UserId);
                        message = error ?? "Account Verified Successfully";
                        break;
                    case VerificationTypeEnum.AccountDeletion:
                        // Delete User Account
                        error = await DeleteUser(userToken.UserId, VerifiedAt);
                        message = error ?? "Account Deleted Successfully";
                        break;
                    case VerificationTypeEnum.AccountDeactivation:
                        // Deactivate User Account
                        error = await DeactivateUser(userToken.UserId);
                        message = error ?? "Account Deacitvated Successfully";
                        break;
                    default:
                        throw new Exception("Invalid Verification Link");
                }

                return string.IsNullOrEmpty(result) && string.IsNullOrEmpty(error) ? Results.Ok(
                    new ResponseData<object>(message)
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
                var userVerification = await _userService
                    .GetVerificationCodeAsync(currentUser.Id, dto.VerificationCode, dto.VerificationType) ??
                    throw new Exception("Invalid Verification Code");
                if (userVerification.ExpiresDate < VerifiedAt) throw new Exception("Verification Code Expired");
                var result = await _userService.UpdateVerificationCodeAsync(userVerification, VerifiedAt);

                // Update User Account Status
                string? error = null;
                string message = "";
                switch (userVerification.VerificationType)
                {
                    case VerificationTypeEnum.Register:
                        // Update User Account Status
                        error = await VerifiedUser(userVerification.UserId);
                        message = error ?? "Account Verified Successfully";
                        break;
                    case VerificationTypeEnum.AccountDeletion:
                        // Delete User Account
                        error = await DeleteUser(userVerification.UserId, VerifiedAt);
                        message = error ?? "Account Deleted Successfully";
                        break;
                    case VerificationTypeEnum.AccountDeactivation:
                        // Deactivate User Account
                        error = await DeactivateUser(userVerification.UserId);
                        message = error ?? "Account Deacitvated Successfully";
                        break;
                    default:
                        throw new Exception("Invalid Verification Link");
                }

                return string.IsNullOrEmpty(result) && string.IsNullOrEmpty(error) ? Results.Ok(
                    new ResponseData<object>(message)
                ) : throw new Exception("Verify Account Failed");
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ResponseData<object>(e.Message));
            }
        }

        private async Task<string?> DeleteUser(int UserId, DateTime DeletedAt)
        {
            var error = await _userService.DeleteUserAsync(UserId, DeletedAt);
            if (string.IsNullOrEmpty(error))
            {
                _logger.LogError($"Failed deleting {UserId} at {DeletedAt}: {error}");
                return error;
            }
            var result = await _userService.UpdateUserStatusAsync(UserId, AccountStatusEnum.Deleted);
            return result;
        }

        private async Task<string?> DeactivateUser(int UserId)
        {
            return await _userService.UpdateUserStatusAsync(UserId, AccountStatusEnum.Inactive);
        }

        private async Task<string?> VerifiedUser(int UserId)
        {
            return await _userService.UpdateUserStatusAsync(UserId, AccountStatusEnum.Verified); ;
        }
    }
}
