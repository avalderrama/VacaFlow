using BigSolutions.VacaFlow.Application.Authentication;
using BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Application.UnitTests.Authentication;

public sealed class SignInHandlerTests
{
    private const string KnownEmail = "carlos.ruiz@vacaflow.test";
    private const string CorrectPassword = "Sup3rSecret!";

    private static Employee BuildEmployee(string email = KnownEmail) =>
        Employee.Create(
            new EmployeeId(Guid.Parse("00000000-0000-0000-0000-000000000042")),
            "Carlos Ruiz",
            Email.Create(email).Value,
            EmployeeRole.Employee).Value;

    private static (SignInHandler Handler, CountingPasswordHasher Hasher) BuildHandler(
        Employee? existingEmployee = null,
        string? storedPassword = CorrectPassword)
    {
        var repository = new FakeEmployeeRepository();
        var credentialStore = new FakeCredentialStore();
        var hasher = new CountingPasswordHasher();

        if (existingEmployee is not null)
        {
            repository.WithEmployee(existingEmployee);

            if (storedPassword is not null)
            {
                credentialStore.Add(existingEmployee.Id, hasher.Hash(storedPassword));
            }
        }

        return (new SignInHandler(repository, credentialStore, hasher), hasher);
    }

    [Fact]
    public async Task Handle_Should_Succeed_With_Correct_Credentials()
    {
        var (handler, _) = BuildHandler(BuildEmployee());

        var result = await handler.Handle(new SignInCommand(KnownEmail, CorrectPassword), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Carlos Ruiz", result.Value.FullName);
        Assert.Equal(KnownEmail, result.Value.Email);
        Assert.Equal("Employee", result.Value.Role);
    }

    [Fact]
    public async Task Handle_Should_Match_The_Email_Case_Insensitively()
    {
        var (handler, _) = BuildHandler(BuildEmployee());

        var result = await handler.Handle(
            new SignInCommand("CARLOS.RUIZ@VacaFlow.TEST", CorrectPassword), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// FR-AUT-006: the two cases must be indistinguishable. Comparing the Error
    /// objects rather than just their codes catches a message or field drifting
    /// apart between the two paths.
    /// </summary>
    [Fact]
    public async Task A_Wrong_Password_And_An_Unknown_Email_Should_Return_The_Very_Same_Error()
    {
        var (wrongPasswordHandler, _) = BuildHandler(BuildEmployee());
        var (unknownEmailHandler, _) = BuildHandler();

        var wrongPassword = await wrongPasswordHandler.Handle(
            new SignInCommand(KnownEmail, "NotThePassword!"), CancellationToken.None);
        var unknownEmail = await unknownEmailHandler.Handle(
            new SignInCommand("nobody@vacaflow.test", CorrectPassword), CancellationToken.None);

        Assert.True(wrongPassword.IsFailure);
        Assert.True(unknownEmail.IsFailure);
        Assert.Equal(wrongPassword.Error, unknownEmail.Error);
        Assert.Equal("VF-AUT-002", wrongPassword.Error.Code);
        Assert.Null(wrongPassword.Error.Field);
    }

    [Fact]
    public async Task A_Malformed_Email_Should_Also_Return_Invalid_Credentials()
    {
        var (handler, _) = BuildHandler(BuildEmployee());

        var result = await handler.Handle(new SignInCommand("not-an-email", CorrectPassword), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-AUT-002", result.Error.Code);
    }

    /// <summary>
    /// The timing-oracle guard: verification must run even when there is no
    /// account, so an unknown address does not answer faster than a known one.
    /// </summary>
    [Fact]
    public async Task Verification_Should_Run_Even_When_The_Email_Is_Unknown()
    {
        var (handler, hasher) = BuildHandler();

        await handler.Handle(new SignInCommand("nobody@vacaflow.test", CorrectPassword), CancellationToken.None);

        Assert.Equal(1, hasher.VerifyCallCount);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Account_Is_Inactive()
    {
        var employee = BuildEmployee();
        SetInactive(employee);
        var (handler, _) = BuildHandler(employee);

        var result = await handler.Handle(new SignInCommand(KnownEmail, CorrectPassword), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-AUT-003", result.Error.Code);
    }

    /// <summary>
    /// An inactive account must not be detectable without the password: the
    /// wrong password has to win over the inactive check, or the error becomes
    /// an existence oracle.
    /// </summary>
    [Fact]
    public async Task An_Inactive_Account_With_A_Wrong_Password_Should_Report_Invalid_Credentials()
    {
        var employee = BuildEmployee();
        SetInactive(employee);
        var (handler, _) = BuildHandler(employee);

        var result = await handler.Handle(new SignInCommand(KnownEmail, "NotThePassword!"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-AUT-002", result.Error.Code);
    }

    [Theory]
    [InlineData(null, CorrectPassword)]
    [InlineData(KnownEmail, null)]
    [InlineData("", "")]
    public async Task Handle_Should_Fail_When_Either_Credential_Is_Missing(string? email, string? password)
    {
        var (handler, _) = BuildHandler(BuildEmployee());

        var result = await handler.Handle(new SignInCommand(email, password), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-AUT-002", result.Error.Code);
    }

    /// <summary>
    /// Employee exposes no way to deactivate an account yet — no story has
    /// needed one. Reflection keeps this test honest about the production API
    /// instead of adding a setter that only tests would use.
    /// </summary>
    private static void SetInactive(Employee employee) =>
        typeof(Employee).GetProperty(nameof(Employee.IsActive))!.SetValue(employee, false);

    private sealed class CountingPasswordHasher : IPasswordHasherSpy
    {
        private const string Prefix = "fake-hash:";

        public int VerifyCallCount { get; private set; }

        public string Hash(string password) => Prefix + password;

        public bool Verify(string password, string hash)
        {
            VerifyCallCount++;
            return hash == Prefix + password;
        }
    }

    private interface IPasswordHasherSpy : Abstractions.IPasswordHasher
    {
        int VerifyCallCount { get; }
    }
}
