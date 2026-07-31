using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BigSolutions.VacaFlow.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Against a real, temporary SQLite database (CA-TST-004) — never a mock of
/// the ORM. SqliteDatabaseFixture already seeds the AbsenceTypes catalog, so
/// ExistsActiveAsync has real rows to check against.
/// </summary>
public sealed class RequestRepositoryTests(SqliteDatabaseFixture fixture) : IClassFixture<SqliteDatabaseFixture>
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static async Task<Employee> SeedEmployeeAsync(IServiceProvider services, string email)
    {
        await using var scope = services.CreateAsyncScope();
        var employee = Employee.Create(
            new EmployeeId(Guid.NewGuid()), "Integration Test Employee", Email.Create(email).Value, EmployeeRole.Employee).Value;

        scope.ServiceProvider.GetRequiredService<IEmployeeRepository>().Add(employee);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

        return employee;
    }

    private async Task<AbsenceTypeId> GetSeededVacationTypeIdAsync()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var types = await scope.ServiceProvider.GetRequiredService<IAbsenceTypeRepository>()
            .ListActiveAsync(CancellationToken.None);

        return types.Single(type => type.Code.Value == "VACATION").Id;
    }

    [Fact]
    public async Task Add_Followed_By_SaveChanges_Should_Persist_The_Request()
    {
        var owner = await SeedEmployeeAsync(fixture.Services, $"owner-{Guid.NewGuid():N}@vacaflow.test");
        var typeId = await GetSeededVacationTypeIdAsync();
        var period = DateRange.Create(Today, Today.AddDays(2)).Value;
        var nowUtc = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
        var request = Request.Create(
            new RequestId(Guid.NewGuid()), owner.Id, typeId, period, "Family trip", Today, nowUtc).Value;

        await using (var writeScope = fixture.Services.CreateAsyncScope())
        {
            writeScope.ServiceProvider.GetRequiredService<IRequestRepository>().Add(request);
            var saveResult = await writeScope.ServiceProvider.GetRequiredService<IUnitOfWork>()
                .SaveChangesAsync(CancellationToken.None);

            Assert.True(saveResult.IsSuccess);
        }

        // No read port exists yet on IRequestRepository (plan D8) — the
        // roundtrip is verified through the same raw-SQL pattern
        // DatabaseSeederTests uses for tables with no repository operation.
        await using var connection = new SqliteConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT EmployeeId, AbsenceTypeId, StartDate, EndDate, Reason, State, CreatedAtUtc, UpdatedAtUtc, " +
            "SubmittedAtUtc, ClosedAtUtc FROM Requests WHERE Id = $id";
        // EF Core's Sqlite provider stores Guid as upper-case TEXT; the
        // comparison here is a raw SQL string match, so it must be cased the
        // same way the value was written.
        command.Parameters.AddWithValue("$id", request.Id.Value.ToString().ToUpperInvariant());

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());

        Assert.Equal(owner.Id.Value.ToString().ToUpperInvariant(), reader.GetString(0));
        Assert.Equal(typeId.Value.ToString().ToUpperInvariant(), reader.GetString(1));
        Assert.Equal(Today.ToString("yyyy-MM-dd"), reader.GetString(2));
        Assert.Equal(Today.AddDays(2).ToString("yyyy-MM-dd"), reader.GetString(3));
        Assert.Equal("Family trip", reader.GetString(4));
        Assert.Equal(0, reader.GetInt32(5));
        Assert.False(reader.IsDBNull(6));
        Assert.False(reader.IsDBNull(7));
        Assert.True(reader.IsDBNull(8));
        Assert.True(reader.IsDBNull(9));
    }

    /// <remarks>
    /// UnitOfWork only translates the Employees.Email uniqueness violation
    /// (plan §3.1) — every other constraint, including a foreign key to a
    /// nonexistent owner, propagates as a DbUpdateException and becomes a 500
    /// at the edge, same precedent as EmployeeRepositoryTests's
    /// A_Constraint_Violation_That_Is_Not_Email_Uniqueness_Should_Not_Be_Translated.
    /// </remarks>
    [Fact]
    public async Task A_Foreign_Key_Violation_On_The_Owner_Should_Not_Be_Translated()
    {
        var typeId = await GetSeededVacationTypeIdAsync();
        var period = DateRange.Create(Today, Today).Value;
        var request = Request.Create(
            new RequestId(Guid.NewGuid()),
            new EmployeeId(Guid.NewGuid()),
            typeId,
            period,
            "Family trip",
            Today,
            DateTime.UtcNow).Value;

        await using var scope = fixture.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IRequestRepository>().Add(request);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ExistsActiveAsync_Should_Return_True_For_A_Seeded_Active_Type()
    {
        var typeId = await GetSeededVacationTypeIdAsync();

        await using var scope = fixture.Services.CreateAsyncScope();
        var exists = await scope.ServiceProvider.GetRequiredService<IAbsenceTypeRepository>()
            .ExistsActiveAsync(typeId, CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsActiveAsync_Should_Return_False_For_A_Random_Id()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var exists = await scope.ServiceProvider.GetRequiredService<IAbsenceTypeRepository>()
            .ExistsActiveAsync(new AbsenceTypeId(Guid.NewGuid()), CancellationToken.None);

        Assert.False(exists);
    }

    /// <summary>
    /// AbsenceType exposes no Deactivate() (SAD.md §5.1), so the only way to
    /// get an inactive row is to write one directly — same pattern as
    /// AbsenceTypeRepositoryTests.ListActiveAsync_Should_Exclude_An_Inactive_Type.
    /// The mutation is reverted so this test does not leave VACATION
    /// deactivated for whichever test runs next against the shared fixture.
    /// </summary>
    [Fact]
    public async Task ExistsActiveAsync_Should_Return_False_For_A_Deactivated_Type()
    {
        var typeId = await GetSeededVacationTypeIdAsync();

        await using var connection = new SqliteConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await SetVacationActiveAsync(connection, isActive: false);

        try
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            var exists = await scope.ServiceProvider.GetRequiredService<IAbsenceTypeRepository>()
                .ExistsActiveAsync(typeId, CancellationToken.None);

            Assert.False(exists);
        }
        finally
        {
            await SetVacationActiveAsync(connection, isActive: true);
        }
    }

    private static async Task SetVacationActiveAsync(SqliteConnection connection, bool isActive)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE AbsenceTypes SET IsActive = $isActive WHERE Code = 'VACATION'";
        command.Parameters.AddWithValue("$isActive", isActive);
        await command.ExecuteNonQueryAsync();
    }
}
