using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BigSolutions.VacaFlow.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Against a real, temporary SQLite database (CA-TST-004) — never against a
/// mock of the ORM. Each test uses its own email so tests sharing the fixture
/// database do not interfere with each other.
/// </summary>
public sealed class EmployeeRepositoryTests(SqliteDatabaseFixture fixture) : IClassFixture<SqliteDatabaseFixture>
{
    private static Employee NewEmployee(string email, EmployeeRole role = EmployeeRole.Employee) =>
        Employee.Create(
            new EmployeeId(Guid.NewGuid()),
            "Integration Test Employee",
            Email.Create(email).Value,
            role).Value;

    [Fact]
    public async Task Add_Followed_By_SaveChanges_Should_Persist_The_Employee()
    {
        var email = $"persist-{Guid.NewGuid():N}@vacaflow.test";
        var employee = NewEmployee(email);

        await using (var writeScope = fixture.Services.CreateAsyncScope())
        {
            writeScope.ServiceProvider.GetRequiredService<IEmployeeRepository>().Add(employee);
            var saveResult = await writeScope.ServiceProvider.GetRequiredService<IUnitOfWork>()
                .SaveChangesAsync(CancellationToken.None);

            Assert.True(saveResult.IsSuccess);
        }

        // A fresh scope forces a real read from the database file, not the
        // first scope's change tracker.
        await using var readScope = fixture.Services.CreateAsyncScope();
        var exists = await readScope.ServiceProvider.GetRequiredService<IEmployeeRepository>()
            .EmailExistsAsync(Email.Create(email).Value, CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task EmailExistsAsync_Should_Return_False_For_An_Email_Never_Registered()
    {
        await using var scope = fixture.Services.CreateAsyncScope();

        var exists = await scope.ServiceProvider.GetRequiredService<IEmployeeRepository>()
            .EmailExistsAsync(Email.Create($"never-{Guid.NewGuid():N}@vacaflow.test").Value, CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task A_Second_Insert_With_The_Same_Email_Should_Be_Translated_Instead_Of_Thrown()
    {
        var email = $"duplicate-{Guid.NewGuid():N}@vacaflow.test";

        await using (var firstScope = fixture.Services.CreateAsyncScope())
        {
            firstScope.ServiceProvider.GetRequiredService<IEmployeeRepository>().Add(NewEmployee(email));
            var firstSave = await firstScope.ServiceProvider.GetRequiredService<IUnitOfWork>()
                .SaveChangesAsync(CancellationToken.None);

            Assert.True(firstSave.IsSuccess);
        }

        // The application-layer check (handler step 3) is bypassed on purpose
        // here, to exercise the unit-of-work's own translation (step 7) — the
        // race-window safety net the plan calls for.
        await using var secondScope = fixture.Services.CreateAsyncScope();
        secondScope.ServiceProvider.GetRequiredService<IEmployeeRepository>().Add(NewEmployee(email));

        var secondSave = await secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>()
            .SaveChangesAsync(CancellationToken.None);

        Assert.True(secondSave.IsFailure);
        Assert.Equal("VF-AUT-001", secondSave.Error.Code);
    }

    [Fact]
    public async Task GetByEmailAsync_Should_Return_The_Employee_Regardless_Of_The_Requested_Casing()
    {
        var email = $"lookup-{Guid.NewGuid():N}@vacaflow.test";
        var employee = NewEmployee(email);

        await using (var writeScope = fixture.Services.CreateAsyncScope())
        {
            writeScope.ServiceProvider.GetRequiredService<IEmployeeRepository>().Add(employee);
            await writeScope.ServiceProvider.GetRequiredService<IUnitOfWork>()
                .SaveChangesAsync(CancellationToken.None);
        }

        await using var readScope = fixture.Services.CreateAsyncScope();
        var repository = readScope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var found = await repository.GetByEmailAsync(Email.Create(email).Value, CancellationToken.None);
        var foundUpperCased = await repository.GetByEmailAsync(
            Email.Create(email.ToUpperInvariant()).Value, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(employee.Id, found.Id);
        Assert.NotNull(foundUpperCased);
        Assert.Equal(employee.Id, foundUpperCased.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_Should_Return_Null_For_An_Email_Never_Registered()
    {
        await using var scope = fixture.Services.CreateAsyncScope();

        var found = await scope.ServiceProvider.GetRequiredService<IEmployeeRepository>()
            .GetByEmailAsync(Email.Create($"absent-{Guid.NewGuid():N}@vacaflow.test").Value, CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_The_Employee_With_Its_Full_Data()
    {
        var email = $"by-id-{Guid.NewGuid():N}@vacaflow.test";
        var employee = NewEmployee(email, EmployeeRole.Manager);

        await using (var writeScope = fixture.Services.CreateAsyncScope())
        {
            writeScope.ServiceProvider.GetRequiredService<IEmployeeRepository>().Add(employee);
            await writeScope.ServiceProvider.GetRequiredService<IUnitOfWork>()
                .SaveChangesAsync(CancellationToken.None);
        }

        await using var readScope = fixture.Services.CreateAsyncScope();
        var found = await readScope.ServiceProvider.GetRequiredService<IEmployeeRepository>()
            .GetByIdAsync(employee.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(employee.Id, found.Id);
        Assert.Equal("Integration Test Employee", found.FullName);
        Assert.Equal(email, found.Email.Value);
        Assert.Equal(EmployeeRole.Manager, found.Role);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_For_An_Id_Never_Registered()
    {
        await using var scope = fixture.Services.CreateAsyncScope();

        var found = await scope.ServiceProvider.GetRequiredService<IEmployeeRepository>()
            .GetByIdAsync(new EmployeeId(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task A_Constraint_Violation_That_Is_Not_Email_Uniqueness_Should_Not_Be_Translated()
    {
        // Storing a credential for an employee that does not exist violates the
        // UserAccounts -> Employees foreign key. SQLite reports that with the
        // same primary error code as a uniqueness violation (19), so a naive
        // check would mistranslate it into "this email already exists" and hide
        // a real defect behind a 409.
        await using var scope = fixture.Services.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<ICredentialStore>()
            .Add(new EmployeeId(Guid.CreateVersion7()), "pbkdf2-sha256$210000$c2FsdA==$aGFzaA==");

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None));

        // It propagates as an exception (becoming a 500 at the edge) rather than
        // being reported to the user as an email conflict.
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ListByIdsAsync_Returns_Exactly_The_Requested_Ids_And_Ignores_Unknown_Ones()
    {
        var first = NewEmployee($"list-first-{Guid.NewGuid():N}@vacaflow.test");
        var second = NewEmployee($"list-second-{Guid.NewGuid():N}@vacaflow.test");
        var notRequested = NewEmployee($"list-not-requested-{Guid.NewGuid():N}@vacaflow.test");

        await using (var writeScope = fixture.Services.CreateAsyncScope())
        {
            var repository = writeScope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
            repository.Add(first);
            repository.Add(second);
            repository.Add(notRequested);
            await writeScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        }

        await using var readScope = fixture.Services.CreateAsyncScope();
        var unknownId = new EmployeeId(Guid.NewGuid());
        var result = await readScope.ServiceProvider.GetRequiredService<IEmployeeRepository>()
            .ListByIdsAsync([first.Id, second.Id, unknownId], CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, employee => employee.Id == first.Id);
        Assert.Contains(result, employee => employee.Id == second.Id);
        Assert.DoesNotContain(result, employee => employee.Id == notRequested.Id);
    }
}
