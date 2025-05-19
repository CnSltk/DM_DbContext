namespace WebApplication1.DTO;

public class EmployeeDetailDto
{
    // Person data
    public string PassportNumber { get; set; } = null!;
    public string FirstName      { get; set; } = null!;
    public string? MiddleName    { get; set; }
    public string LastName       { get; set; } = null!;
    public string PhoneNumber    { get; set; } = null!;
    public string Email          { get; set; } = null!;

    // Employee data
    public decimal Salary        { get; set; }
    public PositionDto Position  { get; set; } = null!;
    public DateTime HireDate     { get; set; }
}