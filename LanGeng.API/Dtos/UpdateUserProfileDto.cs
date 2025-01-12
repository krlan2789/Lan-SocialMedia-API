using System.ComponentModel.DataAnnotations;

namespace LanGeng.API.Dtos;

public record class UpdateUserProfileDto
(
    string Bio,
    string ProfileImage,
    [StringLength(32)] string PhoneNumber,
    [StringLength(255)] string CityBorn,
    [StringLength(255)] string CityHome
);