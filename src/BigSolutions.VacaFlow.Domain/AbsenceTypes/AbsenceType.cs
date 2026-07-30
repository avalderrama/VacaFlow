using BigSolutions.VacaFlow.Domain.AbsenceTypes.Errors;
using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Domain.AbsenceTypes;

/// <summary>
/// A kind of absence an employee can request (Intent.md §7.1). Seeded
/// catalog, read-only at runtime — no story in the MVP activates,
/// deactivates or renames one, so this aggregate carries no such behavior
/// (SAD.md §5.1).
/// </summary>
public sealed class AbsenceType : AggregateRoot<AbsenceTypeId>
{
    private const int MaxNameLength = 120;

    private AbsenceType(AbsenceTypeId id, AbsenceTypeCode code, string name)
        : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
    }

    /// <summary>Required by EF Core. Never call it from application code (CA-DOM-002).</summary>
    private AbsenceType()
    {
        Code = null!;
        Name = string.Empty;
    }

    public AbsenceTypeCode Code { get; private set; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public static Result<AbsenceType> Create(AbsenceTypeId id, AbsenceTypeCode code, string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength)
        {
            return Result.Failure<AbsenceType>(AbsenceTypeErrors.NameRequired);
        }

        return Result.Success(new AbsenceType(id, code, name.Trim()));
    }
}
