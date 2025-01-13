using System.ComponentModel.DataAnnotations;

namespace LanGeng.API.Entities;

public class PostComment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string Content { get; set; }

    [Required]
    public int UserId { get; set; }
    [Required]
    public User? User { get; set; }

    [Required]
    public int PostId { get; set; }
    public UserPost? Post { get; set; }

    public int? ReplyId { get; set; }
    public PostComment? Reply { get; set; }

    public ICollection<CommentReaction> Reactions { get; set; } = [];

    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
