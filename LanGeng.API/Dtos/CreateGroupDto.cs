using System.ComponentModel.DataAnnotations;
using LanGeng.API.Enums;

namespace LanGeng.API.Dtos;

public record class CreateGroupDto
(
    [MinLength(4), MaxLength(255)] string Name,
    PrivacyTypeEnum? PrivacyType,
    string? ProfileImage = null,
    [StringLength(1024)] string? Description = null
);