using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees.Errors;
using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests.Errors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence;

/// <summary>
/// Translates the provider exceptions this use case expects into
/// application-level errors before they cross the ring (CA-INF-005).
/// </summary>
/// <remarks>
/// Only two uniqueness violations are translated, and only those: each is
/// a race window its own handler's in-memory check cannot fully close —
/// Employees.Email (plan §3.1, steps 3 and 7) and, since US-021,
/// Approvals.RequestId (two concurrent decisions on the same Submitted
/// request both pass ApprovalPolicy/Request.Decide's in-memory guard
/// before either persists; the second SaveChangesAsync hits the unique
/// index and lands here as VF-DEC-005 — exactly what a second decider
/// should see for "already decided"). Every other constraint violation is
/// a bug, not an expected outcome, so it propagates and becomes a 500 —
/// turning a foreign key or NOT NULL failure into one of these two
/// messages would report the wrong cause and hide the defect.
///
/// The match is on the *extended* error code. SqliteErrorCode is the primary
/// code, and 19 (SQLITE_CONSTRAINT) covers every constraint type — unique,
/// foreign key, NOT NULL and primary key all report 19. Only the extended code
/// distinguishes them (2067 = SQLITE_CONSTRAINT_UNIQUE).
/// </remarks>
internal sealed class UnitOfWork(VacaFlowDbContext dbContext) : IUnitOfWork
{
    private const int SqliteUniqueConstraintViolation = 2067;
    private const string EmailUniqueIndexTarget = "Employees.Email";
    private const string ApprovalRequestUniqueIndexTarget = "Approvals.RequestId";

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException exception) when (IsUniquenessViolation(exception, EmailUniqueIndexTarget))
        {
            return Result.Failure(EmployeeErrors.EmailAlreadyRegistered);
        }
        catch (DbUpdateException exception) when (IsUniquenessViolation(exception, ApprovalRequestUniqueIndexTarget))
        {
            return Result.Failure(RequestErrors.AlreadyDecided);
        }
    }

    private static bool IsUniquenessViolation(DbUpdateException exception, string indexTarget) =>
        exception.InnerException is SqliteException
        {
            SqliteExtendedErrorCode: SqliteUniqueConstraintViolation,
        } sqliteException
        && sqliteException.Message.Contains(indexTarget, StringComparison.Ordinal);
}
