using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WebApplication1.Context;
using WebApplication1.DTO;
using WebApplication1.Helper;
using WebApplication1.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DeviceManager") 
                       ?? throw new InvalidOperationException("No connection string found");

builder.Services.AddDbContext<DeviceContext>(options => 
    options.UseSqlServer(connectionString));
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSection);
var jwtSettings = jwtSection.Get<JwtSettings>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken            = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer=true,
            ValidateAudience=true,
            ValidateLifetime=true,
            ValidateIssuerSigningKey=true,
            ValidIssuer=jwtSettings.Issuer,
            ValidAudience=jwtSettings.Audience,
            IssuerSigningKey=new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly",  policy => policy.RequireRole("User"));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DeviceContext>();
    if (!await db.Roles.AnyAsync())
    {
        db.Roles.AddRange(
            new Role { Name = "Admin" },
            new Role { Name = "User" }
        );
        await db.SaveChangesAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseHttpsRedirection();
app.UseAuthorization();
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
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

//GET DEVICE BY ID
app.MapGet("/api/devices/{id:int}", async (int id, DeviceContext db, CancellationToken ct) =>
{
    try
    {
        var raw = await db.Devices
            .Include(d => d.DeviceType)
            .Include(d => d.DeviceEmployees.Where(de => de.ReturnDate == null))
            .ThenInclude(de => de.Employee).ThenInclude(e => e.Person)
            .Where(d => d.Id == id)
            .Select(d => new
            {
                DeviceTypeName       = d.DeviceType.Name,
                IsEnabled            = d.IsEnabled,
                AdditionalProperties = d.AdditionalProperties,
                Employee = d.DeviceEmployees
                    .Where(de => de.ReturnDate == null)
                    .Select(de => new EmployeeDto
                    {
                        Id   = de.Employee.Id,
                        Name = de.Employee.Person.FirstName + " " + de.Employee.Person.LastName
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);
        if (raw == null)
            return Results.NotFound();
        var jsonString = string.IsNullOrWhiteSpace(raw.AdditionalProperties)
            ? "{}"
            : raw.AdditionalProperties;
        var detail = new DeviceDetailsDto
        {
            DeviceTypeName       = raw.DeviceTypeName,
            IsEnabled            = raw.IsEnabled,
            AdditionalProperties = JsonSerializer.Deserialize<object>(jsonString)!,
            Employee             = raw.Employee
        };
        return Results.Ok(detail);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});


// POST CREATE DEVICE
app.MapPost("/api/devices", async ([FromBody] DeviceCreateDto dto, DeviceContext db, CancellationToken ct) =>
{
    try
    {
        var type = await db.DeviceTypes
            .SingleOrDefaultAsync(t => t.Name == dto.DeviceTypeName, ct);
        if (type == null)
            return Results.BadRequest($"Unknown DeviceType '{dto.DeviceTypeName}'.");
        var json = dto.AdditionalProperties == null
            ? "{}"
            : JsonSerializer.Serialize(dto.AdditionalProperties);
        var device = new Device {
            Name                 = dto.Name,
            IsEnabled            = dto.IsEnabled,
            AdditionalProperties = json,
            DeviceTypeId         = type.Id
        };
        db.Devices.Add(device);
        await db.SaveChangesAsync(ct);
        return Results.Created(
            $"/api/devices/{device.Id}", 
            new DeviceDto { Id = device.Id, Name = device.Name }
        );
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// PUT UPDATE DEVICE
app.MapPut("/api/devices/{id:int}", async (int id, [FromBody] DeviceCreateDto dto, DeviceContext db, CancellationToken ct) =>
{
    try
    {
        var device = await db.Devices.FindAsync(new object[]{ id }, ct);
        if (device == null) 
            return Results.NotFound();
        var type = await db.DeviceTypes
            .SingleOrDefaultAsync(t => t.Name == dto.DeviceTypeName, ct);
        if (type == null)
            return Results.BadRequest($"Unknown DeviceType '{dto.DeviceTypeName}'.");
        var json = dto.AdditionalProperties == null
            ? "{}"
            : JsonSerializer.Serialize(dto.AdditionalProperties);
        device.Name                 = dto.Name;
        device.IsEnabled            = dto.IsEnabled;
        device.AdditionalProperties = json;
        device.DeviceTypeId         = type.Id;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
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
        return Results.Problem(detail: ex.Message, statusCode: 500);
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
                Name = e.Person.FirstName + " " + e.Person.LastName
            })
            .ToListAsync(ct);
        return Results.Ok(list);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
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
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});


//----------ACCOUNTS-------------------

// POST LOGIN /api/auth
app.MapPost("/api/auth", async ([FromBody] AccountLoginDto dto, DeviceContext db, IOptions<JwtSettings> jwtOpts, CancellationToken ct) =>
{
    var account = await db.Accounts
        .Include(a => a.Role)
        .SingleOrDefaultAsync(a => a.Username == dto.Username, ct);
    if (account == null)
        return Results.Unauthorized();
    using var hmac = new HMACSHA512(account.PasswordSalt);
    var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password));
    if (!computedHash.SequenceEqual(account.PasswordHash))
        return Results.Unauthorized();
    var jwtSettings = jwtOpts.Value!;
    var keyBytes = Encoding.UTF8.GetBytes(jwtSettings.Key);
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, account.EmployeeId.ToString()),
        new Claim(ClaimTypes.Name, account.Username),
        new Claim(ClaimTypes.Role, account.Role.Name)
    };

    var creds = new SigningCredentials(
        new SymmetricSecurityKey(keyBytes),
        SecurityAlgorithms.HmacSha512Signature);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiresInMinutes),
        Issuer = jwtSettings.Issuer,
        Audience = jwtSettings.Audience,
        SigningCredentials = creds
    };

    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var tokenString = tokenHandler.WriteToken(token);

    return Results.Ok(new { token = tokenString });
});

//POST CREATE ACCOUNTS
app.MapPost("/api/accounts", async (
        [FromBody] AccountCreateDto dto,
        DeviceContext db,
        CancellationToken ct) =>
    {
        try
        {
            var employee = await db.Employees.FindAsync(new object[] { dto.EmployeeId }, ct);
            if (employee == null)
                return Results.BadRequest($"Employee {dto.EmployeeId} does not exist.");
            if (await db.Accounts.AnyAsync(a => a.Username == dto.Username, ct))
                return Results.BadRequest("Username is already taken.");
            var roleName = string.IsNullOrWhiteSpace(dto.RoleName)
                ? "User"
                : dto.RoleName;

            var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == roleName, ct);
            if (role == null)
                return Results.BadRequest($"Role '{roleName}' not found.");
            using var hmac = new HMACSHA512();
            var salt = hmac.Key;
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dto.Password));
            var account = new Account
            {
                Username     = dto.Username,
                PasswordHash = hash,
                PasswordSalt = salt,
                EmployeeId   = dto.EmployeeId,
                RoleId       = role.Id
            };
            db.Accounts.Add(account);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/accounts/{account.Id}", new AccountDto
            {
                Id         = account.Id,
                Username   = account.Username,
                RoleName   = role.Name,
                EmployeeId = account.EmployeeId
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    })
    .RequireAuthorization("AdminOnly");
// ─── GET ALL ACCOUNTS (AdminOnly) ───────────────────────────────────────────
app.MapGet("/api/accounts", async (DeviceContext db, CancellationToken ct) =>
    {
        var list = await db.Accounts
            .Include(a => a.Role)
            .Select(a => new AccountDto
            {
                Id = a.Id,
                Username = a.Username,
                RoleName = a.Role.Name,
                EmployeeId = a.EmployeeId
            })
            .ToListAsync(ct);

        return Results.Ok(list);
    })
    .RequireAuthorization("AdminOnly");
// ─── GET ACCOUNT BY ID (AdminOnly) ──────────────────────────────────────────
app.MapGet("/api/accounts/{id:int}", async (
        int id,
        DeviceContext db,
        CancellationToken ct) =>
    {
        var acc = await db.Accounts
            .Include(a => a.Role)
            .Where(a => a.Id == id)
            .Select(a => new AccountDto
            {
                Id = a.Id,
                Username = a.Username,
                RoleName = a.Role.Name,
                EmployeeId = a.EmployeeId
            })
            .FirstOrDefaultAsync(ct);
        return acc != null
            ? Results.Ok(acc)
            : Results.NotFound();
    })
    .RequireAuthorization("AdminOnly");
// ─── PUT UPDATE ACCOUNT (AdminOnly) ─────────────────────────────────────────
app.MapPut("/api/accounts/{id:int}", async (int id, [FromBody] AccountUpdateDto dto, DeviceContext db, CancellationToken ct) =>
    {
        var account = await db.Accounts.FindAsync(new object[] { id }, ct);
        if (account == null)
            return Results.NotFound();
        var newRole = await db.Roles.SingleOrDefaultAsync(r => r.Name == dto.RoleName, ct);
        if (newRole == null)
            return Results.BadRequest($"Role '{dto.RoleName}' not found.");
        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            using var hmac = new HMACSHA512();
            account.PasswordSalt = hmac.Key;
            account.PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dto.NewPassword));
        }
        account.RoleId = newRole.Id;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    })
    .RequireAuthorization("AdminOnly");

// ─── DELETE ACCOUNT (AdminOnly) ──────────────────────────────────────────────
app.MapDelete("/api/accounts/{id:int}", async (int id, DeviceContext db,CancellationToken ct) =>
    {
        var account = await db.Accounts.FindAsync(new object[] { id }, ct);
        if (account == null)
            return Results.NotFound();
        db.Accounts.Remove(account);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    })
    .RequireAuthorization("AdminOnly");

app.Run();