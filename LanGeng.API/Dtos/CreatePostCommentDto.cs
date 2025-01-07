namespace LanGeng.API.Dtos;

public record class CreatePostCommentDto
(
    string Content,
    int? ReplyId = null
);