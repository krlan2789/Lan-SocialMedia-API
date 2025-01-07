using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LanGeng.API.Services;

public class TokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public TokenService(string secretKey, string issuer, string audience)
    {
        _secretKey = secretKey;
        _issuer = issuer;
        _audience = audience;
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

    public string GetUserIdFromToken(HttpContext httpContext)
    {
        // bool isAuthenticated = httpContext.User?.Identity?.IsAuthenticated == true;
        string token = "" + httpContext.Request.Headers["Authorization"].ToString().Split(" ")[1];
        var claim = GetPrincipalFromToken(token).Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
        return claim == null /*|| !isAuthenticated*/ ? "" : claim;
    }
}
