using System.ComponentModel.DataAnnotations;

namespace LanGeng.API.Dtos;

public record class CreateGroupMemberDto
(
    [MinLength(4), MaxLength(255)] string Group
);