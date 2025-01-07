using System.ComponentModel.DataAnnotations;

namespace LanGeng.API.Entities;

public class PostHashtag
{
    [Required]
    public int PostId { get; set; }
    public UserPost? Post { get; set; }

    [Required]
    public int HashtagId { get; set; }
    public Hashtag? Hashtag { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
