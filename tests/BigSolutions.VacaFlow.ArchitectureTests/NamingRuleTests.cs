using BigSolutions.VacaFlow.Domain;
using NetArchTest.Rules;

namespace BigSolutions.VacaFlow.ArchitectureTests;

/// <summary>Naming rules that reveal a responsibility living in the wrong ring.</summary>
public sealed class NamingRuleTests
{
    [Fact] // CA-DOM-011
    public void Domain_Should_Not_Contain_Transport_Types()
    {
        string[] transportSuffixes = ["Dto", "ViewModel", "Response", "Model"];

        var offenders = Types.InAssembly(DomainAssembly.Instance)
            .GetTypes()
            .Where(type => transportSuffixes.Any(
                suffix => type.Name.EndsWith(suffix, StringComparison.Ordinal)))
            .Select(type => type.FullName)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "The domain holds business concepts, not transport objects. Offending types: "
            + string.Join(", ", offenders));
    }

    /// <summary>
    /// CA-DOM-011 also forbids the <c>Request</c> suffix, aimed at HTTP request
    /// objects. In this domain <c>Request</c> is the central business concept —
    /// the absence request itself — so that one type is excluded by exact name.
    /// Recorded as deviation DV-03 in SAD.md §15.
    /// </summary>
    /// <remarks>
    /// The exclusion is by exact name, not by pattern, so a future
    /// <c>CreateRequestRequest</c> in the domain still fails.
    /// </remarks>
    [Fact] // CA-DOM-011, DV-03
    public void Domain_Should_Not_Contain_Request_Suffixed_Types_Other_Than_The_Entity()
    {
        var offenders = Types.InAssembly(DomainAssembly.Instance)
            .That().HaveNameEndingWith("Request")
            .GetTypes()
            .Where(type => type.Name != "Request")
            .Select(type => type.FullName)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "Only the Request aggregate may carry that name in the domain. Offending types: "
            + string.Join(", ", offenders));
    }

    [Fact] // CA-STR-005
    public void Domain_Should_Not_Contain_Types_With_Undefined_Responsibility()
    {
        // `Manager` is deliberately absent from this list: it is a business role
        // in VacaFlow, not a vague suffix. Recorded as deviation DV-05.
        var offenders = Types.InAssembly(DomainAssembly.Instance)
            .GetTypes()
            .Where(type => type.Name.EndsWith("Helper", StringComparison.Ordinal)
                        || type.Name.EndsWith("Util", StringComparison.Ordinal)
                        || type.Name.EndsWith("Utils", StringComparison.Ordinal)
                        || type.Name.EndsWith("Processor", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "Helper, Util and Processor signal a responsibility nobody named. Offending types: "
            + string.Join(", ", offenders));
    }
}
