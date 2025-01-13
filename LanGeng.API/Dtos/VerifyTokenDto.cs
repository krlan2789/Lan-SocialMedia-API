using LanGeng.API.Enums;

namespace LanGeng.API.Dtos;

public record class VerifyTokenDto
(
    VerificationTypeEnum? S,
    string T,
    string U
);