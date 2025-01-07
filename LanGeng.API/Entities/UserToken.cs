using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Entities;

[Index(nameof(Token), IsUnique = true)]
public class UserToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string Token { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime ExpiresDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
