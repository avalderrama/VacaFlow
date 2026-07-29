using BigSolutions.VacaFlow.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API mapping only — the domain carries no persistence attribute
/// (CA-DOM-001). One configuration per aggregate (CA-INF-002).
/// </summary>
internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(employee => employee.Id);

        builder.Property(employee => employee.Id)
            .HasConversion(id => id.Value, value => new EmployeeId(value))
            .ValueGeneratedNever();

        builder.Property(employee => employee.FullName)
            .HasMaxLength(120)
            .IsRequired();

        // Reading back through Email.Create() re-validates the stored value.
        // That is deliberate: a row that fails to parse means corrupt data,
        // and surfacing that as a failure beats silently trusting the column.
        builder.Property(employee => employee.Email)
            .HasConversion(email => email.Value, value => Email.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(employee => employee.Email).IsUnique();

        builder.Property(employee => employee.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(employee => employee.IsActive)
            .IsRequired();

        builder.Property(employee => employee.ManagerId)
            .HasConversion(
                managerId => managerId.HasValue ? managerId.Value.Value : (Guid?)null,
                value => value.HasValue ? new EmployeeId(value.Value) : (EmployeeId?)null);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(employee => employee.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
