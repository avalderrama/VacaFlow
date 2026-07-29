using System.Reflection;

namespace BigSolutions.VacaFlow.Application;

/// <summary>Assembly anchor for the architecture tests. See <c>DomainAssembly</c>.</summary>
public static class ApplicationAssembly
{
    public static readonly Assembly Instance = typeof(ApplicationAssembly).Assembly;
}
