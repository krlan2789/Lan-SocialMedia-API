using System.ComponentModel.DataAnnotations;
using LanGeng.API.Enums;

namespace LanGeng.API.Entities;

public class PostMedia
{
    [Key]
    public int Id { get; set; }

    public required string Path { get; set; }
    public required MediaTypeEnum MediaType { get; set; }

    [Required]
    public int PostId { get; set; }
    public UserPost? Post { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
