namespace LanGeng.API.Dtos;

public record class ResponseUserDto
(
    string Fullname,
    string Email,
    string? Username,
    string? Bio,
    string? ProfileImage
// string? PhoneNumber,
// string? CityBorn,
// string? CityHome,
// DateOnly? BirthDate
);
