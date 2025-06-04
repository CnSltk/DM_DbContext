namespace WebApplication1.Models;

public class Role
{
    public int    Id   { get; set; }
    public string Name { get; set; } = null!;  // “Admin” or “User”
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}