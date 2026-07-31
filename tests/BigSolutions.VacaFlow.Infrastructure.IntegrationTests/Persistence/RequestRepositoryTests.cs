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

    private async Task<AbsenceTypeId> GetSeededPersonalLeaveTypeIdAsync()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var types = await scope.ServiceProvider.GetRequiredService<IAbsenceTypeRepository>()
            .ListActiveAsync(CancellationToken.None);

        return types.Single(type => type.Code.Value == "PERSONAL_LEAVE").Id;
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

    [Fact]
    public async Task GetByIdAsync_Should_Return_The_Full_Aggregate_For_A_Seeded_Request()
    {
        var owner = await SeedEmployeeAsync(fixture.Services, $"getbyid-{Guid.NewGuid():N}@vacaflow.test");
        var typeId = await GetSeededVacationTypeIdAsync();
        var period = DateRange.Create(Today, Today.AddDays(2)).Value;
        var nowUtc = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
        var request = Request.Create(
            new RequestId(Guid.NewGuid()), owner.Id, typeId, period, "Family trip", Today, nowUtc).Value;

        await using (var writeScope = fixture.Services.CreateAsyncScope())
        {
            writeScope.ServiceProvider.GetRequiredService<IRequestRepository>().Add(request);
            await writeScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        }

        await using var readScope = fixture.Services.CreateAsyncScope();
        var found = await readScope.ServiceProvider.GetRequiredService<IRequestRepository>()
            .GetByIdAsync(request.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(owner.Id, found.OwnerId);
        Assert.Equal(typeId, found.AbsenceTypeId);
        Assert.Equal(period, found.Period);
        Assert.Equal("Family trip", found.Reason);
        Assert.Equal(RequestState.Draft, found.State);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_For_A_Random_Id()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var found = await scope.ServiceProvider.GetRequiredService<IRequestRepository>()
            .GetByIdAsync(new RequestId(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task Loading_Then_UpdateDetails_Then_SaveChanges_Should_Persist_The_Edit_Without_An_Explicit_Update_Call()
    {
        var owner = await SeedEmployeeAsync(fixture.Services, $"edit-{Guid.NewGuid():N}@vacaflow.test");
        var originalTypeId = await GetSeededVacationTypeIdAsync();
        var originalPeriod = DateRange.Create(Today, Today.AddDays(2)).Value;
        var createdAtUtc = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
        var request = Request.Create(
            new RequestId(Guid.NewGuid()), owner.Id, originalTypeId, originalPeriod, "Family trip",
            Today, createdAtUtc).Value;

        await using (var seedScope = fixture.Services.CreateAsyncScope())
        {
            seedScope.ServiceProvider.GetRequiredService<IRequestRepository>().Add(request);
            await seedScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        }

        var otherTypeId = await GetSeededPersonalLeaveTypeIdAsync();
        var newPeriod = DateRange.Create(Today.AddDays(1), Today.AddDays(3)).Value;
        var updatedAtUtc = createdAtUtc.AddHours(3);

        await using (var editScope = fixture.Services.CreateAsyncScope())
        {
            var repository = editScope.ServiceProvider.GetRequiredService<IRequestRepository>();
            var loaded = await repository.GetByIdAsync(request.Id, CancellationToken.None);
            Assert.NotNull(loaded);

            var updateResult = loaded.UpdateDetails(otherTypeId, newPeriod, "Updated reason", Today, updatedAtUtc);
            Assert.True(updateResult.IsSuccess);

            // No explicit repository.Update()/Add() call — EF's own change
            // tracking on the loaded, still-tracked entity is what this test
            // verifies (plan §3, item #7's remark).
            var saveResult = await editScope.ServiceProvider.GetRequiredService<IUnitOfWork>()
                .SaveChangesAsync(CancellationToken.None);
            Assert.True(saveResult.IsSuccess);
        }

        await using var readScope = fixture.Services.CreateAsyncScope();
        var reread = await readScope.ServiceProvider.GetRequiredService<IRequestRepository>()
            .GetByIdAsync(request.Id, CancellationToken.None);

        Assert.NotNull(reread);
        Assert.Equal(otherTypeId, reread.AbsenceTypeId);
        Assert.Equal(newPeriod, reread.Period);
        Assert.Equal("Updated reason", reread.Reason);
        Assert.Equal(updatedAtUtc, reread.UpdatedAtUtc);
        Assert.Equal(createdAtUtc, reread.CreatedAtUtc);
        Assert.Equal(RequestState.Draft, reread.State);
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

    /// <summary>
    /// RULE-03 (US-016): only a Draft request can be edited. Forces the
    /// State column directly rather than going through Submit() — same
    /// pattern as ExistsActiveAsync_Should_Return_False_For_A_Deactivated_Type
    /// above — so this proves the guard at the persisted-row level
    /// independently of the aggregate's own transition logic (US-016 plan
    /// D7). The equivalent scenario is also proven at the HTTP level, via a
    /// real Submit(), by RequestEndpointTests.Put_After_Submit_Returns_VF_REQ_003
    /// (US-018).
    /// </summary>
    [Fact]
    public async Task UpdateDetails_On_A_Row_Forced_To_Submitted_Should_Fail_With_VF_REQ_003()
    {
        var owner = await SeedEmployeeAsync(fixture.Services, $"submitted-{Guid.NewGuid():N}@vacaflow.test");
        var typeId = await GetSeededVacationTypeIdAsync();
        var period = DateRange.Create(Today, Today.AddDays(2)).Value;
        var request = Request.Create(
            new RequestId(Guid.NewGuid()), owner.Id, typeId, period, "Family trip",
            Today, DateTime.UtcNow).Value;

        await using (var seedScope = fixture.Services.CreateAsyncScope())
        {
            seedScope.ServiceProvider.GetRequiredService<IRequestRepository>().Add(request);
            await seedScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        }

        await using var connection = new SqliteConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Requests SET State = 1 WHERE Id = $id";
        command.Parameters.AddWithValue("$id", request.Id.Value.ToString().ToUpperInvariant());
        await command.ExecuteNonQueryAsync();

        await using var readScope = fixture.Services.CreateAsyncScope();
        var loaded = await readScope.ServiceProvider.GetRequiredService<IRequestRepository>()
            .GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(RequestState.Submitted, loaded.State);

        var result = loaded.UpdateDetails(typeId, period, "Attempted edit", Today, DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-003", result.Error.Code);
    }

    private static async Task<Employee> SeedEmployeeAsync(IServiceProvider services, string email, EmployeeId? managerId)
    {
        await using var scope = services.CreateAsyncScope();
        var employee = Employee.Create(
            new EmployeeId(Guid.NewGuid()), "Integration Test Employee", Email.Create(email).Value, EmployeeRole.Employee).Value;

        if (managerId is not null)
        {
            employee.AssignManager(managerId.Value);
        }

        scope.ServiceProvider.GetRequiredService<IEmployeeRepository>().Add(employee);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

        return employee;
    }

    private async Task<Request> SeedRequestAsync(EmployeeId owner, AbsenceTypeId typeId, DateTime createdAtUtc)
    {
        var period = DateRange.Create(Today, Today.AddDays(2)).Value;
        var request = Request.Create(
            new RequestId(Guid.NewGuid()), owner, typeId, period, "Family trip", Today, createdAtUtc).Value;

        await using var scope = fixture.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IRequestRepository>().Add(request);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

        return request;
    }

    [Fact]
    public async Task ListOwnedByAsync_Returns_Only_The_Owners_Requests_In_Every_State_Most_Recent_First()
    {
        var typeId = await GetSeededVacationTypeIdAsync();
        var owner = await SeedEmployeeAsync(fixture.Services, $"owned-{Guid.NewGuid():N}@vacaflow.test", managerId: null);
        var other = await SeedEmployeeAsync(fixture.Services, $"owned-other-{Guid.NewGuid():N}@vacaflow.test", managerId: null);
        var now = DateTime.UtcNow;
        var older = await SeedRequestAsync(owner.Id, typeId, now.AddMinutes(-10));
        var newer = await SeedRequestAsync(owner.Id, typeId, now);
        await SeedRequestAsync(other.Id, typeId, now);

        await using var scope = fixture.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<IRequestRepository>()
            .ListOwnedByAsync(owner.Id, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }

    [Fact]
    public async Task ListPendingForManagerAsync_Returns_Only_Submitted_Requests_Of_Assigned_Employees()
    {
        var typeId = await GetSeededVacationTypeIdAsync();
        var manager = await SeedEmployeeAsync(fixture.Services, $"manager-{Guid.NewGuid():N}@vacaflow.test", managerId: null);
        var assigned = await SeedEmployeeAsync(fixture.Services, $"assigned-{Guid.NewGuid():N}@vacaflow.test", manager.Id);
        var unassigned = await SeedEmployeeAsync(fixture.Services, $"unassigned-{Guid.NewGuid():N}@vacaflow.test", managerId: null);
        var otherManagersEmployee = await SeedEmployeeAsync(fixture.Services, $"other-mgr-{Guid.NewGuid():N}@vacaflow.test", unassigned.Id);

        var pending = await SeedRequestAsync(assigned.Id, typeId, DateTime.UtcNow);
        await SubmitAsync(pending.Id);

        var draft = await SeedRequestAsync(assigned.Id, typeId, DateTime.UtcNow.AddMinutes(-1));

        var cancelled = await SeedRequestAsync(assigned.Id, typeId, DateTime.UtcNow.AddMinutes(-2));
        await SubmitAsync(cancelled.Id);
        await CancelAsync(cancelled.Id);

        var unassignedSubmitted = await SeedRequestAsync(unassigned.Id, typeId, DateTime.UtcNow);
        await SubmitAsync(unassignedSubmitted.Id);

        var otherManagerSubmitted = await SeedRequestAsync(otherManagersEmployee.Id, typeId, DateTime.UtcNow);
        await SubmitAsync(otherManagerSubmitted.Id);

        await using var scope = fixture.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<IRequestRepository>()
            .ListPendingForManagerAsync(manager.Id, CancellationToken.None);

        var resultId = Assert.Single(result);
        Assert.Equal(pending.Id, resultId.Id);
        Assert.DoesNotContain(result, request => request.Id == draft.Id);
        Assert.DoesNotContain(result, request => request.Id == cancelled.Id);
        Assert.DoesNotContain(result, request => request.Id == unassignedSubmitted.Id);
        Assert.DoesNotContain(result, request => request.Id == otherManagerSubmitted.Id);
    }

    [Fact]
    public async Task ListPendingForManagerAsync_Excludes_The_Managers_Own_Requests_Even_If_Self_Assigned()
    {
        var typeId = await GetSeededVacationTypeIdAsync();
        var manager = await SeedEmployeeAsync(fixture.Services, $"self-mgr-{Guid.NewGuid():N}@vacaflow.test", managerId: null);
        var own = await SeedRequestAsync(manager.Id, typeId, DateTime.UtcNow);
        await SubmitAsync(own.Id);

        await using var connection = new SqliteConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Employees SET ManagerId = $managerId WHERE Id = $id";
        command.Parameters.AddWithValue("$managerId", manager.Id.Value.ToString().ToUpperInvariant());
        command.Parameters.AddWithValue("$id", manager.Id.Value.ToString().ToUpperInvariant());
        await command.ExecuteNonQueryAsync();

        await using var scope = fixture.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<IRequestRepository>()
            .ListPendingForManagerAsync(manager.Id, CancellationToken.None);

        Assert.DoesNotContain(result, request => request.Id == own.Id);
    }

    private async Task SubmitAsync(RequestId id)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRequestRepository>();
        var request = await repository.GetByIdAsync(id, CancellationToken.None);
        request!.Submit(Today, DateTime.UtcNow);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
    }

    private async Task CancelAsync(RequestId id)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRequestRepository>();
        var request = await repository.GetByIdAsync(id, CancellationToken.None);
        request!.Cancel(DateTime.UtcNow);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
    }
}
