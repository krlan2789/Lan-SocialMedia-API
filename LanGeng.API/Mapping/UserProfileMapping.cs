using LanGeng.API.Dtos;
using LanGeng.API.Entities;

namespace LanGeng.API.Mapping;

public static class UserProfileMapping
{
    public static UserProfile ToEntity(this CreateUserProfileDto dto)
    {
        return new UserProfile
        {
            Bio = dto.Bio,
            ProfileImage = dto.ProfileImage,
            PhoneNumber = dto.PhoneNumber,
            CityBorn = dto.CityBorn,
            CityHome = dto.CityHome,
            BirthDate = dto.BirthDate,
            UpdatedAt = DateTime.Now
        };
    }

    public static UserProfile ToEntity(this UpdateUserProfileDto dto)
    {
        return new UserProfile
        {
            Bio = dto.Bio,
            ProfileImage = dto.ProfileImage,
            PhoneNumber = dto.PhoneNumber,
            CityBorn = dto.CityBorn,
            CityHome = dto.CityHome,
            UpdatedAt = DateTime.Now
        };
    }
}
