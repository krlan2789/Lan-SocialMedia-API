namespace LanGeng.API.Dtos;

public record class UserPostDto
(
    int Id,
    string Slug,
    bool CommentAvailability,
    string? Content,
    PostMediaDto[]? Media,
    string AuthorName,
    string AuthorUserame,
    string? GroupName,
    string? GroupSlug,
    int CommentCount,
    int ReactionCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);