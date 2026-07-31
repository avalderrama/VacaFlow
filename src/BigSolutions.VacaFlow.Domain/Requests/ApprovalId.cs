namespace BigSolutions.VacaFlow.Domain.Requests;

/// <summary>
/// Strongly-typed identifier for <see cref="Approval"/> (ADR-006). Mirrors
/// RequestId: no factory that generates a value — the Guid always arrives
/// as a constructor argument, sourced from IIdGenerator in the outer rings.
/// </summary>
public readonly record struct ApprovalId(Guid Value)
{
    public override string ToString() => Value.ToString();
}
