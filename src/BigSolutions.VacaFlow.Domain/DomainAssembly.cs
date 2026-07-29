using System.Reflection;

namespace BigSolutions.VacaFlow.Domain;

/// <summary>
/// Assembly anchor for the architecture tests. Holds no behaviour and is
/// referenced by nothing at runtime — it exists so the test project can load
/// this assembly without hard-coding its name. The pattern is the one used by
/// the normative rules document, §13.1.
/// </summary>
public static class DomainAssembly
{
    public static readonly Assembly Instance = typeof(DomainAssembly).Assembly;
}
