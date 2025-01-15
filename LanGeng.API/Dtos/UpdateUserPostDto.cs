namespace LanGeng.API.Dtos;

public record class UpdateUserPostDto
(
    bool CommentAvailability = true,
    string? Content = null,
    List<IFormFile>? NewMedia = null,
    List<int>? DeletedMediaIds = null
);