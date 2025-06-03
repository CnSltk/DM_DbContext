namespace WebApplication1.DTO;

public class DeviceDetailsDto
{
    public string DeviceTypeName { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public object AdditionalProperties { get; set; } = new { };
    public EmployeeDto? Employee { get; set; }
}