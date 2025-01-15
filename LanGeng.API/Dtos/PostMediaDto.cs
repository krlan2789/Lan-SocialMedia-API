using LanGeng.API.Enums;

namespace LanGeng.API.Dtos;

public record class PostMediaDto
(
    int Id,
    string Path,
    MediaTypeEnum MediaType
);