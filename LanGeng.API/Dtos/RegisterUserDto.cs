using System.ComponentModel.DataAnnotations;

namespace LanGeng.API.Dtos;

public record class RegisterUserDto
(
    [Required, StringLength(255)] string Fullname,
    [Required, StringLength(128), EmailAddress] string Email,
    [Required, MinLength(8), StringLength(64)] string Password
);
