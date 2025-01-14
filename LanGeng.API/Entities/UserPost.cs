using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Entities;

[Index(nameof(Slug), IsUnique = true)]
public class UserPost
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public required string Slug { get; set; }

    public bool CommentAvailability { get; set; } = true;
    public string? Content { get; set; }

    [Required]
    public int AuthorId { get; set; }
    public User? Author { get; set; }

    public int? GroupId { get; set; }
    public Group? Group { get; set; }

    public ICollection<PostMedia> Media { get; set; } = [];
    public ICollection<PostComment> Comments { get; set; } = [];
    public ICollection<PostReaction> Reactions { get; set; } = [];

    public ICollection<PostHashtag> PostHashtags { get; set; } = [];
    public ICollection<Hashtag> Hashtags { get; set; } = [];

    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
