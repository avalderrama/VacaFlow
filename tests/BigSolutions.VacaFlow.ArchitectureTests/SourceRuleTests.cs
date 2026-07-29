using System.Reflection;

namespace BigSolutions.VacaFlow.ArchitectureTests;

/// <summary>
/// Two rules that IL-level inspection cannot see, because they are about which
/// member is called rather than which type is referenced. Both are asserted by
/// reading the source. Comment lines are ignored, so documenting a rule does not
/// break it.
/// </summary>
public sealed class SourceRuleTests
{
    private static readonly string SolutionRoot = ResolveSolutionRoot();

    [Fact] // CA-DOM-009, CA-CRS-002
    public void Domain_And_Application_Should_Not_Read_The_Clock_Directly()
    {
        string[] forbidden =
        [
            "DateTime.Now",
            "DateTime.UtcNow",
            "DateTime.Today",
            "DateTimeOffset.Now",
            "DateTimeOffset.UtcNow",
            "DateOnly.FromDateTime(DateTime",
            "Guid.NewGuid(",
            "new Random(",
        ];

        var offenders = Scan(
            forbidden,
            Path.Combine(SolutionRoot, "src", "BigSolutions.VacaFlow.Domain"),
            Path.Combine(SolutionRoot, "src", "BigSolutions.VacaFlow.Application"));

        Assert.True(
            offenders.Count == 0,
            "Time and identifiers are injected, never read statically — a test must be able to fix the clock. "
            + "Use the injected TimeProvider and pass the date into the domain method.\n"
            + string.Join("\n", offenders));
    }

    [Fact] // CA-CFG-003
    public void No_Layer_Should_Resolve_Services_From_The_Container()
    {
        string[] forbidden =
        [
            "GetService<",
            "GetRequiredService<",
            "ServiceLocator",
        ];

        var offenders = Scan(
            forbidden,
            Path.Combine(SolutionRoot, "src", "BigSolutions.VacaFlow.Domain"),
            Path.Combine(SolutionRoot, "src", "BigSolutions.VacaFlow.Application"),
            Path.Combine(SolutionRoot, "src", "BigSolutions.VacaFlow.Infrastructure"));

        Assert.True(
            offenders.Count == 0,
            "Dependencies arrive through the constructor. Only the composition root touches the container.\n"
            + string.Join("\n", offenders));
    }

    private static List<string> Scan(string[] forbidden, params string[] directories)
    {
        var offenders = new List<string>();

        foreach (var directory in directories.Where(Directory.Exists))
        {
            var files = Directory
                .EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                            && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);

                for (var index = 0; index < lines.Length; index++)
                {
                    var line = lines[index];
                    var trimmed = line.TrimStart();

                    if (trimmed.StartsWith("//", StringComparison.Ordinal)
                        || trimmed.StartsWith('*'))
                    {
                        continue;
                    }

                    foreach (var token in forbidden.Where(token => line.Contains(token, StringComparison.Ordinal)))
                    {
                        offenders.Add(
                            $"  {Path.GetRelativePath(SolutionRoot, file)}:{index + 1} — {token.TrimEnd('(', '<')}");
                    }
                }
            }
        }

        return offenders;
    }

    private static string ResolveSolutionRoot()
    {
        var root = typeof(SourceRuleTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "SolutionRoot")
            ?.Value;

        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            throw new InvalidOperationException(
                "SolutionRoot metadata is missing. It is set in Directory.Build.props.");
        }

        return root;
    }
}
