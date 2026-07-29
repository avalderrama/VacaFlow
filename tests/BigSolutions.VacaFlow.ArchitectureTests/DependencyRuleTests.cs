using System.Reflection;
using BigSolutions.VacaFlow.Application;
using BigSolutions.VacaFlow.Domain;
using BigSolutions.VacaFlow.Infrastructure;
using NetArchTest.Rules;

namespace BigSolutions.VacaFlow.ArchitectureTests;

/// <summary>
/// Enforces the dependency rules of <c>Docs/reglas-clean-architecture-onion.md</c>.
/// Each test names the rule it asserts. A failure is a rejected pull request,
/// not a style opinion — see SAD.md §13.
/// </summary>
public sealed class DependencyRuleTests
{
    private const string DomainNamespace = "BigSolutions.VacaFlow.Domain";
    private const string ApplicationNamespace = "BigSolutions.VacaFlow.Application";
    private const string InfrastructureNamespace = "BigSolutions.VacaFlow.Infrastructure";
    private const string ApiNamespace = "BigSolutions.VacaFlow.Api";

    private static readonly Assembly Api = typeof(Program).Assembly;

    [Fact] // CA-DEP-001, CA-DEP-002
    public void Domain_Should_Not_Depend_On_Any_Outer_Ring()
    {
        var result = Types.InAssembly(DomainAssembly.Instance)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        AssertSuccess(result, "The domain must not know any outer ring.");
    }

    [Fact] // CA-DEP-003
    public void Domain_Should_Not_Depend_On_Frameworks()
    {
        var result = Types.InAssembly(DomainAssembly.Instance)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Microsoft.Extensions.DependencyInjection",
                "Newtonsoft.Json",
                "System.Text.Json",
                "Dapper")
            .GetResult();

        AssertSuccess(result, "The domain must depend on the BCL and nothing else.");
    }

    [Fact] // CA-DEP-001
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly.Instance)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        AssertSuccess(result, "The application layer must not know its adapters.");
    }

    [Fact] // CA-APP-004, CA-APP-005
    public void Application_Should_Not_Depend_On_Web_Or_Data_Access()
    {
        var result = Types.InAssembly(ApplicationAssembly.Instance)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.Data.Sqlite")
            .GetResult();

        AssertSuccess(result, "A use case must return a Result or a DTO, never an HTTP or ORM type.");
    }

    [Fact] // CA-DEP-001
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly.Instance)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        AssertSuccess(result, "Infrastructure is an inner ring relative to presentation.");
    }

    [Fact] // CA-DEP-008, CA-PRE-002
    public void Api_Endpoints_Should_Not_Depend_On_Persistence()
    {
        var endpoints = Types.InAssembly(Api)
            .That().ResideInNamespaceStartingWith($"{ApiNamespace}.Endpoints");

        if (!endpoints.GetTypes().Any())
        {
            return; // No endpoints yet; the assertion arms itself as they are written.
        }

        var result = endpoints
            .ShouldNot()
            .HaveDependencyOnAny(
                $"{InfrastructureNamespace}.Persistence",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        AssertSuccess(result, "An endpoint delegates to a use case; it never touches persistence.");
    }

    [Fact] // CA-DEP-007
    public void Infrastructure_Implementations_Should_Not_Be_Public()
    {
        var implementations = Types.InAssembly(InfrastructureAssembly.Instance)
            .That().HaveNameEndingWith("Repository")
            .Or().HaveNameEndingWith("UnitOfWork")
            .Or().HaveNameEndingWith("DbContext")
            .Or().HaveNameEndingWith("Hasher")
            .Or().HaveNameEndingWith("Seeder");

        if (!implementations.GetTypes().Any())
        {
            return; // Nothing to guard yet.
        }

        var result = implementations.Should().NotBePublic().GetResult();

        AssertSuccess(
            result,
            "Only AddInfrastructure() is public. If presentation can construct a repository, the barrier is broken.");
    }

    [Fact] // CA-DEP-005 — no cycle between the domain feature slices
    public void Employees_Should_Not_Depend_On_Requests()
    {
        var employees = Types.InAssembly(DomainAssembly.Instance)
            .That().ResideInNamespaceStartingWith($"{DomainNamespace}.Employees");

        if (!employees.GetTypes().Any())
        {
            return;
        }

        var result = employees
            .ShouldNot()
            .HaveDependencyOn($"{DomainNamespace}.Requests")
            .GetResult();

        AssertSuccess(
            result,
            "Requests may reference Employees. The reverse would close a cycle between features.");
    }

    [Fact] // CA-APP-001
    public void Handlers_Should_Be_Sealed_And_Suffixed()
    {
        var handlers = Types.InAssembly(ApplicationAssembly.Instance)
            .That().HaveNameEndingWith("Handler");

        if (!handlers.GetTypes().Any())
        {
            return;
        }

        var result = handlers.Should().BeSealed().GetResult();

        AssertSuccess(result, "A use case handler is a leaf; nothing inherits from it.");
    }

    private static void AssertSuccess(TestResult result, string because)
    {
        var offenders = result.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames);

        Assert.True(result.IsSuccessful, $"{because} Offending types: {offenders}");
    }
}
