using System.ComponentModel;

namespace LanGeng.API.Dtos;

public record class CreateUserPostDto
(
    [DefaultValue(true)] bool? CommentAvailability,
    string? Content,
    string[]? Media,
    int? GroupId
);