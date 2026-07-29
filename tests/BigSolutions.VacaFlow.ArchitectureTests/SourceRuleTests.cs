using System.Reflection;
using System.Text.RegularExpressions;

namespace BigSolutions.VacaFlow.ArchitectureTests;

/// <summary>
/// Rules that IL-level inspection cannot see, because they are about which
/// member is called, which literal is written, or which string is present —
/// rather than which type is referenced. All are asserted by reading the
/// source. <see cref="Scan"/>'s callers ignore comment lines; the two
/// endpoint/error-catalogue checks below read the whole file, comments
/// included.
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

    [Fact] // FR-AUT-011 — the FallbackPolicy denies by default, but a mapped
           // endpoint that states neither opt-out nor opt-in reads as an
           // oversight, not a decision.
    public void Every_Endpoint_Should_State_Its_Authorization_Explicitly()
    {
        // The whole Api project, not just Endpoints/: a minimal API can be
        // mapped straight from the composition root (Program.cs does, for
        // /health), and a rule named "every endpoint" that only reads one
        // folder is a blind spot wearing a guardrail's name.
        var apiDirectory = Path.Combine(SolutionRoot, "src", "BigSolutions.VacaFlow.Api");
        Assert.True(Directory.Exists(apiDirectory), $"Expected project directory not found: {apiDirectory}");

        var mapCall = new Regex(@"\.Map(Get|Post|Put|Delete|Patch)\(""(?<route>[^""]*)""");
        var offenders = new List<string>();
        var totalEndpoints = 0;
        var files = Directory
            .EnumerateFiles(apiDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var matches = mapCall.Matches(content);
            totalEndpoints += matches.Count;

            for (var index = 0; index < matches.Count; index++)
            {
                var start = matches[index].Index;
                var end = index + 1 < matches.Count ? matches[index + 1].Index : content.Length;
                var block = content[start..end];

                if (!block.Contains(".AllowAnonymous()", StringComparison.Ordinal)
                    && !block.Contains(".RequireAuthorization()", StringComparison.Ordinal))
                {
                    var line = content[..start].Count(c => c == '\n') + 1;
                    offenders.Add(
                        $"  {Path.GetRelativePath(SolutionRoot, file)}:{line} — \"{matches[index].Groups["route"].Value}\"");
                }
            }
        }

        Assert.True(
            totalEndpoints > 0,
            "No Map{Verb}(\"...\") call was found under the Api project — the scan pattern or the "
            + "project layout moved, and this test would otherwise pass with nothing to check.");

        Assert.True(
            offenders.Count == 0,
            "Every mapped endpoint must call .AllowAnonymous() or .RequireAuthorization() so its "
            + "authorization contract is visible at the call site, not just implied by the global "
            + "FallbackPolicy.\n" + string.Join("\n", offenders));
    }

    [Fact] // Keeps ErrorStatusMap from drifting silently behind the FRD §7 catalogue
    public void Every_Domain_Error_Code_Should_Have_A_Status_Mapping()
    {
        // Every *Errors.cs under Domain, not just Employees': RequestErrors,
        // AbsenceTypeErrors and the rest arrive with their own stories, and a
        // guardrail scoped to today's one catalogue would not see them.
        var domainDirectory = Path.Combine(SolutionRoot, "src", "BigSolutions.VacaFlow.Domain");
        var statusMapFile = Path.Combine(
            SolutionRoot, "src", "BigSolutions.VacaFlow.Api",
            "ErrorHandling", "ErrorStatusMap.cs");

        Assert.True(Directory.Exists(domainDirectory), $"Expected project directory not found: {domainDirectory}");
        Assert.True(File.Exists(statusMapFile), $"Expected file not found: {statusMapFile}");

        var errorFiles = Directory.EnumerateFiles(domainDirectory, "*Errors.cs", SearchOption.AllDirectories);
        var codes = errorFiles
            .SelectMany(file => Regex.Matches(File.ReadAllText(file), @"""(VF-[A-Z]+-\d+)""")
                .Select(match => match.Groups[1].Value))
            .Distinct()
            .ToList();
        var statusMapContent = File.ReadAllText(statusMapFile);

        Assert.True(codes.Count > 0, "No VF-*-NNN error code was found under Domain's *Errors.cs files.");

        var offenders = codes
            .Where(code => !statusMapContent.Contains($"\"{code}\"", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "Every error code declared in a Domain *Errors.cs file must have an entry in ErrorStatusMap, "
            + "or it silently falls back to 500.\n" + string.Join("\n", offenders));
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
