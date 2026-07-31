using BigSolutions.VacaFlow.Application.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BigSolutions.VacaFlow.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// SqliteDatabaseFixture already runs IDatabaseInitializer once during
/// InitializeAsync (CA-TST-004), so the seeded catalog from TE-003 is already
/// present by the time these tests run — the same startup path Program.cs
/// takes.
/// </summary>
public sealed class AbsenceTypeRepositoryTests(SqliteDatabaseFixture fixture) : IClassFixture<SqliteDatabaseFixture>
{
    [Fact]
    public async Task ListActiveAsync_Should_Return_The_Three_Seeded_Types()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAbsenceTypeRepository>();

        var types = await repository.ListActiveAsync(CancellationToken.None);

        Assert.Equal(3, types.Count);
        Assert.Contains(types, type => type.Code.Value == "VACATION" && type.Name == "Vacation");
        Assert.Contains(types, type => type.Code.Value == "PERSONAL_LEAVE" && type.Name == "Personal Leave");
        Assert.Contains(types, type => type.Code.Value == "SICK_LEAVE" && type.Name == "Sick Leave");
    }

    [Fact]
    public async Task ListActiveAsync_Should_Order_By_Name_Ascending()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAbsenceTypeRepository>();

        var types = await repository.ListActiveAsync(CancellationToken.None);

        Assert.Equal(["Personal Leave", "Sick Leave", "Vacation"], types.Select(type => type.Name));
    }

    /// <summary>
    /// AbsenceType exposes no Deactivate() — the catalog is read-only at
    /// runtime by design (SAD.md §5.1), so the only way to get an inactive
    /// row for this test is to write one directly, not through the aggregate.
    /// The fixture's database is shared across every test in this class
    /// (IClassFixture), so the mutation is reverted in a finally block —
    /// this test must not leave SICK_LEAVE deactivated for whichever test
    /// runs next.
    /// </summary>
    [Fact]
    public async Task ListActiveAsync_Should_Exclude_An_Inactive_Type()
    {
        await using var connection = new SqliteConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await SetSickLeaveActiveAsync(connection, isActive: false);
        try
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAbsenceTypeRepository>();

            var types = await repository.ListActiveAsync(CancellationToken.None);

            Assert.Equal(2, types.Count);
            Assert.DoesNotContain(types, type => type.Code.Value == "SICK_LEAVE");
        }
        finally
        {
            await SetSickLeaveActiveAsync(connection, isActive: true);
        }
    }

    private static async Task SetSickLeaveActiveAsync(SqliteConnection connection, bool isActive)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE AbsenceTypes SET IsActive = $isActive WHERE Code = 'SICK_LEAVE'";
        command.Parameters.AddWithValue("$isActive", isActive);
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task ListByIdsAsync_Returns_Exactly_The_Requested_Ids_And_Ignores_Unknown_Ones()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAbsenceTypeRepository>();
        var seeded = await repository.ListActiveAsync(CancellationToken.None);
        var vacation = seeded.Single(type => type.Code.Value == "VACATION");
        var sickLeave = seeded.Single(type => type.Code.Value == "SICK_LEAVE");

        var result = await repository.ListByIdsAsync([vacation.Id, sickLeave.Id, new(Guid.NewGuid())], CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, type => type.Id == vacation.Id);
        Assert.Contains(result, type => type.Id == sickLeave.Id);
    }

    /// <summary>
    /// D7: unlike ListActiveAsync, a request whose absence type was later
    /// deactivated must still resolve its code/name — the fixture's shared
    /// database is reverted in a finally block, same pattern as
    /// ListActiveAsync_Should_Exclude_An_Inactive_Type above.
    /// </summary>
    [Fact]
    public async Task ListByIdsAsync_Includes_Inactive_Types()
    {
        await using var connection = new SqliteConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var scope = fixture.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAbsenceTypeRepository>();
        var sickLeave = (await repository.ListActiveAsync(CancellationToken.None)).Single(type => type.Code.Value == "SICK_LEAVE");

        await SetSickLeaveActiveAsync(connection, isActive: false);
        try
        {
            var result = await repository.ListByIdsAsync([sickLeave.Id], CancellationToken.None);

            var found = Assert.Single(result);
            Assert.Equal("Sick Leave", found.Name);
        }
        finally
        {
            await SetSickLeaveActiveAsync(connection, isActive: true);
        }
    }
}
