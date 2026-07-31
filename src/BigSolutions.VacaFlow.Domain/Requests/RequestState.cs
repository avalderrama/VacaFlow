namespace BigSolutions.VacaFlow.Domain.Requests;

/// <summary>
/// The closed lifecycle of a <see cref="Request"/> (FRD.md §4.1). The
/// enumeration is fixed from day one even though this story only exercises
/// <see cref="Draft"/> — the persistence mapping and future UI badges need it
/// stable (US-015 plan D5).
/// </summary>
public enum RequestState
{
    Draft,
    Submitted,
    Approved,
    Rejected,
    Cancelled,
}
