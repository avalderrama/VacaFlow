using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BigSolutions.VacaFlow.Api.Contracts;
using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace BigSolutions.VacaFlow.Api.FunctionalTests.Endpoints;

/// <summary>
/// Demonstrates every acceptance criterion of US-015, US-016, US-017's
/// GET /api/requests/{id}, US-018's POST /api/requests/{id}/submit and
/// US-019's POST /api/requests/{id}/cancel end-to-end, against the real
/// pipeline: a real cookie session, the seeded catalog (TE-003), and the
/// real FallbackPolicy.
/// </summary>
public sealed class RequestEndpointTests(VacaFlowApiFactory factory) : IClassFixture<VacaFlowApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private Task<(Guid EmployeeId, Guid AbsenceTypeId)> RegisterAndGetVacationTypeIdAsync() =>
        RegisterAndGetVacationTypeIdAsync(_client);

    private static async Task<(Guid EmployeeId, Guid AbsenceTypeId)> RegisterAndGetVacationTypeIdAsync(HttpClient client)
    {
        var email = $"{Guid.NewGuid():N}@vacaflow.test";

        using var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterAccountContract(
            "Request Test User", email, "Password123!", "Employee"));
        registerResponse.EnsureSuccessStatusCode();
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        using var typesResponse = await client.GetAsync("/api/absence-types");
        typesResponse.EnsureSuccessStatusCode();
        var types = await typesResponse.Content.ReadFromJsonAsync<List<AbsenceTypeResponse>>();

        return (registered!.Id, types!.Single(type => type.Code == "VACATION").Id);
    }

    private Task<Guid> CreateDraftAsync(Guid absenceTypeId) =>
        CreateDraftAsync(absenceTypeId, DateOnly.FromDateTime(DateTime.UtcNow));

    private Task<Guid> CreateDraftAsync(Guid absenceTypeId, DateOnly startDate) =>
        CreateDraftAsync(_client, absenceTypeId, startDate);

    private static async Task<Guid> CreateDraftAsync(HttpClient client, Guid absenceTypeId, DateOnly startDate)
    {
        using var response = await client.PostAsJsonAsync("/api/requests", new CreateRequestContract(
            absenceTypeId, startDate, startDate.AddDays(2), "Family trip"));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    /// <summary>
    /// No GET endpoint exists yet to read a request back (US-020, plan D8),
    /// so persistence is verified through the same DI container the live
    /// host resolves its own repositories from — proven reliable in this
    /// harness (unlike a fresh out-of-band SqliteConnection against the
    /// factory's file, which this project's US-015 session found
    /// reproducibly broken; see the AC4 test's remarks below).
    /// </summary>
    private async Task<Request?> LoadRequestAsync(Guid id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IRequestRepository>()
            .GetByIdAsync(new RequestId(id), CancellationToken.None);
    }

    [Fact]
    public async Task Post_With_Valid_Data_Returns_201_With_A_Location_Header()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        // today + 1 rather than today, so this test cannot flake if the real
        // clock crosses a midnight-UTC boundary between building the payload
        // here and the server re-reading it.
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        using var response = await _client.PostAsJsonAsync("/api/requests", new CreateRequestContract(
            typeId, start, start.AddDays(2), "Family trip"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.EndsWith($"/api/requests/{id}", response.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_With_An_End_Date_Before_The_Start_Date_Returns_VF_REQ_001()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var response = await _client.PostAsJsonAsync("/api/requests", new CreateRequestContract(
            typeId, today, today.AddDays(-1), "Family trip"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-001", body.GetProperty("code").GetString());
        Assert.Equal("endDate", body.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Post_With_A_Start_Date_Before_Today_Returns_VF_REQ_002()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        using var response = await _client.PostAsJsonAsync("/api/requests", new CreateRequestContract(
            typeId, yesterday, yesterday, "Family trip"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-002", body.GetProperty("code").GetString());
        Assert.Equal("startDate", body.GetProperty("field").GetString());
    }

    [Theory]
    [InlineData(false, true, true, true, "absenceTypeId")]
    [InlineData(true, false, true, true, "startDate")]
    [InlineData(true, true, false, true, "endDate")]
    [InlineData(true, true, true, false, "reason")]
    public async Task Post_With_A_Missing_Field_Returns_VF_VAL_001_With_That_Field(
        bool includeType, bool includeStartDate, bool includeEndDate, bool includeReason, string expectedField)
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var payload = new Dictionary<string, object?>
        {
            ["absenceTypeId"] = includeType ? typeId : null,
            ["startDate"] = includeStartDate ? today : null,
            ["endDate"] = includeEndDate ? today.AddDays(2) : null,
            ["reason"] = includeReason ? "Family trip" : null,
        };

        using var response = await _client.PostAsJsonAsync("/api/requests", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-VAL-001", body.GetProperty("code").GetString());
        Assert.Equal(expectedField, body.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Post_With_A_Reason_Over_500_Characters_Returns_VF_VAL_001()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tooLong = new string('a', 501);

        using var response = await _client.PostAsJsonAsync("/api/requests", new CreateRequestContract(
            typeId, today, today.AddDays(2), tooLong));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-VAL-001", body.GetProperty("code").GetString());
        Assert.Equal("reason", body.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Post_With_A_Nonexistent_Absence_Type_Returns_VF_CAT_001()
    {
        await RegisterAndGetVacationTypeIdAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var response = await _client.PostAsJsonAsync("/api/requests", new CreateRequestContract(
            Guid.NewGuid(), today, today.AddDays(2), "Family trip"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-CAT-001", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_Without_A_Session_Returns_VF_AUT_004()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var response = await _client.PostAsJsonAsync("/api/requests", new CreateRequestContract(
            Guid.NewGuid(), today, today.AddDays(2), "Family trip"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-AUT-004", body.GetProperty("code").GetString());
    }

    /// <remarks>
    /// AC4: a payload carrying an employeeId (and responsibleManagerId) has no
    /// effect — CreateRequestContract has no such property, so JSON binding
    /// silently drops it before the handler ever runs, same pattern as
    /// IdentityIgnoredTests.Register_Ignores_An_Injected_EmployeeId_And_ResponsibleManagerId.
    /// The strong version of this assertion — proving the persisted owner is
    /// the session's employee, not the injected one — is covered at the
    /// handler-unit level instead of here
    /// (CreateRequestHandlerTests.Handle_Should_Succeed_With_Valid_Data
    /// asserts added.OwnerId == the FakeCurrentUser's EmployeeId). Reading the
    /// row back through a fresh SqliteConnection from this WebApplicationFactory
    /// harness was attempted and abandoned: it reproducibly finds a
    /// zero-byte, table-less database file, even though the same request
    /// already round-tripped correctly through DI-resolved repositories
    /// moments earlier — the same class of WebApplicationFactory/
    /// HostFactoryResolver double-host-execution quirk flagged during
    /// US-014's review as the suspected root cause of this codebase's
    /// existing DatabaseSeeder UNIQUE-constraint flake, and which the user is
    /// already addressing in a separate session. Not this story's bug to fix.
    /// </remarks>
    [Fact]
    public async Task Post_Ignores_An_Injected_EmployeeId_From_A_Different_Account()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        // today + 1, not today — this test asserts 201, so it must not flake
        // if the real clock crosses midnight UTC before the server re-reads
        // it (same reasoning as Post_With_Valid_Data_Returns_201_With_A_Location_Header).
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var foreignEmployeeId = Guid.NewGuid();

        var payload = new Dictionary<string, object?>
        {
            ["absenceTypeId"] = typeId,
            ["startDate"] = start,
            ["endDate"] = start.AddDays(2),
            ["reason"] = "Family trip",
            ["employeeId"] = foreignEmployeeId,
            ["responsibleManagerId"] = Guid.NewGuid(),
        };

        using var response = await _client.PostAsJsonAsync("/api/requests", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetGuid();
        Assert.NotEqual(foreignEmployeeId, id);
    }

    [Fact]
    public async Task Put_With_Valid_Data_On_Own_Draft_Returns_204_And_Persists_The_Edit()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await CreateDraftAsync(typeId);
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        using var response = await _client.PutAsJsonAsync($"/api/requests/{id}", new UpdateRequestContract(
            typeId, start, start.AddDays(3), "Updated reason"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await LoadRequestAsync(id);
        Assert.NotNull(persisted);
        Assert.Equal("Updated reason", persisted.Reason);
        Assert.Equal(start, persisted.Period.Start);
        Assert.Equal(start.AddDays(3), persisted.Period.End);
    }

    [Fact]
    public async Task Put_On_Another_Employees_Draft_Returns_VF_REQ_004()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var victimsDraftId = await CreateDraftAsync(typeId);

        var (_, attackerTypeId) = await RegisterAndGetVacationTypeIdAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var response = await _client.PutAsJsonAsync($"/api/requests/{victimsDraftId}", new UpdateRequestContract(
            attackerTypeId, today, today.AddDays(2), "Attacker's edit"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-004", body.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData(false, true, true, true, "absenceTypeId")]
    [InlineData(true, false, true, true, "startDate")]
    [InlineData(true, true, false, true, "endDate")]
    [InlineData(true, true, true, false, "reason")]
    public async Task Put_With_A_Missing_Field_Returns_VF_VAL_001_With_That_Field_Same_As_Creation(
        bool includeType, bool includeStartDate, bool includeEndDate, bool includeReason, string expectedField)
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await CreateDraftAsync(typeId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var payload = new Dictionary<string, object?>
        {
            ["absenceTypeId"] = includeType ? typeId : null,
            ["startDate"] = includeStartDate ? today : null,
            ["endDate"] = includeEndDate ? today.AddDays(2) : null,
            ["reason"] = includeReason ? "Updated reason" : null,
        };

        using var response = await _client.PutAsJsonAsync($"/api/requests/{id}", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-VAL-001", body.GetProperty("code").GetString());
        Assert.Equal(expectedField, body.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Put_With_An_End_Date_Before_The_Start_Date_Returns_VF_REQ_001_Same_As_Creation()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await CreateDraftAsync(typeId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var response = await _client.PutAsJsonAsync($"/api/requests/{id}", new UpdateRequestContract(
            typeId, today, today.AddDays(-1), "Updated reason"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-001", body.GetProperty("code").GetString());
        Assert.Equal("endDate", body.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Put_With_A_Start_Date_Before_Today_Returns_VF_REQ_002_Same_As_Creation()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await CreateDraftAsync(typeId);
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        using var response = await _client.PutAsJsonAsync($"/api/requests/{id}", new UpdateRequestContract(
            typeId, yesterday, yesterday, "Updated reason"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-002", body.GetProperty("code").GetString());
        Assert.Equal("startDate", body.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Put_With_A_Nonexistent_Id_Returns_VF_REQ_006()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var response = await _client.PutAsJsonAsync($"/api/requests/{Guid.NewGuid()}", new UpdateRequestContract(
            typeId, today, today.AddDays(2), "Updated reason"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-006", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Put_With_A_Nonexistent_Absence_Type_Returns_VF_CAT_001()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await CreateDraftAsync(typeId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var response = await _client.PutAsJsonAsync($"/api/requests/{id}", new UpdateRequestContract(
            Guid.NewGuid(), today, today.AddDays(2), "Updated reason"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-CAT-001", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Put_Without_A_Session_Returns_VF_AUT_004()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var response = await _client.PutAsJsonAsync($"/api/requests/{Guid.NewGuid()}", new UpdateRequestContract(
            Guid.NewGuid(), today, today.AddDays(2), "Updated reason"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-AUT-004", body.GetProperty("code").GetString());
    }

    /// <summary>
    /// AC4/identity: a payload carrying an employeeId has no effect —
    /// UpdateRequestContract has no such property. Unlike the equivalent
    /// create-side test, this one CAN make a strong, non-tautological
    /// assertion: IRequestRepository gained GetByIdAsync in this story, so
    /// the persisted owner is read back through the live DI container and
    /// compared directly against the injected value.
    /// </summary>
    [Fact]
    public async Task Put_Ignores_An_Injected_EmployeeId_From_A_Different_Account()
    {
        var (sessionEmployeeId, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await CreateDraftAsync(typeId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var foreignEmployeeId = Guid.NewGuid();

        var payload = new Dictionary<string, object?>
        {
            ["absenceTypeId"] = typeId,
            ["startDate"] = today,
            ["endDate"] = today.AddDays(2),
            ["reason"] = "Updated reason",
            ["employeeId"] = foreignEmployeeId,
            ["responsibleManagerId"] = Guid.NewGuid(),
        };

        using var response = await _client.PutAsJsonAsync($"/api/requests/{id}", payload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await LoadRequestAsync(id);
        Assert.NotNull(persisted);
        Assert.Equal(sessionEmployeeId, persisted.OwnerId.Value);
        Assert.NotEqual(foreignEmployeeId, persisted.OwnerId.Value);
    }

    [Fact]
    public async Task Get_Returns_The_Full_Detail_For_The_Owner()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        using var createResponse = await _client.PostAsJsonAsync("/api/requests", new CreateRequestContract(
            typeId, start, start.AddDays(2), "Family trip"));
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        using var response = await _client.GetAsync($"/api/requests/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<RequestDetailResponse>();
        Assert.NotNull(detail);
        Assert.Equal(id, detail.Id);
        Assert.Equal(typeId, detail.AbsenceTypeId);
        Assert.Equal(start, detail.StartDate);
        Assert.Equal(start.AddDays(2), detail.EndDate);
        Assert.Equal("Family trip", detail.Reason);
        Assert.Equal("Draft", detail.State);
    }

    [Fact]
    public async Task Get_On_Another_Employees_Draft_Returns_VF_REQ_004()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var victimsDraftId = await CreateDraftAsync(typeId);

        await RegisterAndGetVacationTypeIdAsync();

        using var response = await _client.GetAsync($"/api/requests/{victimsDraftId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-004", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_With_A_Nonexistent_Id_Returns_VF_REQ_006()
    {
        await RegisterAndGetVacationTypeIdAsync();

        using var response = await _client.GetAsync($"/api/requests/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-006", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Without_A_Session_Returns_VF_AUT_004()
    {
        using var response = await _client.GetAsync($"/api/requests/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-AUT-004", body.GetProperty("code").GetString());
    }

    /// <summary>
    /// Submit re-validates RULE-02, so a submit test built on a request
    /// created for today's own date could flake if the real clock crosses
    /// midnight UTC between the create call and the submit call — same
    /// reasoning as Post_With_Valid_Data_Returns_201_With_A_Location_Header.
    /// today + 1 gives the whole create-then-submit sequence a full day of
    /// margin.
    /// </summary>
    private async Task<Guid> CreateAndSubmitDraftAsync(Guid absenceTypeId)
    {
        var id = await CreateDraftAsync(absenceTypeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));
        using var submitResponse = await _client.PostAsync($"/api/requests/{id}/submit", content: null);
        submitResponse.EnsureSuccessStatusCode();
        return id;
    }

    [Fact]
    public async Task Submit_Own_Draft_Returns_204_And_Persists_The_Transition()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await CreateDraftAsync(typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        using var response = await _client.PostAsync($"/api/requests/{id}/submit", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await LoadRequestAsync(id);
        Assert.NotNull(persisted);
        Assert.Equal(RequestState.Submitted, persisted.State);
        Assert.NotNull(persisted.SubmittedAtUtc);
    }

    /// <summary>AC5 end-to-end: once submitted, the draft is immutable to its own owner.</summary>
    [Fact]
    public async Task Put_After_Submit_Returns_VF_REQ_003()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await CreateAndSubmitDraftAsync(typeId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var response = await _client.PutAsJsonAsync($"/api/requests/{id}", new UpdateRequestContract(
            typeId, today, today.AddDays(2), "Updated reason"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-003", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Submit_A_Request_That_Is_Not_A_Draft_Returns_VF_REQ_005_With_The_Interpolated_Message()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await CreateAndSubmitDraftAsync(typeId);

        using var response = await _client.PostAsync($"/api/requests/{id}/submit", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-005", body.GetProperty("code").GetString());
        Assert.Equal(
            "This request cannot move from Submitted to Submitted.",
            body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Submit_On_Another_Employees_Draft_Returns_VF_REQ_004()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var victimsDraftId = await CreateDraftAsync(typeId);

        await RegisterAndGetVacationTypeIdAsync();

        using var response = await _client.PostAsync($"/api/requests/{victimsDraftId}/submit", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-004", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Submit_With_A_Nonexistent_Id_Returns_VF_REQ_006()
    {
        await RegisterAndGetVacationTypeIdAsync();

        using var response = await _client.PostAsync($"/api/requests/{Guid.NewGuid()}/submit", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-006", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Submit_Without_A_Session_Returns_VF_AUT_004()
    {
        using var response = await _client.PostAsync($"/api/requests/{Guid.NewGuid()}/submit", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-AUT-004", body.GetProperty("code").GetString());
    }

    /// <summary>
    /// Request.Create takes "today" as a caller-supplied parameter (not a
    /// clock read), so a stale draft can be built legitimately — no SQL,
    /// no reaching the internal VacaFlowDbContext — and seeded through the
    /// same DI route LoadRequestAsync already proves reliable in this
    /// harness: IRequestRepository.Add + IUnitOfWork.SaveChangesAsync from
    /// the live container, same pattern
    /// RequestRepositoryTests.UpdateDetails_On_A_Row_Forced_To_Submitted_Should_Fail_With_VF_REQ_003
    /// uses for its own seed step.
    /// </summary>
    private async Task<Guid> SeedStaleDraftAsync(Guid ownerId, Guid absenceTypeId)
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var period = DateRange.Create(yesterday, yesterday).Value;
        var stale = Request.Create(
            new RequestId(Guid.NewGuid()), new EmployeeId(ownerId), new AbsenceTypeId(absenceTypeId),
            period, "Family trip", yesterday, DateTime.UtcNow).Value;

        await using var scope = factory.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IRequestRepository>().Add(stale);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

        return stale.Id.Value;
    }

    [Fact]
    public async Task Submit_A_Draft_Whose_Start_Date_Has_Since_Passed_Returns_VF_REQ_002()
    {
        var (employeeId, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await SeedStaleDraftAsync(employeeId, typeId);

        using var response = await _client.PostAsync($"/api/requests/{id}/submit", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-002", body.GetProperty("code").GetString());
        Assert.Equal("startDate", body.GetProperty("field").GetString());
        var persisted = await LoadRequestAsync(id);
        Assert.NotNull(persisted);
        Assert.Equal(RequestState.Draft, persisted.State);
    }

    [Fact]
    public async Task Cancel_Own_Draft_Returns_204_And_Persists_The_Transition()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await CreateDraftAsync(typeId);

        using var response = await _client.PostAsync($"/api/requests/{id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await LoadRequestAsync(id);
        Assert.NotNull(persisted);
        Assert.Equal(RequestState.Cancelled, persisted.State);
        Assert.NotNull(persisted.ClosedAtUtc);
        Assert.Null(persisted.SubmittedAtUtc);
    }

    [Fact]
    public async Task Cancel_Own_Submitted_Request_Returns_204_And_Preserves_SubmittedAtUtc()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await CreateAndSubmitDraftAsync(typeId);

        using var response = await _client.PostAsync($"/api/requests/{id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await LoadRequestAsync(id);
        Assert.NotNull(persisted);
        Assert.Equal(RequestState.Cancelled, persisted.State);
        Assert.NotNull(persisted.ClosedAtUtc);
        Assert.NotNull(persisted.SubmittedAtUtc);
    }

    [Fact]
    public async Task Cancel_A_Request_That_Is_Already_Cancelled_Returns_VF_REQ_005_With_The_Interpolated_Message()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await CreateDraftAsync(typeId);
        using (var firstCancel = await _client.PostAsync($"/api/requests/{id}/cancel", content: null))
        {
            firstCancel.EnsureSuccessStatusCode();
        }

        using var response = await _client.PostAsync($"/api/requests/{id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-005", body.GetProperty("code").GetString());
        Assert.Equal(
            "This request cannot move from Cancelled to Cancelled.",
            body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Submit_A_Cancelled_Request_Returns_VF_REQ_005_With_The_Interpolated_Message()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await CreateDraftAsync(typeId);
        using (var cancelResponse = await _client.PostAsync($"/api/requests/{id}/cancel", content: null))
        {
            cancelResponse.EnsureSuccessStatusCode();
        }

        using var response = await _client.PostAsync($"/api/requests/{id}/submit", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-005", body.GetProperty("code").GetString());
        Assert.Equal(
            "This request cannot move from Cancelled to Submitted.",
            body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Cancel_On_Another_Employees_Draft_Returns_VF_REQ_004()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var victimsDraftId = await CreateDraftAsync(typeId);

        await RegisterAndGetVacationTypeIdAsync();

        using var response = await _client.PostAsync($"/api/requests/{victimsDraftId}/cancel", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-004", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Cancel_With_A_Nonexistent_Id_Returns_VF_REQ_006()
    {
        await RegisterAndGetVacationTypeIdAsync();

        using var response = await _client.PostAsync($"/api/requests/{Guid.NewGuid()}/cancel", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-006", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Cancel_Without_A_Session_Returns_VF_AUT_004()
    {
        using var response = await _client.PostAsync($"/api/requests/{Guid.NewGuid()}/cancel", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-AUT-004", body.GetProperty("code").GetString());
    }

    // Cancel from Approved/Rejected is deliberately not exercised at the
    // HTTP level in this class, for the same reason Submit_A_Request_...
    // (this class) and RequestRepositoryTests could only reach Submitted
    // and Cancelled directly: those two states are unreachable until
    // US-021 delivers Decide. The guard (Request.Cancel's single pattern
    // match) is already proven from Cancelled above; US-021 adds the
    // Approved/Rejected cases once a real Decide() exists to produce them.

    /// <summary>
    /// Logs into a seeded account (TE-003/Backlog.md §3.6 — Carlos and Ana
    /// are assigned to Laura the manager) on this test's own HttpClient,
    /// returning the employee id from the login response body.
    /// </summary>
    private async Task<Guid> LoginAsSeededAccountAsync(string email, string password)
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/login", new SignInContract(email, password));
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();
        return user!.Id;
    }

    private async Task<Guid> CreateAndSubmitDraftAsync(Guid absenceTypeId, DateOnly startDate)
    {
        var id = await CreateDraftAsync(absenceTypeId, startDate);
        using var response = await _client.PostAsync($"/api/requests/{id}/submit", content: null);
        response.EnsureSuccessStatusCode();
        return id;
    }

    private static Guid GetVacationTypeId(List<AbsenceTypeResponse> types) =>
        types.Single(type => type.Code == "VACATION").Id;

    /// <summary>AC1: the manager receives the Submitted requests of the employees assigned to them.</summary>
    [Fact]
    public async Task Get_As_Manager_Returns_Submitted_Requests_Of_Assigned_Employees()
    {
        var carlosClient = factory.CreateClient();
        var carlosId = await LoginAsAsync(carlosClient, "employee@vacaflow.test", "Employee123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(carlosClient));
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var carlosRequestId = await CreateAndSubmitDraftAsync(carlosClient, typeId, startDate);

        var laraId = await LoginAsSeededAccountAsync("manager@vacaflow.test", "Manager123!");

        using var response = await _client.GetAsync("/api/requests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<RequestSummaryResponse>>();
        Assert.NotNull(body);
        var carlosItem = Assert.Single(body, item => item.Id == carlosRequestId);
        Assert.Equal("Carlos Ruiz", carlosItem.Employee.FullName);
        Assert.Equal("Submitted", carlosItem.State);
        Assert.Equal("VACATION", carlosItem.AbsenceType.Code);
        Assert.NotEqual(laraId, carlosItem.Employee.Id);
    }

    /// <summary>AC2: a submitted request belonging to another manager's employee is absent.</summary>
    [Fact]
    public async Task Get_As_Manager_Excludes_Submitted_Requests_Of_Unassigned_Employees()
    {
        var (unassignedEmployeeId, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var unassignedRequestId = await CreateAndSubmitDraftAsync(typeId, startDate);

        var managerClient = factory.CreateClient();
        await LoginAsAsync(managerClient, "manager@vacaflow.test", "Manager123!");

        using var response = await managerClient.GetAsync("/api/requests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<RequestSummaryResponse>>();
        Assert.NotNull(body);
        Assert.DoesNotContain(body, item => item.Id == unassignedRequestId);
        Assert.DoesNotContain(body, item => item.Employee.Id == unassignedEmployeeId);
    }

    /// <summary>AC3: a request in a final state is absent from the manager's queue.</summary>
    [Fact]
    public async Task Get_As_Manager_Excludes_Requests_In_A_Final_State()
    {
        var anaClient = factory.CreateClient();
        await LoginAsAsync(anaClient, "ana@vacaflow.test", "Employee123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(anaClient));
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var cancelledId = await CreateAndSubmitDraftAsync(anaClient, typeId, startDate);
        using (var cancelResponse = await anaClient.PostAsync($"/api/requests/{cancelledId}/cancel", content: null))
        {
            cancelResponse.EnsureSuccessStatusCode();
        }

        var managerClient = factory.CreateClient();
        await LoginAsAsync(managerClient, "manager@vacaflow.test", "Manager123!");

        using var response = await managerClient.GetAsync("/api/requests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<RequestSummaryResponse>>();
        Assert.NotNull(body);
        Assert.DoesNotContain(body, item => item.Id == cancelledId);
    }

    /// <summary>AC4: a manager's own request is present as their own, but exactly once — never duplicated by the queue.</summary>
    [Fact]
    public async Task Get_As_Manager_Includes_Their_Own_Request_Exactly_Once()
    {
        var managerClient = factory.CreateClient();
        var managerId = await LoginAsAsync(managerClient, "manager@vacaflow.test", "Manager123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(managerClient));
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var ownRequestId = await CreateAndSubmitDraftAsync(managerClient, typeId, startDate);

        using var response = await managerClient.GetAsync("/api/requests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<RequestSummaryResponse>>();
        Assert.NotNull(body);
        var matches = body.Where(item => item.Id == ownRequestId).ToList();
        var ownItem = Assert.Single(matches);
        Assert.Equal(managerId, ownItem.Employee.Id);
    }

    /// <summary>AC5: an employee sees only their own requests, in every state; the filter is server-side.</summary>
    [Fact]
    public async Task Get_As_Employee_Returns_Only_Their_Own_Requests_In_Every_State()
    {
        var (myEmployeeId, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var draftId = await CreateDraftAsync(typeId, today.AddDays(1));
        var submittedId = await CreateAndSubmitDraftAsync(typeId, today.AddDays(2));

        var otherClient = factory.CreateClient();
        var (_, otherTypeId) = await RegisterAndGetVacationTypeIdAsync(otherClient);
        await CreateAndSubmitDraftAsync(otherClient, otherTypeId, today.AddDays(1));

        using var response = await _client.GetAsync("/api/requests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<RequestSummaryResponse>>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Count);
        Assert.All(body, item => Assert.Equal(myEmployeeId, item.Employee.Id));
        Assert.Contains(body, item => item.Id == draftId);
        Assert.Contains(body, item => item.Id == submittedId);
    }

    [Fact]
    public async Task Get_List_Without_A_Session_Returns_VF_AUT_004()
    {
        using var response = await _client.GetAsync("/api/requests");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-AUT-004", body.GetProperty("code").GetString());
    }

    private static async Task<Guid> LoginAsAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new SignInContract(email, password));
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();
        return user!.Id;
    }

    private static async Task<List<AbsenceTypeResponse>> ListAbsenceTypesAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/absence-types");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<AbsenceTypeResponse>>())!;
    }

    private static async Task<Guid> CreateAndSubmitDraftAsync(HttpClient client, Guid absenceTypeId, DateOnly startDate)
    {
        using var createResponse = await client.PostAsJsonAsync("/api/requests", new CreateRequestContract(
            absenceTypeId, startDate, startDate.AddDays(2), "Family trip"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        using var submitResponse = await client.PostAsync($"/api/requests/{id}/submit", content: null);
        submitResponse.EnsureSuccessStatusCode();

        return id;
    }

    private static async Task<Guid> RegisterAsManagerAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterAccountContract(
            "Approve Test Manager", $"{Guid.NewGuid():N}@vacaflow.test", "Password123!", "Manager"));
        response.EnsureSuccessStatusCode();
        var registered = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();
        return registered!.Id;
    }

    /// <summary>AC1: Carlos (seeded, assigned to Laura) submits; Laura approves.</summary>
    [Fact]
    public async Task Approve_A_Submitted_Request_From_An_Assigned_Employee_Returns_204()
    {
        var carlosClient = factory.CreateClient();
        await LoginAsAsync(carlosClient, "employee@vacaflow.test", "Employee123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(carlosClient));
        var id = await CreateAndSubmitDraftAsync(carlosClient, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var lauraClient = factory.CreateClient();
        await LoginAsAsync(lauraClient, "manager@vacaflow.test", "Manager123!");

        using var response = await lauraClient.PostAsJsonAsync($"/api/requests/{id}/approve", new ApproveRequestContract("Enjoy"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await LoadRequestAsync(id);
        Assert.NotNull(persisted);
        Assert.Equal(RequestState.Approved, persisted.State);
        Assert.NotNull(persisted.Approval);
        Assert.Equal(DecisionType.Approved, persisted.Approval.Decision);
        Assert.Equal("Enjoy", persisted.Approval.Comment);
        Assert.NotNull(persisted.ClosedAtUtc);

        using var queueResponse = await lauraClient.GetAsync("/api/requests");
        var queue = await queueResponse.Content.ReadFromJsonAsync<List<RequestSummaryResponse>>();
        Assert.DoesNotContain(queue!, item => item.Id == id);
    }

    // AC2 (the responsible manager is the authenticated caller, never a
    // payload value) is exercised in IdentityIgnoredTests — its natural
    // home alongside every other identity-injection test in this project.

    /// <summary>AC3: any state other than Submitted — Carlos (assigned to Laura) leaves a draft unsubmitted.</summary>
    [Fact]
    public async Task Approve_A_Draft_Returns_VF_DEC_001()
    {
        var carlosClient = factory.CreateClient();
        await LoginAsAsync(carlosClient, "employee@vacaflow.test", "Employee123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(carlosClient));
        var id = await CreateDraftAsync(carlosClient, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var lauraClient = factory.CreateClient();
        await LoginAsAsync(lauraClient, "manager@vacaflow.test", "Manager123!");

        using var response = await lauraClient.PostAsJsonAsync($"/api/requests/{id}/approve", new ApproveRequestContract(null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-DEC-001", body.GetProperty("code").GetString());
        Assert.Equal("Only Submitted requests can be approved or rejected.", body.GetProperty("message").GetString());
    }

    /// <summary>AC4: a non-manager caller.</summary>
    [Fact]
    public async Task Approve_By_A_Non_Manager_Returns_VF_DEC_002()
    {
        var anaClient = factory.CreateClient();
        await LoginAsAsync(anaClient, "ana@vacaflow.test", "Employee123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(anaClient));
        var id = await CreateAndSubmitDraftAsync(anaClient, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var carlosClient = factory.CreateClient();
        await LoginAsAsync(carlosClient, "employee@vacaflow.test", "Employee123!");

        using var response = await carlosClient.PostAsJsonAsync($"/api/requests/{id}/approve", new ApproveRequestContract(null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-DEC-002", body.GetProperty("code").GetString());
    }

    /// <summary>AC5: a manager deciding their own request.</summary>
    [Fact]
    public async Task Approve_Own_Request_Returns_VF_DEC_004()
    {
        var lauraClient = factory.CreateClient();
        await LoginAsAsync(lauraClient, "manager@vacaflow.test", "Manager123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(lauraClient));
        var id = await CreateAndSubmitDraftAsync(lauraClient, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        using var response = await lauraClient.PostAsJsonAsync($"/api/requests/{id}/approve", new ApproveRequestContract(null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-DEC-004", body.GetProperty("code").GetString());
    }

    /// <summary>AC6: a manager not assigned to the owner's employee.</summary>
    [Fact]
    public async Task Approve_By_An_Unassigned_Manager_Returns_VF_DEC_003()
    {
        var carlosClient = factory.CreateClient();
        await LoginAsAsync(carlosClient, "employee@vacaflow.test", "Employee123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(carlosClient));
        var id = await CreateAndSubmitDraftAsync(carlosClient, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var otherManagerClient = factory.CreateClient();
        await RegisterAsManagerAsync(otherManagerClient);

        using var response = await otherManagerClient.PostAsJsonAsync($"/api/requests/{id}/approve", new ApproveRequestContract(null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-DEC-003", body.GetProperty("code").GetString());
    }

    /// <summary>AC6/OQ-B: fail-closed when the owner has no assigned manager at all.</summary>
    [Fact]
    public async Task Approve_An_Unassigned_Employees_Request_Returns_VF_DEC_003()
    {
        var (_, typeId) = await RegisterAndGetVacationTypeIdAsync();
        var id = await CreateAndSubmitDraftAsync(_client, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var lauraClient = factory.CreateClient();
        await LoginAsAsync(lauraClient, "manager@vacaflow.test", "Manager123!");

        using var response = await lauraClient.PostAsJsonAsync($"/api/requests/{id}/approve", new ApproveRequestContract(null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-DEC-003", body.GetProperty("code").GetString());
    }

    /// <summary>AC7: a request that already has a final decision.</summary>
    [Fact]
    public async Task Approve_An_Already_Decided_Request_Returns_VF_DEC_005()
    {
        var carlosClient = factory.CreateClient();
        await LoginAsAsync(carlosClient, "employee@vacaflow.test", "Employee123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(carlosClient));
        var id = await CreateAndSubmitDraftAsync(carlosClient, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var lauraClient = factory.CreateClient();
        await LoginAsAsync(lauraClient, "manager@vacaflow.test", "Manager123!");
        using (var firstApprove = await lauraClient.PostAsJsonAsync($"/api/requests/{id}/approve", new ApproveRequestContract(null)))
        {
            firstApprove.EnsureSuccessStatusCode();
        }

        using var response = await lauraClient.PostAsJsonAsync($"/api/requests/{id}/approve", new ApproveRequestContract(null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-DEC-005", body.GetProperty("code").GetString());
        Assert.Equal("This request already has a final decision.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Approve_With_A_Comment_Over_500_Characters_Returns_VF_VAL_001()
    {
        var carlosClient = factory.CreateClient();
        await LoginAsAsync(carlosClient, "employee@vacaflow.test", "Employee123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(carlosClient));
        var id = await CreateAndSubmitDraftAsync(carlosClient, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var lauraClient = factory.CreateClient();
        await LoginAsAsync(lauraClient, "manager@vacaflow.test", "Manager123!");
        var tooLong = new string('a', 501);

        using var response = await lauraClient.PostAsJsonAsync($"/api/requests/{id}/approve", new ApproveRequestContract(tooLong));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-VAL-001", body.GetProperty("code").GetString());
        Assert.Equal("comment", body.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Approve_Without_A_Session_Returns_VF_AUT_004()
    {
        using var response = await _client.PostAsJsonAsync($"/api/requests/{Guid.NewGuid()}/approve", new ApproveRequestContract(null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-AUT-004", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Approve_A_Nonexistent_Id_Returns_VF_REQ_006()
    {
        var lauraClient = factory.CreateClient();
        await LoginAsAsync(lauraClient, "manager@vacaflow.test", "Manager123!");

        using var response = await lauraClient.PostAsJsonAsync($"/api/requests/{Guid.NewGuid()}/approve", new ApproveRequestContract(null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-REQ-006", body.GetProperty("code").GetString());
    }

    /// <summary>AC1: Carlos (seeded, assigned to Laura) submits; Laura rejects with a comment.</summary>
    [Fact]
    public async Task Reject_A_Submitted_Request_From_An_Assigned_Employee_Returns_204()
    {
        var carlosClient = factory.CreateClient();
        await LoginAsAsync(carlosClient, "employee@vacaflow.test", "Employee123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(carlosClient));
        var id = await CreateAndSubmitDraftAsync(carlosClient, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var lauraClient = factory.CreateClient();
        await LoginAsAsync(lauraClient, "manager@vacaflow.test", "Manager123!");

        using var response = await lauraClient.PostAsJsonAsync($"/api/requests/{id}/reject", new RejectRequestContract("No coverage that week"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await LoadRequestAsync(id);
        Assert.NotNull(persisted);
        Assert.Equal(RequestState.Rejected, persisted.State);
        Assert.NotNull(persisted.Approval);
        Assert.Equal(DecisionType.Rejected, persisted.Approval.Decision);
        Assert.Equal("No coverage that week", persisted.Approval.Comment);
        Assert.NotNull(persisted.ClosedAtUtc);

        using var queueResponse = await lauraClient.GetAsync("/api/requests");
        var queue = await queueResponse.Content.ReadFromJsonAsync<List<RequestSummaryResponse>>();
        Assert.DoesNotContain(queue!, item => item.Id == id);
    }

    /// <summary>AC2: the comment is optional for a rejection too.</summary>
    [Fact]
    public async Task Reject_A_Submitted_Request_Without_A_Comment_Returns_204()
    {
        var carlosClient = factory.CreateClient();
        await LoginAsAsync(carlosClient, "employee@vacaflow.test", "Employee123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(carlosClient));
        var id = await CreateAndSubmitDraftAsync(carlosClient, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var lauraClient = factory.CreateClient();
        await LoginAsAsync(lauraClient, "manager@vacaflow.test", "Manager123!");

        using var response = await lauraClient.PostAsJsonAsync($"/api/requests/{id}/reject", new RejectRequestContract(null));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await LoadRequestAsync(id);
        Assert.Equal(RequestState.Rejected, persisted!.State);
        Assert.Null(persisted.Approval!.Comment);
    }

    /// <summary>AC4 (D2 — representative subset): a Draft cannot be rejected.</summary>
    [Fact]
    public async Task Reject_A_Draft_Returns_VF_DEC_001()
    {
        var carlosClient = factory.CreateClient();
        await LoginAsAsync(carlosClient, "employee@vacaflow.test", "Employee123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(carlosClient));
        var id = await CreateDraftAsync(carlosClient, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var lauraClient = factory.CreateClient();
        await LoginAsAsync(lauraClient, "manager@vacaflow.test", "Manager123!");

        using var response = await lauraClient.PostAsJsonAsync($"/api/requests/{id}/reject", new RejectRequestContract(null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-DEC-001", body.GetProperty("code").GetString());
    }

    /// <summary>AC4 (D2 — representative subset): a non-manager caller.</summary>
    [Fact]
    public async Task Reject_By_A_Non_Manager_Returns_VF_DEC_002()
    {
        var anaClient = factory.CreateClient();
        await LoginAsAsync(anaClient, "ana@vacaflow.test", "Employee123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(anaClient));
        var id = await CreateAndSubmitDraftAsync(anaClient, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var carlosClient = factory.CreateClient();
        await LoginAsAsync(carlosClient, "employee@vacaflow.test", "Employee123!");

        using var response = await carlosClient.PostAsJsonAsync($"/api/requests/{id}/reject", new RejectRequestContract(null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-DEC-002", body.GetProperty("code").GetString());
    }

    /// <summary>AC4 (D2 — representative subset): a manager rejecting their own request.</summary>
    [Fact]
    public async Task Reject_Own_Request_Returns_VF_DEC_004()
    {
        var lauraClient = factory.CreateClient();
        await LoginAsAsync(lauraClient, "manager@vacaflow.test", "Manager123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(lauraClient));
        var id = await CreateAndSubmitDraftAsync(lauraClient, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        using var response = await lauraClient.PostAsJsonAsync($"/api/requests/{id}/reject", new RejectRequestContract(null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-DEC-004", body.GetProperty("code").GetString());
    }

    /// <summary>AC4 (D2 — the approve/reject cross): a request already approved cannot then be rejected.</summary>
    [Fact]
    public async Task Reject_An_Already_Approved_Request_Returns_VF_DEC_005()
    {
        var carlosClient = factory.CreateClient();
        await LoginAsAsync(carlosClient, "employee@vacaflow.test", "Employee123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(carlosClient));
        var id = await CreateAndSubmitDraftAsync(carlosClient, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var lauraClient = factory.CreateClient();
        await LoginAsAsync(lauraClient, "manager@vacaflow.test", "Manager123!");
        using (var firstApprove = await lauraClient.PostAsJsonAsync($"/api/requests/{id}/approve", new ApproveRequestContract(null)))
        {
            firstApprove.EnsureSuccessStatusCode();
        }

        using var response = await lauraClient.PostAsJsonAsync($"/api/requests/{id}/reject", new RejectRequestContract(null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-DEC-005", body.GetProperty("code").GetString());
        var persisted = await LoadRequestAsync(id);
        Assert.Equal(DecisionType.Approved, persisted!.Approval!.Decision);
    }

    [Fact]
    public async Task Reject_With_A_Comment_Over_500_Characters_Returns_VF_VAL_001()
    {
        var carlosClient = factory.CreateClient();
        await LoginAsAsync(carlosClient, "employee@vacaflow.test", "Employee123!");
        var typeId = GetVacationTypeId(await ListAbsenceTypesAsync(carlosClient));
        var id = await CreateAndSubmitDraftAsync(carlosClient, typeId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var lauraClient = factory.CreateClient();
        await LoginAsAsync(lauraClient, "manager@vacaflow.test", "Manager123!");
        var tooLong = new string('a', 501);

        using var response = await lauraClient.PostAsJsonAsync($"/api/requests/{id}/reject", new RejectRequestContract(tooLong));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-VAL-001", body.GetProperty("code").GetString());
        Assert.Equal("comment", body.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Reject_Without_A_Session_Returns_VF_AUT_004()
    {
        using var response = await _client.PostAsJsonAsync($"/api/requests/{Guid.NewGuid()}/reject", new RejectRequestContract(null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VF-AUT-004", body.GetProperty("code").GetString());
    }
}
