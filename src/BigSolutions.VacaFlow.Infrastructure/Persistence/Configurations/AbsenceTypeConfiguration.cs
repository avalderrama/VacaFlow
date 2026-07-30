using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API mapping only — the domain carries no persistence attribute
/// (CA-DOM-001). One configuration per aggregate (CA-INF-002).
/// </summary>
internal sealed class AbsenceTypeConfiguration : IEntityTypeConfiguration<AbsenceType>
{
    public void Configure(EntityTypeBuilder<AbsenceType> builder)
    {
        builder.ToTable("AbsenceTypes");

        builder.HasKey(absenceType => absenceType.Id);

        builder.Property(absenceType => absenceType.Id)
            .HasConversion(id => id.Value, value => new AbsenceTypeId(value))
            .ValueGeneratedNever();

        // Reading back through AbsenceTypeCode.Create() re-validates the
        // stored value, same reasoning as Employee.Email's converter: a row
        // that fails to parse means corrupt data, not a value to trust blindly.
        builder.Property(absenceType => absenceType.Code)
            .HasConversion(code => code.Value, value => AbsenceTypeCode.Create(value).Value)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(absenceType => absenceType.Code).IsUnique();

        builder.Property(absenceType => absenceType.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(absenceType => absenceType.IsActive)
            .IsRequired();
    }
}
