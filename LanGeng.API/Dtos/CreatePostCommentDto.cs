namespace LanGeng.API.Dtos;

public record class CreatePostCommentDto
(
    string Slug,
    string Content,
    int? ReplyId = null
);