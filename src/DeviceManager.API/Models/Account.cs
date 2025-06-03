namespace WebApplication1.Models;

public class Account
{
    public int    Id           { get; set; }
    public string Username     { get; set; } = null!; 
    public byte[] PasswordHash { get; set; } = null!;
    public byte[] PasswordSalt { get; set; } = null!;
    public int      EmployeeId { get; set; }
    public Employee Employee   { get; set; } = null!;
    public int  RoleId { get; set; }
    public Role Role   { get; set; } = null!;
}