namespace BigSolutions.VacaFlow.Domain.Primitives;

/// <summary>
/// An expected business failure. <paramref name="Code"/> is the identifier from
/// the FRD §7 catalogue, so mapping to HTTP at the edge is a lookup rather than
/// a translation. No HTTP status ever appears inside the domain (CA-DOM-010).
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
