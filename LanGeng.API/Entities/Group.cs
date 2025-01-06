using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Entities;

[Index(nameof(Slug), IsUnique = true)]
public class Group
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public required string Name { get; set; }

    [Required, MaxLength(255)]
    public required string Slug { get; set; }

    public string? ProfileImage { get; set; }
    public string? Description { get; set; }

    [Required]
    public int CreatorId { get; set; }
    public User? Creator { get; set; }

    public ICollection<GroupMember>? Members { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Group()
    {
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }
}
