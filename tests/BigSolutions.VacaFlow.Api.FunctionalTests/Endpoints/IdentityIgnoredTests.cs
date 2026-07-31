using System.Net.Http.Json;
using System.Text.Json;
using BigSolutions.VacaFlow.Api.Contracts;
using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Requests;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>
    /// AC2 of US-021: the responsible manager on an Approval is the
    /// authenticated caller, never a payload value — an injected
    /// responsibleManagerId has no effect. Uses the seeded manager/employee
    /// pair (TE-003/Backlog.md §3.6 — Carlos assigned to Laura) since this
    /// is the only account pairing with a real manager assignment available
    /// without touching the seed.
    /// </summary>
    [Fact]
    public async Task Approve_Ignores_An_Injected_ResponsibleManagerId()
    {
        using var carlosLogin = await _client.PostAsJsonAsync("/api/auth/login", new SignInContract("employee@vacaflow.test", "Employee123!"));
        carlosLogin.EnsureSuccessStatusCode();

        using var typesResponse = await _client.GetAsync("/api/absence-types");
        typesResponse.EnsureSuccessStatusCode();
        var types = await typesResponse.Content.ReadFromJsonAsync<List<AbsenceTypeResponse>>();
        var typeId = types!.Single(type => type.Code == "VACATION").Id;

        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        using var createResponse = await _client.PostAsJsonAsync("/api/requests", new CreateRequestContract(
            typeId, startDate, startDate.AddDays(2), "Family trip"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        using var submitResponse = await _client.PostAsync($"/api/requests/{id}/submit", content: null);
        submitResponse.EnsureSuccessStatusCode();

        using var lauraLogin = await _client.PostAsJsonAsync("/api/auth/login", new SignInContract("manager@vacaflow.test", "Manager123!"));
        lauraLogin.EnsureSuccessStatusCode();
        var laura = await lauraLogin.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        var payload = new Dictionary<string, object?>
        {
            ["comment"] = "x",
            ["responsibleManagerId"] = Guid.NewGuid(),
        };

        using var approveResponse = await _client.PostAsJsonAsync($"/api/requests/{id}/approve", payload);
        approveResponse.EnsureSuccessStatusCode();

        await using var scope = factory.Services.CreateAsyncScope();
        var persisted = await scope.ServiceProvider.GetRequiredService<IRequestRepository>()
            .GetByIdAsync(new RequestId(id), CancellationToken.None);

        Assert.NotNull(persisted?.Approval);
        Assert.Equal(laura!.Id, persisted.Approval.ResponsibleManagerId.Value);
    }
}
