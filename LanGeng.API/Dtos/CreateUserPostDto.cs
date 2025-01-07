using System.ComponentModel;

namespace LanGeng.API.Dtos;

public record class CreateUserPostDto
(
    [DefaultValue(true)] bool? CommentAvailability = true,
    string? Content = null,
    string[]? Media = null,
    int? GroupId = null
);