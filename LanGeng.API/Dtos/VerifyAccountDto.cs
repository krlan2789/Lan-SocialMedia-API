using System.ComponentModel.DataAnnotations;

namespace LanGeng.API.Dtos;

public record class VerifyAccountDto
(
    [Required, MinLength(6), StringLength(6)] string VerificationCode
);