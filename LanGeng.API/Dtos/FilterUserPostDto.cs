using System.ComponentModel.DataAnnotations;

namespace LanGeng.API.Dtos;

public record class FilterUserPostDto
(
    [Range(1, int.MaxValue)] int Page,
    [Range(1, 64)] int? Limit,
    [StringLength(255)] string? AuthorUsername,
    string? Keyword
);