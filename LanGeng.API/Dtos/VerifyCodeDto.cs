using System.ComponentModel.DataAnnotations;
using LanGeng.API.Enums;

namespace LanGeng.API.Dtos;

public record class VerifyCodeDto
(
    [Required, MinLength(6), StringLength(6)] string VerificationCode,
    VerificationTypeEnum VerificationType
);