using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Domain.AbsenceTypes.Errors;

/// <summary>
/// Not part of the FRD §7 catalogue: an invalid code or an empty name here is
/// a programming or data error (the value never reaches a user-facing form in
/// the MVP — the catalog is seeded, read-only at runtime), not a business
/// rule violation to report through the API.
/// </summary>
public static class AbsenceTypeErrors
{
    public static readonly Error CodeInvalid = new(
        "VF-INT-001",
        "The absence type code is not one of the known values.");

    public static readonly Error NameRequired = new(
        "VF-INT-003",
        "The absence type name is required.");
}
