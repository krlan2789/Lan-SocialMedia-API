namespace LanGeng.API.Dtos;

public record class PostReactionDto
(
    int Id,
    string PostSlug,
    string Username,
    string Fullname,
    byte Type
);

