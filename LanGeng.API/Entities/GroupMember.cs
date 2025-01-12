using System.ComponentModel.DataAnnotations;
using LanGeng.API.Enums;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Entities;

[Index(nameof(Slug), IsUnique = true)]
public class GroupMember
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public required string Slug { get; set; }

    [Required]
    public required GroupMemberStatusEnum Status { get; set; }

    [Required]
    public int GroupId { get; set; }
    [Required]
    public Group? Group { get; set; }

    [Required]
    public int MemberId { get; set; }
    public User? Member { get; set; }

    public DateTime? JoinedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
