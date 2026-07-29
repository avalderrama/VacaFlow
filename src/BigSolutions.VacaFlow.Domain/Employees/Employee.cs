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
    /// The simple manager assignment (Intent.md BC-04). Null until OQ-01 is
    /// resolved — no registration flow sets this yet, and no fallback is
    /// encoded here (SAD.md §5.4).
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
}
