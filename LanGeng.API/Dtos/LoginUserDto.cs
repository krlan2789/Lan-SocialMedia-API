using System.ComponentModel.DataAnnotations;

namespace LanGeng.API.Dtos;

public record class LoginUserDto
(
    [Required, StringLength(128)] string Username,
    [Required, MinLength(8), StringLength(64)] string Password
);