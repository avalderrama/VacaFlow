namespace BigSolutions.VacaFlow.Domain.Primitives;

/// <summary>
/// The entry point to an aggregate: the only member other aggregates may
/// reference, and the unit modified in a single transaction (CA-DOM-007).
/// One repository exists per aggregate root, never per child entity.
/// </summary>
/// <remarks>
/// It carries no domain-event machinery. VacaFlow raises no domain events —
/// there is no notification, projection or downstream consumer, and adding an
/// event bus for zero subscribers is exactly the machinery TC-06 forbids.
/// See SAD §5.6.
/// </remarks>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }
}
