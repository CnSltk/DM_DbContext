using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTO;

    public class AccountLoginDto
    {
        [Required]
        public string Username { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
    }
