using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Context
{
    public class DeviceContext : DbContext
    {
        public DeviceContext(DbContextOptions<DeviceContext> options)
            : base(options)
        {
        }

        public DbSet<DeviceType> DeviceTypes { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<DeviceEmployee> DeviceEmployees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // DeviceType
            modelBuilder.Entity<DeviceType>(entity =>
            {
                entity.ToTable("DeviceType");
                entity.HasKey(e => e.Id).HasName("PK_DeviceType");
                entity.Property(e => e.Name)
                      .HasMaxLength(100)
                      .IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Position
            modelBuilder.Entity<Position>(entity =>
            {
                entity.ToTable("Position");
                entity.HasKey(e => e.Id).HasName("PK_Position");
                entity.Property(e => e.Name)
                      .HasMaxLength(100)
                      .IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.MinExpYears)
                      .IsRequired();
            });

            // Person
            modelBuilder.Entity<Person>(entity =>
            {
                entity.ToTable("Person");
                entity.HasKey(e => e.Id).HasName("PK_Person");
                entity.Property(e => e.PassportNumber)
                      .HasMaxLength(30)
                      .IsRequired();
                entity.HasIndex(e => e.PassportNumber).IsUnique();

                entity.Property(e => e.FirstName)
                      .HasMaxLength(100)
                      .IsRequired();
                entity.Property(e => e.MiddleName)
                      .HasMaxLength(100)
                      .IsRequired(false);
                entity.Property(e => e.LastName)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(e => e.PhoneNumber)
                      .HasMaxLength(20)
                      .IsRequired();
                entity.HasIndex(e => e.PhoneNumber).IsUnique();

                entity.Property(e => e.Email)
                      .HasMaxLength(150)
                      .IsRequired();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Device
            modelBuilder.Entity<Device>(entity =>
            {
                entity.ToTable("Device");
                entity.HasKey(e => e.Id).HasName("PK_Device");

                entity.Property(e => e.Name)
                      .HasMaxLength(150)
                      .IsRequired();
                entity.Property(e => e.IsEnabled)
                      .IsRequired()
                      .HasDefaultValue(true);
                entity.Property(e => e.AdditionalProperties)
                      .HasColumnType("varchar(8000)")
                      .IsRequired()
                      .HasDefaultValue("");

                entity.HasOne(e => e.DeviceType)
                      .WithMany(dt => dt.Devices)
                      .HasForeignKey(e => e.DeviceTypeId)
                      .HasConstraintName("FK_Device_DeviceType");
            });

            // Employee
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.ToTable("Employee");
                entity.HasKey(e => e.Id).HasName("PK_Employee");

                entity.Property(e => e.Salary)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();
                entity.Property(e => e.HireDate)
                      .IsRequired()
                      .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.Position)
                      .WithMany(p => p.Employees)
                      .HasForeignKey(e => e.PositionId)
                      .HasConstraintName("FK_Employee_Position");

                entity.HasOne(e => e.Person)
                      .WithMany(p => p.Employees)
                      .HasForeignKey(e => e.PersonId)
                      .HasConstraintName("FK_Employee_Person");
            });

            // DeviceEmployee
            modelBuilder.Entity<DeviceEmployee>(entity =>
            {
                entity.ToTable("DeviceEmployee");
                entity.HasKey(e => e.Id).HasName("PK_DeviceEmployee");

                entity.Property(e => e.IssueDate)
                      .IsRequired()
                      .HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.ReturnDate)
                      .IsRequired(false);

                entity.HasOne(de => de.Device)
                      .WithMany(d => d.DeviceEmployees)
                      .HasForeignKey(de => de.DeviceId)
                      .HasConstraintName("FK_DeviceEmployee_Device");

                entity.HasOne(de => de.Employee)
                      .WithMany(e => e.DeviceEmployees)
                      .HasForeignKey(de => de.EmployeeId)
                      .HasConstraintName("FK_DeviceEmployee_Employee");
            });
        }
    }
}
