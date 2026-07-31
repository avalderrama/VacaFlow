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

    /// <remarks>
    /// Unlike the two errors above, this one is part of the FRD §7 catalogue
    /// (FR-CAT-003) — raised by CreateRequestHandler when the referenced type
    /// does not exist or is inactive (US-015 plan D6).
    /// </remarks>
    public static readonly Error NotAvailable = new(
        "VF-CAT-001",
        "The selected absence type does not exist or is not available.",
        Field: "absenceTypeId");
}
