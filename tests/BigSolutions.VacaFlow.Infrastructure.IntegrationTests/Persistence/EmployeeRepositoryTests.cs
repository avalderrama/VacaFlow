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
}
