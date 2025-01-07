using System.ComponentModel.DataAnnotations;
using LanGeng.API.Enums;

namespace LanGeng.API.Entities;

public class GroupMember
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required GroupMemberEnum Status { get; set; }

    [Required]
    public int GroupId { get; set; }
    [Required]
    public Group? Group { get; set; }

    [Required]
    public int MemberId { get; set; }
    public User? Member { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
