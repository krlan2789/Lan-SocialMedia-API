using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Entities;

[Index(nameof(Email), IsUnique = true), Index(nameof(Username), IsUnique = true)]
public class User
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public required string Fullname { get; set; }

    [Required, MaxLength(64)]
    public required string Username { get; set; }

    [Required, MaxLength(128)]
    public required string Email { get; set; }

    [Required, MaxLength(255)]
    public required string PasswordHash { get; set; }

    public UserProfile? Profile { get; set; }
    public UserStatus? AccountStatus { get; set; }
    public ICollection<UserToken> UserTokens { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
