using System.Text.RegularExpressions;
using BigSolutions.VacaFlow.Domain.Employees.Errors;
using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Domain.Employees;

/// <summary>
/// A validated, normalized email address (CA-DOM-005). Normalizing to lower
/// case at construction is what lets a case-insensitive uniqueness check
/// (FR-AUT-002) be a plain UNIQUE index rather than a collation choice made in
/// Infrastructure.
/// </summary>
public sealed partial class Email : ValueObject
{
    private const int MaxLength = 200;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Email>(EmployeeErrors.EmailInvalid);
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength || !EmailPattern().IsMatch(trimmed))
        {
            return Result.Failure<Email>(EmployeeErrors.EmailInvalid);
        }

        return Result.Success(new Email(trimmed.ToLowerInvariant()));
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex EmailPattern();
}
