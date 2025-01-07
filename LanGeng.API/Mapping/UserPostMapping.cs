using LanGeng.API.Dtos;
using LanGeng.API.Entities;

namespace LanGeng.API.Mapping;

public static class UserPostMapping
{
    public static UserPost ToEntity(this CreateUserPostDto dto, int authorId)
    {
        var post = new UserPost
        {
            Slug = Guid.NewGuid().ToString().Replace("-", "")[..16],
            AuthorId = authorId,
            CommentAvailability = dto.CommentAvailability ?? true
        };
        if (dto.Content != null) post.Content = dto.Content;
        if (dto.GroupId != null) post.GroupId = dto.GroupId;
        if (dto.Media != null) post.Media = dto.Media;
        return post;
    }

    public static UserPostDto ToDto(this UserPost post)
    {
        return new UserPostDto
        (
            post.Id,
            post.Slug,
            post.CommentAvailability,
            post.Content,
            post.Media?.ToArray(),
            "" + post.Author?.Fullname,
            "" + post.Author?.Username,
            post.Group?.Name,
            post.Group?.Slug,
            post.Comments?.Count ?? 0,
            post.Reactions?.Count ?? 0,
            post.CreatedAt,
            post.UpdatedAt
        );
    }

    public static UserPostFullDto ToFullDto(this UserPost post)
    {
        return new UserPostFullDto
        (
            post.Slug,
            post.CommentAvailability,
            post.Content,
            post.Media?.ToArray(),
            "" + post.Author?.Fullname,
            "" + post.Author?.Username,
            post.Group?.Name,
            post.Group?.Slug,
            post.Comments?.Select(c => c.ToDto()).ToArray() ?? [],
            post.Reactions?.Select(r => r.ToDto()).ToArray() ?? [],
            post.CreatedAt,
            post.UpdatedAt
        );
    }
}
