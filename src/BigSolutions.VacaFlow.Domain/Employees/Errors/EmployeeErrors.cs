using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Domain.Employees.Errors;

/// <summary>
/// Codes and messages from the FRD §7 catalogue and the Backlog.md §3.5
/// microcopy catalogue, reproduced verbatim (Backlog.md §3.5 is authoritative
/// for copy; FRD.md §7 states the English error catalogue is identical).
/// </summary>
public static class EmployeeErrors
{
    public static readonly Error EmailAlreadyRegistered = new(
        "VF-AUT-001",
        "An account with this email already exists.",
        Field: "email");

    public static readonly Error EmailInvalid = new(
        "VF-VAL-001",
        "Enter a valid email address, for example name@company.com",
        Field: "email");

    /// <remarks>
    /// Deliberately field-less. A wrong password and an unknown email must be
    /// indistinguishable (FR-AUT-006), so this cannot be attached to either
    /// input — attaching it to "email" would confirm the address exists. It
    /// renders in the form-level alert block instead.
    /// </remarks>
    public static readonly Error InvalidCredentials = new(
        "VF-AUT-002",
        "The email or password is incorrect.");

    /// <remarks>
    /// Only ever reachable after the password has already been verified, so it
    /// discloses nothing to someone who does not hold the credentials. See
    /// SignInHandler for why that ordering is deliberate.
    /// </remarks>
    public static readonly Error AccountInactive = new(
        "VF-AUT-003",
        "This account is not active.");

    public static readonly Error FullNameRequired = new(
        "VF-VAL-001",
        "Full name is required (max 120 characters).",
        Field: "fullName");

    public static readonly Error PasswordTooShort = new(
        "VF-VAL-001",
        "The password must be at least 8 characters.",
        Field: "password");

    /// <remarks>
    /// Not in the Backlog.md §3.5 microcopy catalogue — the catalogue has no
    /// string for an over-long password because the length cap was introduced by
    /// the security review of this story. Reusing PasswordTooShort here would
    /// tell the user the opposite of what happened. Needs adding to §3.5.
    /// </remarks>
    public static readonly Error PasswordTooLong = new(
        "VF-VAL-001",
        "The password must be 128 characters or fewer.",
        Field: "password");

    public static readonly Error RoleInvalid = new(
        "VF-VAL-001",
        "Select a valid role.",
        Field: "role");

    /// <remarks>
    /// Raised by the cookie authentication middleware on every session-protected
    /// endpoint (FR-AUT-011), and by <c>GetCurrentUserHandler</c> when a valid
    /// session names an employee that no longer exists — a data lifecycle
    /// mismatch, not a bug (US-010 decision D3). Field-less either way.
    /// </remarks>
    public static readonly Error NotAuthenticated = new(
        "VF-AUT-004",
        "You must be signed in to perform this action.");

    /// <remarks>
    /// Not in the FRD §7 catalogue — no user-facing flow in the MVP lets
    /// anyone assign their own manager; the only caller is the seeder
    /// (TE-003), for whom this is a programming error, not a business case.
    /// </remarks>
    public static readonly Error CannotBeOwnManager = new(
        "VF-INT-002",
        "An employee cannot be their own manager.");
}
