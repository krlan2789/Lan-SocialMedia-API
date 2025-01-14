using System.ComponentModel;

namespace LanGeng.API.Dtos;

public record class CreateUserPostDto
(
    bool? CommentAvailability = true,
    string? Content = null,
    List<IFormFile>? Media = null,
    int? GroupId = null
);