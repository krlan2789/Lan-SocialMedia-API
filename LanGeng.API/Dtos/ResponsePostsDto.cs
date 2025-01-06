namespace LanGeng.API.Dtos;

public record class ResponsePostsDto
(
    int Page,
    int Limit,
    int Total,
    IEnumerable<UserPostDto> Posts
);