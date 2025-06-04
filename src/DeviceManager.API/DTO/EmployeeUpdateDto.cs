using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTO;

public class EmployeeUpdateDto
{
    [Required]
    public string FirstName { get; set; } = null!;
    public string? MiddleName { get; set; }
    [Required]
    public string LastName { get; set; } = null!;
    [Required]
    public string PhoneNumber { get; set; } = null!;
    [Required]
    public string Email { get; set; } = null!;
    [Required]
    public decimal Salary { get; set; }
    [Required]
    public int PositionId { get; set; }
}