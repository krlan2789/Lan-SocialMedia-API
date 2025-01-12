using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LanGeng.API.Dtos;

public record class CreateUserProfileDto
(
    string Bio,
    string ProfileImage,
    [TypeConverter(typeof(DateOnlyConverter))] DateOnly? BirthDate,
    [StringLength(32)] string? PhoneNumber,
    [StringLength(255)] string? CityBorn,
    [StringLength(255)] string? CityHome
);