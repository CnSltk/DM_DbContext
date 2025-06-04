using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class Account
{
    public int Id { get; set; }
    [Required]
    [StringLength(100)]
    public string Username{ get; set; } = null!;
    [Required]
    public byte[] PasswordHash { get; set; } = null!;
    [Required]
    public byte[] PasswordSalt { get; set; } = null!;
    [Required]
    public int EmployeeId { get; set; }
    public  Employee Employee{ get; set; } = null!;
    [Required]
    public int RoleId { get; set; }
    public Role Role{ get; set; } = null!;
}