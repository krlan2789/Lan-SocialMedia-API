namespace LanGeng.API.Dtos;

public record class ResponseUserDto
(
    string Fullname,
    string Email,
    string? Username = null,
    string? Bio = null,
    string? ProfileImage = null
// string? PhoneNumber = null,
// string? CityBorn = null,
// string? CityHome = null,
// DateOnly? BirthDate = null
);
