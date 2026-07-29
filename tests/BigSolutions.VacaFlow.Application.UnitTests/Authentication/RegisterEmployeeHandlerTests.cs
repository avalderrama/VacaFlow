using BigSolutions.VacaFlow.Application.Authentication;
using BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;
using BigSolutions.VacaFlow.Domain.Employees.Errors;
using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Application.UnitTests.Authentication;

public sealed class RegisterEmployeeHandlerTests
{
    private static readonly Guid FixedEmployeeId = Guid.Parse("00000000-0000-0000-0000-000000000042");

    private static RegisterEmployeeCommand ValidCommand(string email = "new.employee@vacaflow.test") =>
        new("New Employee", email, "Sup3rSecret!", "Employee");

    private static (RegisterEmployeeHandler Handler, FakeEmployeeRepository Repository,
        FakeCredentialStore CredentialStore, FakeUnitOfWork UnitOfWork) BuildHandler(
        Result? saveChangesResult = null, params string[] existingEmails)
    {
        var repository = new FakeEmployeeRepository(existingEmails);
        var credentialStore = new FakeCredentialStore();
        var unitOfWork = new FakeUnitOfWork(saveChangesResult);

        var handler = new RegisterEmployeeHandler(
            repository,
            credentialStore,
            new FakePasswordHasher(),
            unitOfWork,
            new FakeIdGenerator(FixedEmployeeId));

        return (handler, repository, credentialStore, unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_Persist_The_Employee_And_The_Credential_On_The_Happy_Path()
    {
        var (handler, repository, credentialStore, unitOfWork) = BuildHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(repository.AddedEmployees);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);

        var storedHash = await credentialStore.FindHashAsync(repository.AddedEmployees[0].Id, CancellationToken.None);
        Assert.NotNull(storedHash);
    }

    [Fact]
    public async Task Handle_Should_Return_A_Dto_With_The_Generated_Id_And_Normalized_Email()
    {
        var (handler, _, _, _) = BuildHandler();

        var result = await handler.Handle(ValidCommand("Mixed.Case@Company.COM"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(FixedEmployeeId, result.Value.Id);
        Assert.Equal("mixed.case@company.com", result.Value.Email);
    }

    [Fact]
    public void RegisteredAccountDto_Should_Have_No_Password_Or_Hash_Property()
    {
        // Structural guarantee, not a runtime one: RegisteredAccountDto (CA-APP-006)
        // simply has no field that could carry a hash, so there is nothing to leak
        // (NFR-SEC-002).
        var properties = typeof(RegisteredAccountDto).GetProperties().Select(p => p.Name);

        Assert.DoesNotContain(properties, name =>
            name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("hash", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Email_Is_Already_Registered()
    {
        var (handler, repository, _, unitOfWork) =
            BuildHandler(existingEmails: ["existing@vacaflow.test"]);

        var result = await handler.Handle(ValidCommand("existing@vacaflow.test"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-AUT-001", result.Error.Code);
        Assert.Equal("email", result.Error.Field);
        Assert.Empty(repository.AddedEmployees);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Password_Is_Too_Short()
    {
        var (handler, repository, _, _) = BuildHandler();
        var command = ValidCommand() with { Password = "short" };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-VAL-001", result.Error.Code);
        Assert.Equal("password", result.Error.Field);
        Assert.Empty(repository.AddedEmployees);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Password_Exceeds_The_Maximum_Length()
    {
        // Guards the KDF against unbounded input from an anonymous caller.
        var (handler, repository, _, _) = BuildHandler();
        var command = ValidCommand() with { Password = new string('a', 129) };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-VAL-001", result.Error.Code);
        Assert.Equal("password", result.Error.Field);
        // The message must describe what actually happened, not the opposite.
        Assert.Contains("or fewer", result.Error.Message, StringComparison.Ordinal);
        Assert.Empty(repository.AddedEmployees);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Email_Is_Malformed()
    {
        var (handler, repository, _, _) = BuildHandler();
        var command = ValidCommand() with { Email = "not-an-email" };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-VAL-001", result.Error.Code);
        Assert.Equal("email", result.Error.Field);
        Assert.Empty(repository.AddedEmployees);
    }

    [Fact]
    public async Task Handle_Should_Never_Store_The_Plain_Text_Password_As_The_Hash()
    {
        var (handler, repository, credentialStore, _) = BuildHandler();

        await handler.Handle(ValidCommand(), CancellationToken.None);

        var storedHash = await credentialStore.FindHashAsync(repository.AddedEmployees[0].Id, CancellationToken.None);
        Assert.NotEqual("Sup3rSecret!", storedHash);
    }

    [Fact]
    public async Task Handle_Should_Source_The_Identifier_From_IIdGenerator()
    {
        var (handler, repository, _, _) = BuildHandler();

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(FixedEmployeeId, repository.AddedEmployees[0].Id.Value);
    }

    [Fact]
    public async Task Handle_Should_Propagate_A_Unit_Of_Work_Failure_As_The_Race_Safety_Net()
    {
        var raceError = EmployeeErrors.EmailAlreadyRegistered;
        var (handler, _, _, unitOfWork) = BuildHandler(Result.Failure(raceError));

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(raceError, result.Error);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
