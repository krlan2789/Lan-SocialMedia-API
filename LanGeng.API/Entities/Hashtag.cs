using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Entities;

[Index(nameof(Tag), IsUnique = true)]
public class Hashtag
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public required string Tag { get; set; }

    public ICollection<PostHashtag> PostHashtags { get; set; } = [];
    public ICollection<UserPost> Posts { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
