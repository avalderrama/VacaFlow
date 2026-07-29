using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;
using Microsoft.Extensions.DependencyInjection;

namespace BigSolutions.VacaFlow.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// CredentialStore is the one place that stamps UserAccounts.CreatedAtUtc from
/// the injected clock, so it is also the only place where CA-CRS-002's "a test
/// must be able to fix the clock" can actually be exercised.
/// </summary>
public sealed class CredentialStoreTests : IAsyncLifetime
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteDatabaseFixture _fixture = new() { TimeProvider = new FixedTimeProvider(FixedNow) };

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Add_Should_Stamp_CreatedAtUtc_From_The_Injected_Clock()
    {
        var employee = Employee.Create(
            new EmployeeId(Guid.CreateVersion7()),
            "Clock Test Employee",
            Email.Create($"clock-{Guid.NewGuid():N}@vacaflow.test").Value,
            EmployeeRole.Employee).Value;

        await using var scope = _fixture.Services.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<IEmployeeRepository>().Add(employee);
        scope.ServiceProvider.GetRequiredService<ICredentialStore>()
            .Add(employee.Id, "pbkdf2-sha256$210000$c2FsdA==$aGFzaA==");

        var saveResult = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>()
            .SaveChangesAsync(CancellationToken.None);

        Assert.True(saveResult.IsSuccess);

        var storedHash = await scope.ServiceProvider.GetRequiredService<ICredentialStore>()
            .FindHashAsync(employee.Id, CancellationToken.None);

        Assert.Equal("pbkdf2-sha256$210000$c2FsdA==$aGFzaA==", storedHash);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
