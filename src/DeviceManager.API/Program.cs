using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1;
using WebApplication1.Context;
using WebApplication1.DTO;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DeviceManager") 
                       ?? throw new InvalidOperationException("No connection string found");

builder.Services.AddDbContext<DeviceContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

/*
 * --------------------
 * -----DEVICES------
 * --------------------
 */
//GET ALL DEVICES
app.MapGet("/api/devices", async (DeviceContext db, CancellationToken ct) =>
{
    try
    {
        var list = await db.Devices
            .Select(d => new DeviceDto {
                Id   = d.Id,
                Name = d.Name
            })
            .ToListAsync(ct);

        return Results.Ok(list);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, null,500);
    }
});

//GET DEVICE BY ID
app.MapGet("/api/devices/{id:int}", async (
    int id,
    DeviceContext db,
    CancellationToken ct) =>
{
    try
    {
        var raw = await db.Devices
            .Include(d => d.DeviceType)
            .Include(d => d.DeviceEmployees)
            .ThenInclude(de => de.Employee).ThenInclude(e => e.Person)
            .Where(d => d.Id == id)
            .Select(d => new {
                DeviceTypeName       = d.DeviceType.Name,
                IsEnabled            = d.IsEnabled,
                AdditionalProperties = d.AdditionalProperties,
                CurrentEmployee = d.DeviceEmployees
                    .Where(de => de.ReturnDate == null)
                    .Select(de => new CurrentEmployeeDto {
                        Id       = de.Employee.Id,
                        FullName = de.Employee.Person.FirstName + " " + de.Employee.Person.LastName
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        if (raw == null)
            return Results.NotFound();
        var detail = new DeviceDetailDto {
            DeviceTypeName       = raw.DeviceTypeName,
            IsEnabled            = raw.IsEnabled,
            AdditionalProperties = JsonSerializer.Deserialize<object>(raw.AdditionalProperties)!,
            CurrentEmployee      = raw.CurrentEmployee
        };

        return Results.Ok(detail);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});


//POST CREATE DEVICE
app.MapPost("/api/devices", async (
    [FromBody] DeviceCreateDto dto,
    DeviceContext db,
    CancellationToken ct) =>
{
    try
    {
        var type = await db.DeviceTypes
            .SingleOrDefaultAsync(t => t.Name == dto.DeviceTypeName, ct);
        if (type == null)
            return Results.BadRequest($"Unknown DeviceType '{dto.DeviceTypeName}'.");
        
        var device = new Device {
            Name                 = dto.Name,
            IsEnabled            = dto.IsEnabled,
            AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties),
            DeviceTypeId         = type.Id
        };
        
        db.Devices.Add(device);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/devices/{device.Id}", 
            new DeviceDto { Id   = device.Id, Name = device.Name
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, null,500);
    }
});

//PUT UPDATE DEVICE
app.MapPut("/api/devices/{id:int}", async (
    int id,
    [FromBody] DeviceCreateDto dto,
    DeviceContext db,
    CancellationToken ct) =>
{
    try
    {
        var device = await db.Devices.FindAsync(new object[]{id}, ct);
        if (device == null) return Results.NotFound();

        var type = await db.DeviceTypes
            .SingleOrDefaultAsync(t => t.Name == dto.DeviceTypeName, ct);
        if (type == null)
            return Results.BadRequest($"Unknown DeviceType '{dto.DeviceTypeName}'.");
        
        device.IsEnabled            = dto.IsEnabled;
        device.AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties);
        device.DeviceTypeId         = type.Id;

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, null,500);
    }
});

//DELETE DEVICE
app.MapDelete("/api/devices/{id:int}", async (int id, DeviceContext db, CancellationToken ct) =>
{
    try
    {
        var device = await db.Devices.FindAsync(new object[]{id}, ct);
        if (device == null) return Results.NotFound();

        db.Devices.Remove(device);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, null,500);
    }
});

/*
 * --------------------
 * -----EMPLOYEES------
 * --------------------
*/
//GET ALL EMPLOYEES
app.MapGet("/api/employees", async (DeviceContext db, CancellationToken ct) =>
{
    try
    {
        var list = await db.Employees
            .Include(e => e.Person)
            .Select(e => new EmployeeDto {
                Id       = e.Id,
                FullName = e.Person.FirstName + " " + e.Person.LastName
            })
            .ToListAsync(ct);

        return Results.Ok(list);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, null,500);
    }
});

//GET EMPLOYEE BY ID
app.MapGet("/api/employees/{id:int}", async (int id, DeviceContext db, CancellationToken ct) =>
{
    try
    {
        var emp = await db.Employees
            .Include(e => e.Person)
            .Include(e => e.Position)
            .Where(e => e.Id == id)
            .Select(e => new EmployeeDetailDto {
                PassportNumber = e.Person.PassportNumber,
                FirstName      = e.Person.FirstName,
                MiddleName     = e.Person.MiddleName,
                LastName       = e.Person.LastName,
                PhoneNumber    = e.Person.PhoneNumber,
                Email          = e.Person.Email,
                Salary         = e.Salary,
                Position       = new PositionDto { Id = e.Position.Id, Name = e.Position.Name },
                HireDate       = e.HireDate
            })
            .FirstOrDefaultAsync(ct);

        return emp != null
            ? Results.Ok(emp)
            : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, null,500);
    }
});








app.UseHttpsRedirection();
app.UseAuthorization();
app.Run();