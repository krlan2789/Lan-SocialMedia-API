namespace LanGeng.API.Dtos;

public record class ResponsePostsDto
(
    short Page,
    byte Limit,
    long Total,
    IEnumerable<UserPostDto> Posts
);