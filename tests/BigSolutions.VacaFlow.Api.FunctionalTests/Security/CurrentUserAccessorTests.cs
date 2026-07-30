using System.Security.Claims;
using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BigSolutions.VacaFlow.Api.FunctionalTests.Security;

/// <summary>
/// Exercises the real <c>ICurrentUser</c> registration from <c>Program.cs</c>
/// through the port only — the accessor's concrete type is internal to Api,
/// and resolving it via DI (rather than via InternalsVisibleTo) is the same
/// convention <c>Infrastructure.IntegrationTests</c> already follows for its
/// internal repository implementations (AC3).
/// </summary>
public sealed class CurrentUserAccessorTests(VacaFlowApiFactory factory)
    : IClassFixture<VacaFlowApiFactory>
{
    [Fact]
    public void Reads_The_Employee_Id_And_Role_From_The_Authenticated_Principal()
    {
        var employeeId = Guid.NewGuid();

        using var scope = CreateScopeWithPrincipal(BuildPrincipal(employeeId, "Manager"));
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        Assert.Equal(new EmployeeId(employeeId), currentUser.EmployeeId);
        Assert.Equal(EmployeeRole.Manager, currentUser.Role);
    }

    [Fact]
    public void Throws_When_There_Is_No_HttpContext()
    {
        var accessor = factory.Services.GetRequiredService<IHttpContextAccessor>();
        using var scope = factory.Services.CreateScope();
        accessor.HttpContext = null;

        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        Assert.Throws<InvalidOperationException>(() => currentUser.EmployeeId);
    }

    [Fact]
    public void Throws_When_The_NameIdentifier_Claim_Is_Missing()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, "Employee")], "Test");

        using var scope = CreateScopeWithPrincipal(new ClaimsPrincipal(identity));
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        Assert.Throws<InvalidOperationException>(() => currentUser.EmployeeId);
    }

    [Fact]
    public void Throws_When_The_NameIdentifier_Claim_Is_Not_A_Guid()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid"),
            new Claim(ClaimTypes.Role, "Employee"),
        ], "Test");

        using var scope = CreateScopeWithPrincipal(new ClaimsPrincipal(identity));
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        Assert.Throws<InvalidOperationException>(() => currentUser.EmployeeId);
    }

    [Fact]
    public void Throws_When_The_Role_Claim_Is_Missing()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Test");

        using var scope = CreateScopeWithPrincipal(new ClaimsPrincipal(identity));
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        Assert.Throws<InvalidOperationException>(() => currentUser.Role);
    }

    [Fact]
    public void Throws_When_The_Role_Claim_Does_Not_Parse()
    {
        using var scope = CreateScopeWithPrincipal(BuildPrincipal(Guid.NewGuid(), "NotARole"));
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        Assert.Throws<InvalidOperationException>(() => currentUser.Role);
    }

    [Fact]
    public void Throws_When_The_Role_Claim_Is_A_Number_With_No_Matching_Enum_Value()
    {
        // Enum.TryParse alone would happily accept "7" as an undefined
        // EmployeeRole — this is the case that closes that gap.
        using var scope = CreateScopeWithPrincipal(BuildPrincipal(Guid.NewGuid(), "7"));
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        Assert.Throws<InvalidOperationException>(() => currentUser.Role);
    }

    private IServiceScope CreateScopeWithPrincipal(ClaimsPrincipal principal)
    {
        var accessor = factory.Services.GetRequiredService<IHttpContextAccessor>();
        var scope = factory.Services.CreateScope();

        accessor.HttpContext = new DefaultHttpContext { User = principal };

        return scope;
    }

    private static ClaimsPrincipal BuildPrincipal(Guid employeeId, string role) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, employeeId.ToString()),
            new Claim(ClaimTypes.Role, role),
        ], "Test"));
}
