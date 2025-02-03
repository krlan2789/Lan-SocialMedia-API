using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LanGeng.API.Dtos;

public record class FilterPostDto
(
    [Range(0, short.MaxValue)] short Page,
    [Range(1, 64)] byte? Limit = null,
    [StringLength(255)] string? Author = null,
    [StringLength(255)] string? Group = null,
    string? Keyword = null,
    string? Tags = null
);