using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace BigSolutions.VacaFlow.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// SqliteDatabaseFixture already runs IDatabaseInitializer once during
/// InitializeAsync (CA-TST-004), so by the time these tests run the database
/// is already seeded — exactly the same startup path Program.cs takes.
/// </summary>
public sealed class DatabaseSeederTests(SqliteDatabaseFixture fixture) : IClassFixture<SqliteDatabaseFixture>
{
    [Fact]
    public async Task Seeding_Should_Create_The_Three_Absence_Types_Of_Backlog_3_6()
    {
        var absenceTypes = await ReadAbsenceTypesAsync();

        Assert.Equal(3, absenceTypes.Count);
        Assert.Contains(absenceTypes, type => type.Code == "VACATION" && type.Name == "Vacation");
        Assert.Contains(absenceTypes, type => type.Code == "PERSONAL_LEAVE" && type.Name == "Personal Leave");
        Assert.Contains(absenceTypes, type => type.Code == "SICK_LEAVE" && type.Name == "Sick Leave");
        Assert.All(absenceTypes, type => Assert.True(type.IsActive));
    }

    [Fact]
    public async Task Seeding_Should_Create_The_Three_Employees_With_Carlos_And_Ana_Assigned_To_Laura()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var laura = await repository.GetByEmailAsync(Email.Create("manager@vacaflow.test").Value, CancellationToken.None);
        var carlos = await repository.GetByEmailAsync(Email.Create("employee@vacaflow.test").Value, CancellationToken.None);
        var ana = await repository.GetByEmailAsync(Email.Create("ana@vacaflow.test").Value, CancellationToken.None);

        Assert.NotNull(laura);
        Assert.Equal(EmployeeRole.Manager, laura.Role);
        Assert.Null(laura.ManagerId);

        Assert.NotNull(carlos);
        Assert.Equal(EmployeeRole.Employee, carlos.Role);
        Assert.Equal(laura.Id, carlos.ManagerId);

        Assert.NotNull(ana);
        Assert.Equal(EmployeeRole.Employee, ana.Role);
        Assert.Equal(laura.Id, ana.ManagerId);
    }

    [Fact]
    public async Task Running_The_Initializer_A_Second_Time_Should_Not_Duplicate_Anything()
    {
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>()
                .InitializeAsync(CancellationToken.None);
        }

        var absenceTypes = await ReadAbsenceTypesAsync();
        Assert.Equal(3, absenceTypes.Count);
        Assert.Equal(3, await CountSeededEmployeesAsync());
    }

    [Fact]
    public async Task The_Seeded_Managers_Password_Should_Verify_And_Not_Be_Stored_In_Plain_Text()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        var credentialStore = scope.ServiceProvider.GetRequiredService<ICredentialStore>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var laura = await repository.GetByEmailAsync(Email.Create("manager@vacaflow.test").Value, CancellationToken.None);
        var hash = await credentialStore.FindHashAsync(laura!.Id, CancellationToken.None);

        Assert.NotNull(hash);
        Assert.DoesNotContain("Manager123!", hash, StringComparison.Ordinal);
        Assert.True(hasher.Verify("Manager123!", hash));
    }

    /// <summary>
    /// Raw SQL, not the ORM: there is no Application port over AbsenceTypes
    /// yet (US-014 introduces one), and VacaFlowDbContext is internal to
    /// Infrastructure — the same file the real DbContext writes to, read
    /// through the connection string the fixture already built for it.
    /// </summary>
    private async Task<List<(string Code, string Name, bool IsActive)>> ReadAbsenceTypesAsync()
    {
        await using var connection = new SqliteConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Code, Name, IsActive FROM AbsenceTypes";

        var results = new List<(string, string, bool)>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1), reader.GetBoolean(2)));
        }

        return results;
    }

    private async Task<long> CountSeededEmployeesAsync()
    {
        await using var connection = new SqliteConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        // The three exact seeded addresses, not a '%@vacaflow.test' pattern:
        // EmployeeRepositoryTests seeds its own throwaway *@vacaflow.test
        // rows into a separate fixture instance today, but a LIKE filter
        // would silently start counting those too the day anything shares
        // a fixture or database across test classes.
        command.CommandText = "SELECT COUNT(*) FROM Employees WHERE Email IN "
            + "('manager@vacaflow.test', 'employee@vacaflow.test', 'ana@vacaflow.test')";

        return (long)(await command.ExecuteScalarAsync())!;
    }
}
