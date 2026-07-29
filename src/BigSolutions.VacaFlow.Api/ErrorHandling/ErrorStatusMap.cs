using Microsoft.AspNetCore.Http;

namespace BigSolutions.VacaFlow.Api.ErrorHandling;

/// <summary>
/// Maps an error code from the FRD §7 catalogue to its HTTP status. Grows one
/// entry per story; today it covers only the codes US-007 can produce.
/// </summary>
internal static class ErrorStatusMap
{
    private static readonly Dictionary<string, int> StatusByCode = new()
    {
        ["VF-VAL-001"] = StatusCodes.Status400BadRequest,
        ["VF-AUT-001"] = StatusCodes.Status409Conflict,
        ["VF-AUT-002"] = StatusCodes.Status401Unauthorized,
        ["VF-AUT-003"] = StatusCodes.Status403Forbidden,
        ["VF-AUT-004"] = StatusCodes.Status401Unauthorized,
    };

    public static int StatusFor(string code) =>
        StatusByCode.GetValueOrDefault(code, StatusCodes.Status500InternalServerError);
}
