using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LanGeng.API.Entities;

[Index(nameof(PhoneNumber), IsUnique = true)]
public class UserProfile
{
    [Key]
    public int Id { get; set; }

    public string? Bio { get; set; }
    public string? ProfileImage { get; set; }

    [MaxLength(32)]
    public string? PhoneNumber { get; set; }

    [MaxLength(255)]
    public string? CityBorn { get; set; }

    [MaxLength(255)]
    public string? CityHome { get; set; }

    public DateOnly? BirthDate { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
