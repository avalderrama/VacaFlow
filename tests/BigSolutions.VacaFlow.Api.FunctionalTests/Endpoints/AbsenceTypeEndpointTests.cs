using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BigSolutions.VacaFlow.Api.Contracts;

namespace BigSolutions.VacaFlow.Api.FunctionalTests.Endpoints;

/// <summary>
/// Demonstrates both acceptance criteria of US-014 end-to-end, against the
/// real pipeline: the seeded catalog (TE-003) is already present when the
/// factory boots, and the FallbackPolicy rejects the endpoint with no code
/// change of its own — same pattern as CurrentUserEndpointTests (US-010).
/// </summary>
public sealed class AbsenceTypeEndpointTests(VacaFlowApiFactory factory) : IClassFixture<VacaFlowApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Without_A_Session_Returns_VF_AUT_004()
    {
        using var response = await _client.GetAsync("/api/absence-types");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-AUT-004", body.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("employee@vacaflow.test", "Employee123!")]
    [InlineData("manager@vacaflow.test", "Manager123!")]
    public async Task With_A_Session_Returns_The_Three_Seeded_Types_In_Alphabetical_Order_Regardless_Of_Role(
        string email, string password)
    {
        using var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login", new SignInContract(email, password));
        loginResponse.EnsureSuccessStatusCode();

        using var response = await _client.GetAsync("/api/absence-types");
        response.EnsureSuccessStatusCode();

        var types = await response.Content.ReadFromJsonAsync<List<AbsenceTypeResponse>>();

        Assert.NotNull(types);
        Assert.Equal(3, types.Count);
        Assert.Equal(["Personal Leave", "Sick Leave", "Vacation"], types.Select(type => type.Name));
        Assert.Equal(["PERSONAL_LEAVE", "SICK_LEAVE", "VACATION"], types.Select(type => type.Code));
        Assert.All(types, type => Assert.NotEqual(Guid.Empty, type.Id));
    }
}
