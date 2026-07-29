namespace BigSolutions.VacaFlow.Domain.Employees;

/// <summary>
/// Strongly-typed identifier for <see cref="Employee"/> (ADR-007). Carries no
/// factory that generates a value: creating a Guid is an infrastructural
/// concern (CA-DOM-009), so the identifier always arrives as a constructor
/// argument, sourced from IIdGenerator in the outer rings.
/// </summary>
public readonly record struct EmployeeId(Guid Value)
{
    public override string ToString() => Value.ToString();
}
