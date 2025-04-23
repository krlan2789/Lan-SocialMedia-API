using LanGeng.API.Controllers;
using LanGeng.API.Data;
using LanGeng.API.Dtos;
using LanGeng.API.Entities;
using LanGeng.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Moq;
using LanGeng.API.Interfaces;

namespace LanGeng.Test;

public class AuthControllerTest
{
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly SocialMediaDatabaseContext dbContext;
    private readonly AuthController _controller;

    public AuthControllerTest()
    {
        _loggerMock = new Mock<ILogger<AuthController>>();
        _tokenServiceMock = new Mock<ITokenService>();
        _emailServiceMock = new Mock<IEmailService>();
        _userServiceMock = new Mock<IUserService>();
        var options = new DbContextOptionsBuilder<SocialMediaDatabaseContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        dbContext = new SocialMediaDatabaseContext(options);
        _controller = new AuthController(_loggerMock.Object, _tokenServiceMock.Object, _emailServiceMock.Object, _userServiceMock.Object);
    }

    [Fact]
    public async Task Login_ReturnsOkResult_WhenCredentialsAreValid()
    {
        // Arrange
        var loginDto = new LoginUserDto("testuser", "password");
        var user = new User { Username = "testuser", PasswordHash = "hashedpassword", Fullname = "Test User", Email = "testuser@example.com" };
        dbContext.Add(user);
        await dbContext.SaveChangesAsync();
        _tokenServiceMock.Setup(ts => ts.GenerateToken(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns("token");

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = Assert.IsAssignableFrom<IResult>(result);
        var responseData = Assert.IsType<ResponseData<ResponseUserDto>>(okResult);
        Assert.Equal("Login Successfully", responseData.Message);
    }

    [Fact]
    public async Task Login_ReturnsNotFoundResult_WhenCredentialsAreInvalid()
    {
        // Arrange
        var loginDto = new LoginUserDto("testuser", "wrongpassword");
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == loginDto.Username);
        NotFoundObjectResult notFoundResult;
        if (user == null || user.PasswordHash != "hashedpassword")
        {
            notFoundResult = new NotFoundObjectResult(new ResponseError<LoginUserDto>("Invalid username or password"));
        }

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var responseError = Assert.IsType<ResponseError<LoginUserDto>>(notFoundResult.Value as ResponseError<LoginUserDto>);
        Assert.Equal("Invalid username or password", responseError.Message);
    }

    [Fact]
    public async Task Logout_ReturnsOkResult_WhenUserIsAuthenticated()
    {
        // Arrange
        var user = new User { Username = "testuser", Fullname = "Test User", Email = "testuser@example.com", PasswordHash = "hashedpassword" };
        _tokenServiceMock.Setup(ts => ts.GetUser(It.IsAny<HttpContext>())).ReturnsAsync(user);
        var userToken = new UserToken { Token = "token" };
        await dbContext.UserTokens.AddAsync(userToken);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.Logout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseData = Assert.IsType<ResponseData<object>>(okResult.Value as ResponseData<object>);
        Assert.Equal("Logout Successfully", responseData.Message);
    }

    [Fact]
    public async Task Logout_ReturnsUnauthorizedResult_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _tokenServiceMock.Setup(ts => ts.GetUser(It.IsAny<HttpContext>())).ReturnsAsync((User?)null);

        // Act
        var result = await _controller.Logout();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Register_ReturnsOkResult_WhenRegistrationIsSuccessful()
    {
        // Arrange
        var registerDto = new RegisterUserDto("testuser", "test@example.com", "password");
        var userExists = await dbContext.Users.AnyAsync(u => u.Email == registerDto.Email);

        ConflictResult? conflictResult;
        if (userExists)
        {
            conflictResult = new ConflictResult();
        }
        _tokenServiceMock.Setup(ts => ts.GenerateToken(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns("token");
        _emailServiceMock.Setup(es => es.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((string?)null);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseData = Assert.IsType<ResponseData<ResponseUserDto>>(okResult.Value);
        Assert.Equal("Registration Successfully", responseData.Message);
    }

    [Fact]
    public async Task Register_ReturnsConflictResult_WhenUserAlreadyExists()
    {
        // Arrange
        var registerDto = new RegisterUserDto("testuser", "test@example.com", "password");
        await dbContext.Users.AddAsync(new User { Username = "testuser", Email = "test@example.com", PasswordHash = "hashedpassword", Fullname = "Test User" });
        await dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        Assert.IsType<ConflictResult>(result);
    }
}