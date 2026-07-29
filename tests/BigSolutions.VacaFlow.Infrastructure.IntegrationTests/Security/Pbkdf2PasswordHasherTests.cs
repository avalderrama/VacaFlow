using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Infrastructure.IntegrationTests.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BigSolutions.VacaFlow.Infrastructure.IntegrationTests.Security;

/// <summary>
/// Pbkdf2PasswordHasher is internal (CA-DEP-007); reached only through the
/// IPasswordHasher port, resolved from the same real container the fixture
/// already builds for the persistence tests — it is a singleton with no
/// dependency on the database, so sharing the fixture costs nothing.
/// </summary>
public sealed class Pbkdf2PasswordHasherTests(SqliteDatabaseFixture fixture) : IClassFixture<SqliteDatabaseFixture>
{
    private IPasswordHasher Hasher => fixture.Services.GetRequiredService<IPasswordHasher>();

    [Fact]
    public void Hash_Should_Not_Contain_The_Plain_Text_Password()
    {
        const string password = "Sup3rSecret!";

        var hash = Hasher.Hash(password);

        Assert.DoesNotContain(password, hash, StringComparison.Ordinal);
    }

    [Fact]
    public void Hashing_The_Same_Password_Twice_Should_Produce_Different_Hashes()
    {
        const string password = "Sup3rSecret!";

        var first = Hasher.Hash(password);
        var second = Hasher.Hash(password);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_Should_Accept_The_Correct_Password()
    {
        const string password = "Sup3rSecret!";
        var hash = Hasher.Hash(password);

        Assert.True(Hasher.Verify(password, hash));
    }

    [Fact]
    public void Verify_Should_Reject_An_Incorrect_Password()
    {
        var hash = Hasher.Hash("Sup3rSecret!");

        Assert.False(Hasher.Verify("WrongPassword!", hash));
    }
}
