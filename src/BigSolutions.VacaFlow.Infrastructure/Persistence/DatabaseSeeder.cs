using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using Microsoft.EntityFrameworkCore;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence;

/// <summary>
/// Seeds the absence type catalog and the three demo accounts fixed by
/// Backlog.md §3.6 (TE-003). Runs after migrations, in the same startup
/// step (SAD.md §7.5) — there is no separate seeding port; a business
/// use case would need one, but "populate fixed demo data" is a technical
/// concern of Infrastructure, not a consumer Application needs to see.
/// </summary>
/// <remarks>
/// Every write goes through the same domain factories, password hasher and
/// credential store a real registration would use (D4) — the seeded
/// accounts are indistinguishable from ones a user created by hand, and
/// idempotent by Code/Email so a restart against an existing database never
/// duplicates them (FR-DAT-004). Employee lookups and inserts go through
/// IEmployeeRepository rather than the DbContext directly, reusing the same
/// Email comparison EmployeeRepositoryTests already guards instead of a
/// second copy of it.
/// </remarks>
/// <remarks>
/// The existence check and the final SaveChangesAsync are not atomic, so two
/// API instances starting at the same instant against the same file could
/// both pass the check before either commits. That is an accepted risk, not
/// an oversight: this is a single-process local SQLite file (no deployment
/// target runs multiple instances), and the unique indexes on Code and Email
/// turn the hypothetical race into a startup crash — loud and immediate,
/// never silent duplicate rows. A crash here happens before
/// UseExceptionHandler is wired (Program.cs), so it never reaches a client.
/// This is precisely why the final commit goes through
/// dbContext.SaveChangesAsync and not IUnitOfWork: UnitOfWork exists to
/// translate a unique-constraint violation into a Result the caller can act
/// on, and this method has no caller that would consume that Result — doing
/// so here would turn the one race this remark relies on being loud back
/// into a swallowed failure.
/// </remarks>
internal sealed class DatabaseSeeder(
    VacaFlowDbContext dbContext,
    IEmployeeRepository employees,
    IPasswordHasher passwordHasher,
    ICredentialStore credentialStore,
    IIdGenerator idGenerator)
{
    // Fixed by Backlog.md §3.6 for the MVP demo, sanctioned there and in
    // SAD.md §7.5. Must not survive into any deployed build (FUT-30) — the
    // same requirement the §3.5 TEST ACCOUNTS block on S-01 carries.
    private const string ManagerPassword = "Manager123!";
    private const string EmployeePassword = "Employee123!";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedAbsenceTypesAsync(cancellationToken);
        var managerId = await SeedManagerAsync(cancellationToken);
        await SeedEmployeeAsync("Carlos Ruiz", "employee@vacaflow.test", managerId, cancellationToken);
        await SeedEmployeeAsync("Ana Torres", "ana@vacaflow.test", managerId, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAbsenceTypesAsync(CancellationToken cancellationToken)
    {
        (AbsenceTypeCode Code, string Name)[] catalog =
        [
            (AbsenceTypeCode.Vacation, "Vacation"),
            (AbsenceTypeCode.PersonalLeave, "Personal Leave"),
            (AbsenceTypeCode.SickLeave, "Sick Leave"),
        ];

        foreach (var (code, name) in catalog)
        {
            var exists = await dbContext.AbsenceTypes
                .AnyAsync(absenceType => absenceType.Code == code, cancellationToken);

            if (exists)
            {
                continue;
            }

            dbContext.AbsenceTypes.Add(
                AbsenceType.Create(new AbsenceTypeId(idGenerator.NewId()), code, name).Value);
        }
    }

    private async Task<EmployeeId> SeedManagerAsync(CancellationToken cancellationToken)
    {
        var email = Email.Create("manager@vacaflow.test").Value;

        var existing = await employees.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var manager = Employee.Create(
            new EmployeeId(idGenerator.NewId()), "Laura Méndez", email, EmployeeRole.Manager).Value;

        employees.Add(manager);
        credentialStore.Add(manager.Id, passwordHasher.Hash(ManagerPassword));

        return manager.Id;
    }

    private async Task SeedEmployeeAsync(
        string fullName, string emailValue, EmployeeId managerId, CancellationToken cancellationToken)
    {
        var email = Email.Create(emailValue).Value;

        if (await employees.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return;
        }

        var employee = Employee.Create(new EmployeeId(idGenerator.NewId()), fullName, email, EmployeeRole.Employee).Value;

        // managerId always names the real seeded manager, so the only
        // failure AssignManager can report — assigning someone to
        // themselves — cannot happen here. Asserted rather than trusted:
        // a silently unassigned manager would fail AC2 with no error.
        if (employee.AssignManager(managerId).IsFailure)
        {
            throw new InvalidOperationException($"Seeding {emailValue}: manager assignment failed.");
        }

        employees.Add(employee);
        credentialStore.Add(employee.Id, passwordHasher.Hash(EmployeePassword));
    }
}
