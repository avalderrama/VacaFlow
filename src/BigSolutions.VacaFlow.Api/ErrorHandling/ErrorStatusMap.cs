using Microsoft.AspNetCore.Http;

namespace BigSolutions.VacaFlow.Api.ErrorHandling;

/// <summary>
/// Maps an error code to its HTTP status. Most entries come from the FRD §7
/// catalogue and grow one per story; a few (like <c>VF-INT-001</c>) are
/// internal-only codes that a Domain *Errors.cs file declares but that never
/// reach a client in practice — they still need an entry so
/// Every_Domain_Error_Code_Should_Have_A_Status_Mapping stays green.
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
        ["VF-INT-001"] = StatusCodes.Status500InternalServerError,
        ["VF-INT-002"] = StatusCodes.Status500InternalServerError,
        ["VF-INT-003"] = StatusCodes.Status500InternalServerError,
        ["VF-REQ-001"] = StatusCodes.Status400BadRequest,
        ["VF-REQ-002"] = StatusCodes.Status400BadRequest,
        ["VF-REQ-003"] = StatusCodes.Status409Conflict,
        ["VF-REQ-004"] = StatusCodes.Status403Forbidden,
        ["VF-REQ-005"] = StatusCodes.Status409Conflict,
        ["VF-REQ-006"] = StatusCodes.Status404NotFound,
        ["VF-CAT-001"] = StatusCodes.Status400BadRequest,
        ["VF-DEC-001"] = StatusCodes.Status409Conflict,
        ["VF-DEC-002"] = StatusCodes.Status403Forbidden,
        ["VF-DEC-003"] = StatusCodes.Status403Forbidden,
        ["VF-DEC-004"] = StatusCodes.Status403Forbidden,
        ["VF-DEC-005"] = StatusCodes.Status409Conflict,
    };

    public static int StatusFor(string code) =>
        StatusByCode.GetValueOrDefault(code, StatusCodes.Status500InternalServerError);
}
