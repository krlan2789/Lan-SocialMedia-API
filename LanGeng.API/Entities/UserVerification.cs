using System.ComponentModel.DataAnnotations;
using LanGeng.API.Enums;

namespace LanGeng.API.Entities;

public class UserVerification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required VerificationTypeEnum VerificationType { get; set; }

    [Required, MaxLength(6)]
    public string Code { get; set; } = $"{new Random().Next(111, 999999):000000}";

    [MaxLength(32)]
    public string? PhoneNumber { get; set; }

    [MaxLength(128)]
    public string? Email { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime ExpiresDate { get; set; } = DateTime.Now.AddMinutes(8);
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
