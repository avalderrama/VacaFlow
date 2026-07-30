using System.Security.Claims;
using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;

namespace BigSolutions.VacaFlow.Api.Security;

/// <summary>
/// Reads the identity <c>AuthEndpoints.BuildPrincipal</c> put on the cookie
/// and hands it to Application as plain values — HttpContext, claims and
/// cookies never cross the boundary (CA-PRE-006).
/// </summary>
/// <remarks>
/// Every endpoint that can resolve this is behind the global FallbackPolicy
/// (Program.cs), so a missing or malformed claim here is not a business case
/// to report — it is a programming error (an anonymous endpoint that
/// shouldn't have resolved <see cref="ICurrentUser"/> at all, or a corrupted
/// cookie) and fails loudly instead of pretending an identity exists.
/// </remarks>
internal sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public EmployeeId EmployeeId
    {
        get
        {
            var value = RequireClaim(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var employeeId)
                ? new EmployeeId(employeeId)
                : throw new InvalidOperationException(
                    $"The '{ClaimTypes.NameIdentifier}' claim value '{value}' is not a valid identifier.");
        }
    }

    public EmployeeRole Role
    {
        get
        {
            var value = RequireClaim(ClaimTypes.Role);

            // TryParse alone accepts any integer-shaped string, defined or
            // not (e.g. "7" becomes an EmployeeRole with no name) — IsDefined
            // closes that gap.
            return Enum.TryParse<EmployeeRole>(value, out var role) && Enum.IsDefined(role)
                ? role
                : throw new InvalidOperationException(
                    $"The '{ClaimTypes.Role}' claim value '{value}' is not a recognized role.");
        }
    }

    private string RequireClaim(string claimType)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException(
                "ICurrentUser was resolved outside an HTTP request.");

        var value = httpContext.User.FindFirstValue(claimType);

        return string.IsNullOrEmpty(value)
            ? throw new InvalidOperationException(
                $"The authenticated principal is missing the '{claimType}' claim.")
            : value;
    }
}
