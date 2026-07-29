using System.Reflection;

namespace BigSolutions.VacaFlow.Infrastructure;

/// <summary>Assembly anchor for the architecture tests. See <c>DomainAssembly</c>.</summary>
public static class InfrastructureAssembly
{
    public static readonly Assembly Instance = typeof(InfrastructureAssembly).Assembly;
}
