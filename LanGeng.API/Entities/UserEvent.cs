using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Entities;

[Index(nameof(Slug), IsUnique = true)]
public class UserEvent
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public required string Name { get; set; }

    [Required, MaxLength(255)]
    public required string Slug { get; set; }

    [Required, MaxLength(255)]
    public required string Location { get; set; }

    [MaxLength(2048)]
    public string? Description { get; set; }

    [Required]
    public required DateTime StartTime { get; set; }

    [Required]
    public required DateTime EndTime { get; set; }

    [Required]
    public int CreatorId { get; set; }
    public User? Creator { get; set; }

    public int? PostId { get; set; }
    public UserPost? Post { get; set; }

    public int? GroupId { get; set; }
    public Group? Group { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public UserEvent()
    {
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }
}
