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
            Content = dto.Content,
            Media = dto.Media,
            AuthorId = authorId,
        };
        post.CommentAvailability = dto.CommentAvailability ?? true;
        if (dto.GroupId != null) post.GroupId = dto.GroupId;
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
}
