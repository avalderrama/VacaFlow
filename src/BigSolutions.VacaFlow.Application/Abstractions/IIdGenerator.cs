namespace BigSolutions.VacaFlow.Application.Abstractions;

/// <summary>
/// Generates identifiers for new aggregates. Exists because the domain cannot
/// call Guid.NewGuid() itself (CA-DOM-009) — creating an identifier is
/// infrastructural, and this port also makes handler tests deterministic.
/// Not anticipated by SAD.md §6.3; added by US-007 (see the plan's §5, S5).
/// </summary>
public interface IIdGenerator
{
    Guid NewId();
}
