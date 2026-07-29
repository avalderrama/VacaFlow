using BigSolutions.VacaFlow.Domain.Employees;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence;

/// <summary>
/// The technical credential record (Intent.md §7.1) — not a business entity,
/// so it has no domain type behind it. Internal to Infrastructure: only
/// CredentialStore ever constructs or queries it (SAD.md §7.2).
/// </summary>
internal sealed class UserAccount
{
    public required Guid Id { get; init; }

    public required EmployeeId EmployeeId { get; init; }

    public required string PasswordHash { get; init; }

    public required DateTime CreatedAtUtc { get; init; }
}
