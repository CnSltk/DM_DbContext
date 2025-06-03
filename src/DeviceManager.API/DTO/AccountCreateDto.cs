using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTO;

public class AccountCreateDto
{
    [Required]
    [RegularExpression(@"^[^\d]\S*$", ErrorMessage = "Username must not start with a number.")]
    [StringLength(100, MinimumLength = 1)]
    public string Username { get; set; } = null!;
    [Required]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).+$",
        ErrorMessage = "Password must contain at least one lowercase, one uppercase, one digit and one symbol.")]
    public string Password { get; set; } = null!;
    [Required]
    public int EmployeeId { get; set; }
    public string? RoleName { get; set; }
}