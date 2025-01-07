using System;
using System.ComponentModel.DataAnnotations;
using LanGeng.API.Enums;

namespace LanGeng.API.Entities;

public class CommentReaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public ReactionTypeEnum Type { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public int CommentId { get; set; }
    public PostComment? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
