using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LanGeng.API.Data;
using LanGeng.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LanGeng.API.Services;

public class TokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly ILogger<TokenService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly SocialMediaDatabaseContext DbContext;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger, IServiceProvider serviceProvider)
    {
        _secretKey = "" + configuration["Jwt:SecretKey"];
        _issuer = "" + configuration["Jwt:Issuer"];
        _audience = "" + configuration["Jwt:Audience"];
        _logger = logger;
        _serviceProvider = serviceProvider;
        DbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<SocialMediaDatabaseContext>();
    }

    public string GenerateToken(string Username, TimeSpan expiration)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.Now.Add(expiration),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal GetPrincipalFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_secretKey)),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true
        };
        return tokenHandler.ValidateToken(token, validationParameters, out _);
    }

    public async Task<string?> GetUsername(HttpContext httpContext)
    {
        try
        {
            string token = "" + httpContext.Request.Headers["Authorization"].ToString().Split(" ")[1];
            _logger.LogInformation("TokenService: Token={Token}", token);
            var username = GetPrincipalFromToken(token).Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
            _logger.LogInformation("TokenService: Username={Username}", username);
            UserToken? userToken = await DbContext.UserTokens.Where(ut => ut.Token == token && ut.User!.Username == username).FirstOrDefaultAsync();
            if (userToken == null || userToken.ExpiresDate < DateTime.Now) username = null;
            return username ?? null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<User?> GetUser(HttpContext httpContext)
    {
        var username = await GetUsername(httpContext);
        User? currentUser = await DbContext.Users.Where(user => user.Username == username).FirstOrDefaultAsync();
        return currentUser;
    }

    public async Task<(User?, bool)> GetUserAndExpiredStatus(HttpContext httpContext)
    {
        try
        {
            string token = "" + httpContext.Request.Headers["Authorization"].ToString().Split(" ")[1];
            _logger.LogInformation("TokenService: Token={Token}", token);
            var username = GetPrincipalFromToken(token).Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
            _logger.LogInformation("TokenService: Username={Username}", username);
            bool isExpired = true;
            UserToken? userToken = await DbContext.UserTokens.Where(ut => ut.Token == token && ut.User!.Username == username).FirstOrDefaultAsync();
            if (userToken != null && userToken.ExpiresDate > DateTime.Now) isExpired = false;
            User? currentUser = await DbContext.Users.Where(user => user.Username == username).FirstOrDefaultAsync();
            return (currentUser, isExpired);
        }
        catch (Exception)
        {
            return (null, true);
        }
    }
}
