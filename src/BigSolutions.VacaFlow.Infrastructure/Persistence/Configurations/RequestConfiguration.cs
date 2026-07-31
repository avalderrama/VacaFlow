using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API mapping only — the domain carries no persistence attribute
/// (CA-DOM-001). One configuration per aggregate (CA-INF-002).
/// </summary>
internal sealed class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.ToTable("Requests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Id)
            .HasConversion(id => id.Value, value => new RequestId(value))
            .ValueGeneratedNever();

        builder.Property(request => request.OwnerId)
            .HasConversion(ownerId => ownerId.Value, value => new EmployeeId(value))
            .HasColumnName("EmployeeId")
            .IsRequired();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(request => request.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(request => request.AbsenceTypeId)
            .HasConversion(absenceTypeId => absenceTypeId.Value, value => new AbsenceTypeId(value))
            .IsRequired();

        builder.HasOne<AbsenceType>()
            .WithMany()
            .HasForeignKey(request => request.AbsenceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(request => request.Period, period =>
        {
            period.Property(range => range.Start).HasColumnName("StartDate").IsRequired();
            period.Property(range => range.End).HasColumnName("EndDate").IsRequired();
        });

        builder.Property(request => request.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(request => request.State)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(request => request.CreatedAtUtc).IsRequired();
        builder.Property(request => request.UpdatedAtUtc).IsRequired();
        builder.Property(request => request.SubmittedAtUtc);
        builder.Property(request => request.ClosedAtUtc);

        builder.HasIndex(request => new { request.OwnerId, request.State });

        // Owned in its own table, not the same-table style Period uses
        // above — Approval needs its own key, its own FK to Employees, and
        // a unique index on the owner key (RULE-09's safety net, not the
        // enforcement point — that lives in Request.Decide, CA-INF-003).
        builder.OwnsOne(request => request.Approval, approval =>
        {
            approval.ToTable("Approvals");

            approval.WithOwner().HasForeignKey("RequestId");

            approval.Property(a => a.Id)
                .HasConversion(id => id.Value, value => new ApprovalId(value));
            approval.HasKey(a => a.Id);

            approval.HasIndex("RequestId").IsUnique();

            approval.Property(a => a.ResponsibleManagerId)
                .HasConversion(managerId => managerId.Value, value => new EmployeeId(value))
                .IsRequired();

            approval.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(a => a.ResponsibleManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            approval.Property(a => a.Decision)
                .HasConversion<int>()
                .IsRequired();

            approval.Property(a => a.Comment).HasMaxLength(500);

            approval.Property(a => a.DecidedAtUtc).IsRequired();
        });
    }
}
