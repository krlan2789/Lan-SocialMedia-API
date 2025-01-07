using System.ComponentModel.DataAnnotations;
using LanGeng.API.Enums;

namespace LanGeng.API.Entities;

public class UserStatus
{
    [Key]
    public int Id { get; set; }

    public AccountStatusEnum AccountStatus { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
