namespace BigSolutions.VacaFlow.Application.UnitTests.Requests.Fakes;

/// <summary>Same pattern as CredentialStoreTests.FixedTimeProvider (TE-004).</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
