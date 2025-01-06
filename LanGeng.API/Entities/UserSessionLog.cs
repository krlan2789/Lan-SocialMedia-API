using System.ComponentModel.DataAnnotations;

namespace LanGeng.API.Entities;

public class UserSessionLog
{
    [Key]
    public int Id { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Action { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public DateTime CreatedAt { get; set; }

    public UserSessionLog()
    {
        CreatedAt = DateTime.Now;
    }
}
