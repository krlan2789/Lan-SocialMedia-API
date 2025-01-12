using System.Text.RegularExpressions;
using LanGeng.API.Dtos;
using LanGeng.API.Entities;
using LanGeng.API.Helper;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Mapping;

public static class UserMapping
{
    public static User ToEntity(this RegisterUserDto dto)
    {
        return new User
        {
            Fullname = dto.Fullname,
            Email = dto.Email,
            Username = Regex.Replace("" + dto.Email.Split('@')[0], @"[^a-zA-Z0-9._]", string.Empty),
            PasswordHash = dto.Password.Hash(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    public static ResponseUserDto ToResponseDto(this User user)
    {
        return new ResponseUserDto
        (
            user.Fullname,
            user.Email,
            user.Username,
            user.Profile?.Bio,
            user.Profile?.ProfileImage
        // user.Profile?.PhoneNumber,
        // user.Profile?.CityBorn,
        // user.Profile?.CityHome,
        // user.Profile?.BirthDate
        );
    }

    public static bool VerifyPassword(this User user, string password)
    {
        return password.VerifyHashed(user.PasswordHash);
    }

    public static IQueryable<UserPost> IncludeAll(this DbSet<UserPost> post)
    {
        return post
            .Include(up => up.Author)
            .Include(up => up.Group)
            .Include(up => up.Reactions)
            .Include(up => up.Comments)
            .Include(up => up.Hashtags);
    }
}
