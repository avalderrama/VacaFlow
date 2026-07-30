namespace BigSolutions.VacaFlow.Domain.AbsenceTypes;

/// <summary>
/// Strongly-typed identifier for <see cref="AbsenceType"/> (ADR-007). Carries
/// no factory that generates a value: creating a Guid is an infrastructural
/// concern (CA-DOM-009), so the identifier always arrives as a constructor
/// argument, sourced from IIdGenerator in the outer rings.
/// </summary>
public readonly record struct AbsenceTypeId(Guid Value)
{
    public override string ToString() => Value.ToString();
}
