using BigSolutions.VacaFlow.Application.Abstractions;

namespace BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;

internal sealed class FakeIdGenerator(Guid fixedId) : IIdGenerator
{
    public Guid NewId() => fixedId;
}
