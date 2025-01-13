using System.ComponentModel.DataAnnotations;
using LanGeng.API.Enums;
using LanGeng.API.Helper;

namespace LanGeng.API.Entities;

public class UserVerificationToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required VerificationTypeEnum VerificationType { get; set; }

    [Required]
    public string Token { get; set; } = SlugHelper.Create();

    [MaxLength(128)]
    public string? Email { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime ExpiresDate { get; set; } = DateTime.Now.AddMinutes(8);
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
