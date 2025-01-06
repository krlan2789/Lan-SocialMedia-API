namespace LanGeng.API.Dtos;

public record class PostCommentDto
(
    int Id,
    string Username,
    string Fullname,
    string Content,
    string Date,
    int? ReplyId
);