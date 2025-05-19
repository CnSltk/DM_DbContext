namespace WebApplication1.DTO;

public class DeviceDetailDto
{
    public string DeviceTypeName { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public object AdditionalProperties { get; set; } = new { };
    public CurrentEmployeeDto? CurrentEmployee { get; set; }
}