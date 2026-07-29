using BigSolutions.VacaFlow.Application.Abstractions;

namespace BigSolutions.VacaFlow.Infrastructure.Identifiers;

/// <summary>
/// Version 7 GUIDs are time-ordered, giving better primary-key index locality
/// than Guid.NewGuid() (version 4) without changing anything observable.
/// </summary>
internal sealed class GuidIdGenerator : IIdGenerator
{
    public Guid NewId() => Guid.CreateVersion7();
}
