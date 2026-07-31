using BigSolutions.VacaFlow.Application.AbsenceTypes;
using BigSolutions.VacaFlow.Application.UnitTests.AbsenceTypes.Fakes;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;

namespace BigSolutions.VacaFlow.Application.UnitTests.AbsenceTypes;

public sealed class ListAbsenceTypesHandlerTests
{
    [Fact]
    public async Task Handle_Should_Map_Each_Absence_Type_To_Its_Dto()
    {
        var vacation = AbsenceType.Create(
            new AbsenceTypeId(Guid.NewGuid()), AbsenceTypeCode.Vacation, "Vacation").Value;
        var handler = new ListAbsenceTypesHandler(new FakeAbsenceTypeRepository(vacation));

        var result = await handler.Handle(CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(vacation.Id.Value, dto.Id);
        Assert.Equal("VACATION", dto.Code);
        Assert.Equal("Vacation", dto.Name);
    }

    [Fact]
    public async Task Handle_Should_Preserve_The_Repositorys_Order()
    {
        var personalLeave = AbsenceType.Create(
            new AbsenceTypeId(Guid.NewGuid()), AbsenceTypeCode.PersonalLeave, "Personal Leave").Value;
        var sickLeave = AbsenceType.Create(
            new AbsenceTypeId(Guid.NewGuid()), AbsenceTypeCode.SickLeave, "Sick Leave").Value;
        var vacation = AbsenceType.Create(
            new AbsenceTypeId(Guid.NewGuid()), AbsenceTypeCode.Vacation, "Vacation").Value;
        var handler = new ListAbsenceTypesHandler(
            new FakeAbsenceTypeRepository(personalLeave, sickLeave, vacation));

        var result = await handler.Handle(CancellationToken.None);

        Assert.Equal(["Personal Leave", "Sick Leave", "Vacation"], result.Select(dto => dto.Name));
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_Not_Null_When_The_Catalog_Is_Empty()
    {
        var handler = new ListAbsenceTypesHandler(new FakeAbsenceTypeRepository());

        var result = await handler.Handle(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
