using LanGeng.API.Entities;

namespace LanGeng.API.Dtos;

public record class UserPostFullDto
(
    string Slug,
    bool CommentAvailability,
    string? Content,
    string[]? Media,
    string AuthorName,
    string AuthorUserame,
    string? GroupName,
    string? GroupSlug,
    PostCommentDto[] Comments,
    PostReactionDto[] Reactions,
    DateTime CreatedAt,
    DateTime UpdatedAt
);