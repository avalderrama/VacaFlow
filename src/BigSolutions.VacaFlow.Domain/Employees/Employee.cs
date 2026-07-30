using BigSolutions.VacaFlow.Domain.Employees.Errors;
using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Domain.Employees;

/// <summary>
/// The person participating in the process (Intent.md §7.1). Aggregate root:
/// identity and manager assignment change independently of any request
/// (SAD.md §5.1). Carries no notion of a password — credentials are technical
/// infrastructure, mapped to the separate UserAccount table (LC-02).
/// </summary>
public sealed class Employee : AggregateRoot<EmployeeId>
{
    private const int MaxFullNameLength = 120;

    private Employee(EmployeeId id, string fullName, Email email, EmployeeRole role)
        : base(id)
    {
        FullName = fullName;
        Email = email;
        Role = role;
        IsActive = true;
        ManagerId = null;
    }

    /// <summary>Required by EF Core. Never call it from application code (CA-DOM-002).</summary>
    private Employee()
    {
        FullName = string.Empty;
        Email = null!;
    }

    public string FullName { get; private set; }

    public Email Email { get; private set; }

    public EmployeeRole Role { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// The simple manager assignment (Intent.md BC-04). Null until something
    /// calls <see cref="AssignManager"/> — no registration flow does that yet
    /// (OQ-01 is still open), so the only caller in the MVP is the seeder
    /// (TE-003), which fixes it directly from Backlog.md §3.6.
    /// </summary>
    public EmployeeId? ManagerId { get; private set; }

    public static Result<Employee> Create(EmployeeId id, string? fullName, Email email, EmployeeRole role)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > MaxFullNameLength)
        {
            return Result.Failure<Employee>(EmployeeErrors.FullNameRequired);
        }

        return Result.Success(new Employee(id, fullName.Trim(), email, role));
    }

    /// <summary>
    /// Changes state independently of creation (SAD.md §5.1) — a mechanism,
    /// not a policy: it does not check that <paramref name="managerId"/>
    /// belongs to an employee with the Manager role, because validating that
    /// would require loading a second aggregate by identity (CA-DOM-007). The
    /// one caller in the MVP (the seeder) already knows it is assigning a
    /// Manager; a future registration flow that resolves OQ-01 would enforce
    /// that rule in its own handler, not here.
    /// </summary>
    public Result AssignManager(EmployeeId managerId)
    {
        if (managerId == Id)
        {
            return Result.Failure(EmployeeErrors.CannotBeOwnManager);
        }

        ManagerId = managerId;
        return Result.Success();
    }
}
