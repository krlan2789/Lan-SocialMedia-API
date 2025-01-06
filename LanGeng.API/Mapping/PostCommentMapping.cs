using System;
using LanGeng.API.Dtos;
using LanGeng.API.Entities;

namespace LanGeng.API.Mapping;

public static class PostCommentMapping
{
    // public static PostComment ToEntity(this CreateUserPostDto dto, int postId)
    // {
    //     var post = new UserPost
    //     {
    //         Slug = Guid.NewGuid().ToString().Replace("-", "")[..16],
    //         Content = dto.Content,
    //         Media = dto.Media,
    //         AuthorId = authorId,
    //     };
    //     post.CommentAvailability = dto.CommentAvailability ?? true;
    //     if (dto.GroupId != null) post.GroupId = dto.GroupId;
    //     return post;
    // }

    public static PostCommentDto ToDto(this PostComment comment)
    {
        return new PostCommentDto
        (
            comment.Id,
            "" + comment.User?.Username,
            "" + comment.User?.Fullname,
            comment.Content,
            comment.UpdatedAt.ToString(),
            comment.PostId
        );
    }
}
