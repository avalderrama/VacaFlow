using BigSolutions.VacaFlow.Application.Requests;
using BigSolutions.VacaFlow.Application.UnitTests.AbsenceTypes.Fakes;
using BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;
using BigSolutions.VacaFlow.Application.UnitTests.Requests.Fakes;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;

namespace BigSolutions.VacaFlow.Application.UnitTests.Requests;

public sealed class CreateRequestHandlerTests
{
    private static readonly EmployeeId OwnerId = new(Guid.NewGuid());
    private static readonly Guid GeneratedId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 8, 10);

    private readonly AbsenceType _seededType =
        AbsenceType.Create(new AbsenceTypeId(Guid.NewGuid()), AbsenceTypeCode.Vacation, "Vacation").Value;

    private CreateRequestHandler CreateHandler(
        FakeRequestRepository requests,
        FakeUnitOfWork? unitOfWork = null,
        AbsenceType? seededType = null) =>
        new(
            new FakeCurrentUser(OwnerId),
            new FakeAbsenceTypeRepository(seededType ?? _seededType),
            requests,
            unitOfWork ?? new FakeUnitOfWork(),
            new FakeIdGenerator(GeneratedId),
            new FixedTimeProvider(FixedNow));

    private CreateRequestCommand ValidCommand() => new(
        _seededType.Id.Value, Today, Today.AddDays(2), "Family trip");

    [Fact]
    public async Task Handle_Should_Succeed_With_Valid_Data()
    {
        var requests = new FakeRequestRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(requests, unitOfWork);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(GeneratedId, result.Value);
        var added = Assert.Single(requests.Added);
        Assert.Equal(OwnerId, added.OwnerId);
        Assert.Equal(Domain.Requests.RequestState.Draft, added.State);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Absence_Type_Is_Missing()
    {
        var requests = new FakeRequestRepository();
        var command = ValidCommand() with { AbsenceTypeId = null };
        var handler = CreateHandler(requests);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-VAL-001", result.Error.Code);
        Assert.Equal("absenceTypeId", result.Error.Field);
        Assert.Empty(requests.Added);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Start_Date_Is_Missing()
    {
        var requests = new FakeRequestRepository();
        var command = ValidCommand() with { StartDate = null };
        var handler = CreateHandler(requests);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("startDate", result.Error.Field);
        Assert.Empty(requests.Added);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_End_Date_Is_Missing()
    {
        var requests = new FakeRequestRepository();
        var command = ValidCommand() with { EndDate = null };
        var handler = CreateHandler(requests);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("endDate", result.Error.Field);
        Assert.Empty(requests.Added);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Reason_Is_Missing()
    {
        var requests = new FakeRequestRepository();
        var command = ValidCommand() with { Reason = null };
        var handler = CreateHandler(requests);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("reason", result.Error.Field);
        Assert.Empty(requests.Added);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_End_Date_Is_Before_The_Start_Date()
    {
        var requests = new FakeRequestRepository();
        var command = ValidCommand() with { EndDate = Today.AddDays(-1) };
        var handler = CreateHandler(requests);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-001", result.Error.Code);
        Assert.Empty(requests.Added);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Start_Date_Is_In_The_Past()
    {
        var requests = new FakeRequestRepository();
        var yesterday = Today.AddDays(-1);
        var command = ValidCommand() with { StartDate = yesterday, EndDate = yesterday };
        var handler = CreateHandler(requests);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-002", result.Error.Code);
        Assert.Empty(requests.Added);
    }

    /// <remarks>
    /// Only the "does not exist" half of VF-CAT-001 is reachable here: the
    /// AbsenceType aggregate exposes no Deactivate() (SAD.md §5.1) — IsActive
    /// is always true through the public Create() factory, so a unit test
    /// cannot construct an inactive instance without bypassing the domain's
    /// own invariants. The "inactive" half is covered where it can actually
    /// happen — a row flipped directly in the database — by
    /// RequestRepositoryTests.ExistsActiveAsync_Should_Return_False_For_A_Deactivated_Type.
    /// </remarks>
    [Fact]
    public async Task Handle_Should_Fail_When_The_Absence_Type_Does_Not_Exist()
    {
        var requests = new FakeRequestRepository();
        var otherType = AbsenceType.Create(new AbsenceTypeId(Guid.NewGuid()), AbsenceTypeCode.SickLeave, "Sick Leave").Value;
        var handler = CreateHandler(requests, seededType: otherType);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-CAT-001", result.Error.Code);
        Assert.Empty(requests.Added);
    }

    [Fact]
    public async Task Handle_Should_Succeed_When_The_Start_Date_Equals_Today()
    {
        var requests = new FakeRequestRepository();
        var command = ValidCommand() with { StartDate = Today, EndDate = Today };
        var handler = CreateHandler(requests);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
