using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTO;

public class DeviceCreateDto
{
    [Required] public string Name { get; set; } = null!;
    [Required] public string DeviceTypeName { get; set; } = null!;
    [Required] public bool   IsEnabled { get; set; }
    [Required] public object AdditionalProperties { get; set; } = new { };
}