using BigSolutions.VacaFlow.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence.Configurations;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.EmployeeId)
            .HasConversion(id => id.Value, value => new EmployeeId(value));

        builder.HasIndex(account => account.EmployeeId).IsUnique();

        builder.HasOne<Employee>()
            .WithOne()
            .HasForeignKey<UserAccount>(account => account.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(account => account.PasswordHash).IsRequired();

        builder.Property(account => account.CreatedAtUtc).IsRequired();
    }
}
