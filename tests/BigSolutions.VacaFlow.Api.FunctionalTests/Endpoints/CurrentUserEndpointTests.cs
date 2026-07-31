using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BigSolutions.VacaFlow.Api.Contracts;

namespace BigSolutions.VacaFlow.Api.FunctionalTests.Endpoints;

/// <summary>
/// Demonstrates both acceptance criteria of US-010 end-to-end, against the
/// real pipeline: the closed-cookie session established by register/login
/// resolves through <c>ICurrentUser</c> to the same identity, and the
/// FallbackPolicy from TE-011/US-009 rejects the endpoint with no code
/// change of its own.
/// </summary>
public sealed class CurrentUserEndpointTests(VacaFlowApiFactory factory) : IClassFixture<VacaFlowApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Me_Returns_The_Signed_In_Users_Own_Identity()
    {
        var email = $"{Guid.NewGuid():N}@vacaflow.test";

        using var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterAccountContract(
            "Current User Test", email, "Password123!", "Manager"));
        registerResponse.EnsureSuccessStatusCode();
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        using var meResponse = await _client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        Assert.NotNull(me);
        Assert.Equal(registered!.Id, me.Id);
        Assert.Equal("Current User Test", me.FullName);
        Assert.Equal(email, me.Email);
        Assert.Equal("Manager", me.Role);
    }

    /// <summary>
    /// "Never the password hash" is only verifiable by asserting the closed
    /// set of keys on the wire — the DTO and contract have no member that
    /// could carry one, but nothing short of reading the actual JSON proves
    /// a future field addition did not reintroduce the risk.
    /// </summary>
    [Fact]
    public async Task Me_Response_Body_Contains_Exactly_The_Four_Expected_Properties()
    {
        var email = $"{Guid.NewGuid():N}@vacaflow.test";

        using var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterAccountContract(
            "Closed Set Test", email, "Password123!", "Employee"));
        registerResponse.EnsureSuccessStatusCode();

        using var meResponse = await _client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();

        var json = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        var propertyNames = json.EnumerateObject().Select(property => property.Name).ToHashSet();

        Assert.Equal(new HashSet<string> { "id", "fullName", "email", "role" }, propertyNames);
    }

    [Fact]
    public async Task Me_Without_A_Session_Returns_VF_AUT_004()
    {
        using var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-AUT-004", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Me_After_Logout_Returns_VF_AUT_004()
    {
        var email = $"{Guid.NewGuid():N}@vacaflow.test";

        using var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterAccountContract(
            "Logout Test", email, "Password123!", "Employee"));
        registerResponse.EnsureSuccessStatusCode();

        using var logoutResponse = await _client.PostAsync("/api/auth/logout", content: null);
        logoutResponse.EnsureSuccessStatusCode();

        using var meResponse = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
        var body = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-AUT-004", body.GetProperty("code").GetString());
    }
}
