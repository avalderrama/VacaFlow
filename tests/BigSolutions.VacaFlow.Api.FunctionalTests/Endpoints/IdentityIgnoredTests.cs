using System.Net.Http.Json;
using BigSolutions.VacaFlow.Api.Contracts;

namespace BigSolutions.VacaFlow.Api.FunctionalTests.Endpoints;

/// <summary>
/// Demonstrates AC2 of TE-011 end-to-end, against the real pipeline: an
/// <c>employeeId</c> or <c>responsibleManagerId</c> injected into a payload is
/// not merely absent from the contract's declared shape (that is AC1, covered
/// structurally by the architecture test) — it is silently dropped by JSON
/// binding before the handler ever runs, so it has no effect whatsoever.
/// </summary>
public sealed class IdentityIgnoredTests(VacaFlowApiFactory factory) : IClassFixture<VacaFlowApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_Ignores_An_Injected_EmployeeId_And_ResponsibleManagerId()
    {
        var foreignEmployeeId = Guid.NewGuid();

        // responsibleManagerId has no observable effect through any endpoint
        // today (no consumer of ICurrentUser exists yet — that is US-010's
        // job), so it is included to prove the request still succeeds with
        // the field present; there is nothing further to assert about its
        // value here.
        var payload = new Dictionary<string, object?>
        {
            ["fullName"] = "Identity Attack",
            ["email"] = $"{Guid.NewGuid():N}@vacaflow.test",
            ["password"] = "Password123!",
            ["role"] = "Employee",
            ["employeeId"] = foreignEmployeeId,
            ["responsibleManagerId"] = Guid.NewGuid(),
        };

        using var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        Assert.NotNull(body);
        Assert.NotEqual(foreignEmployeeId, body.Id);
    }

    [Fact]
    public async Task Login_Ignores_An_Injected_EmployeeId_From_A_Different_Account()
    {
        var victim = await RegisterAsync();
        var attacker = await RegisterAsync();

        var payload = new Dictionary<string, object?>
        {
            ["email"] = attacker.Email,
            ["password"] = attacker.Password,
            ["employeeId"] = victim.Id,
        };

        using var response = await _client.PostAsJsonAsync("/api/auth/login", payload);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        Assert.NotNull(body);
        Assert.Equal(attacker.Id, body.Id);
        Assert.NotEqual(victim.Id, body.Id);
    }

    private async Task<(Guid Id, string Email, string Password)> RegisterAsync()
    {
        const string password = "Password123!";
        var email = $"{Guid.NewGuid():N}@vacaflow.test";

        using var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterAccountContract(
            "Functional Test User", email, password, "Employee"));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        return (body!.Id, email, password);
    }
}
