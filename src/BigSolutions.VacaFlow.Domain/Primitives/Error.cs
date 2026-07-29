namespace BigSolutions.VacaFlow.Domain.Primitives;

/// <summary>
/// An expected business failure. <paramref name="Code"/> is the identifier from
/// the FRD §7 catalogue, so mapping to HTTP at the edge is a lookup rather than
/// a translation. No HTTP status ever appears inside the domain (CA-DOM-010).
/// </summary>
/// <param name="Field">
/// The input field this error is attached to, when the error came from
/// validating a specific field (e.g. "email"). Null for errors that are not
/// field-scoped. Mirrors the { code, message, field? } shape of FR-ERR-002.
/// </param>
public sealed record Error(string Code, string Message, string? Field = null)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
