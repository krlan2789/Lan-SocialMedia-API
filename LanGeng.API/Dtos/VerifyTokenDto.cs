using LanGeng.API.Enums;

namespace LanGeng.API.Dtos;

public record class VerifyTokenDto
(
    string U,
    string T,
    VerificationTypeEnum? S
);