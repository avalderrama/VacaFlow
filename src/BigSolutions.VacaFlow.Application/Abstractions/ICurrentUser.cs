using BigSolutions.VacaFlow.Domain.Employees;

namespace BigSolutions.VacaFlow.Application.Abstractions;

/// <summary>
/// The identity a use case acts on behalf of, derived from the authenticated
/// context — never from a payload (FR-AUT-010, TE-011). Reading a claim is not
/// I/O, so there is no async member here.
/// </summary>
public interface ICurrentUser
{
    EmployeeId EmployeeId { get; }

    EmployeeRole Role { get; }
}
