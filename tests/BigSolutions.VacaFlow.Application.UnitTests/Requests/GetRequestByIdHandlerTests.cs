using BigSolutions.VacaFlow.Application.Requests;
using BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;
using BigSolutions.VacaFlow.Application.UnitTests.Requests.Fakes;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Application.UnitTests.Requests;

public sealed class GetRequestByIdHandlerTests
{
    private static readonly EmployeeId OwnerId = new(Guid.NewGuid());
    private static readonly AbsenceTypeId TypeId = new(Guid.NewGuid());
    private static readonly DateOnly Today = new(2026, 8, 10);
    private static readonly DateTime NowUtc = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    private Request NewDraft() => Request.Create(
        new RequestId(Guid.NewGuid()), OwnerId, TypeId,
        DateRange.Create(Today, Today.AddDays(2)).Value, "Family trip",
        Today, NowUtc).Value;

    [Fact]
    public async Task Handle_Should_Return_The_Full_Detail_For_The_Owner()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var handler = new GetRequestByIdHandler(new FakeCurrentUser(OwnerId), requests);

        var result = await handler.Handle(draft.Id.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.Equal(draft.Id.Value, dto.Id);
        Assert.Equal(TypeId.Value, dto.AbsenceTypeId);
        Assert.Equal(Today, dto.StartDate);
        Assert.Equal(Today.AddDays(2), dto.EndDate);
        Assert.Equal("Family trip", dto.Reason);
        Assert.Equal("Draft", dto.State);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Request_Does_Not_Exist()
    {
        var requests = new FakeRequestRepository();
        var handler = new GetRequestByIdHandler(new FakeCurrentUser(OwnerId), requests);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-006", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Caller_Is_Not_The_Owner()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var handler = new GetRequestByIdHandler(new FakeCurrentUser(new EmployeeId(Guid.NewGuid())), requests);

        var result = await handler.Handle(draft.Id.Value, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-004", result.Error.Code);
    }
}
