using BigSolutions.VacaFlow.Application.Abstractions;

namespace BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;

/// <summary>Deterministic, reversible stand-in — never the real algorithm (that is Pbkdf2PasswordHasherTests).</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    private const string Prefix = "fake-hash:";

    public string Hash(string password) => Prefix + password;

    public bool Verify(string password, string hash) => hash == Prefix + password;
}
