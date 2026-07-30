using BigSolutions.VacaFlow.Domain.AbsenceTypes.Errors;
using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Domain.AbsenceTypes;

/// <summary>
/// The closed set of absence type codes fixed by Backlog.md §3.6. Known
/// values are exposed as statics so callers — the seeder included — never
/// build one from a magic string.
/// </summary>
public sealed class AbsenceTypeCode : ValueObject
{
    private static readonly string[] KnownValues = ["VACATION", "PERSONAL_LEAVE", "SICK_LEAVE"];

    private AbsenceTypeCode(string value) => Value = value;

    public string Value { get; }

    public static AbsenceTypeCode Vacation { get; } = new("VACATION");

    public static AbsenceTypeCode PersonalLeave { get; } = new("PERSONAL_LEAVE");

    public static AbsenceTypeCode SickLeave { get; } = new("SICK_LEAVE");

    public static Result<AbsenceTypeCode> Create(string? value) =>
        value is not null && KnownValues.Contains(value)
            ? Result.Success(new AbsenceTypeCode(value))
            : Result.Failure<AbsenceTypeCode>(AbsenceTypeErrors.CodeInvalid);

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
