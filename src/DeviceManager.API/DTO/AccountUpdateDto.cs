using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTO;

public class AccountUpdateDto
{
    [Required] public string RoleName    { get; set; } = null!;
    public string? NewPassword { get; set; }
}