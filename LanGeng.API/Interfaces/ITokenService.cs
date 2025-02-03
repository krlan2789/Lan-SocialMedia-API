using System.Security.Claims;
using LanGeng.API.Entities;

namespace LanGeng.API.Interfaces;

public interface ITokenService
{
    string GenerateToken(string username, TimeSpan expiration);
    ClaimsPrincipal GetPrincipalFromToken(string token);
    string? GetToken(HttpContext httpContext);
    Task<string?> GetUsername(HttpContext httpContext);
    Task<User?> GetUser(HttpContext httpContext);
    Task<(User?, bool)> GetUserAndExpiredStatus(HttpContext httpContext);
}