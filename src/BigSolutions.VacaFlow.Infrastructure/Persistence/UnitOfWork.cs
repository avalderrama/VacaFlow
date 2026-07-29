using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees.Errors;
using BigSolutions.VacaFlow.Domain.Primitives;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence;

/// <summary>
/// Translates the one provider exception this use case expects into an
/// application-level error before it crosses the ring (CA-INF-005).
/// </summary>
/// <remarks>
/// Only a uniqueness violation on Employees.Email is translated, and only that
/// one: it is the race window the handler's own check cannot fully close
/// (plan §3.1, steps 3 and 7). Every other constraint violation is a bug, not
/// an expected outcome, so it propagates and becomes a 500 — turning a foreign
/// key or NOT NULL failure into "this email already exists" would report the
/// wrong cause and hide the defect.
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

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException exception) when (IsEmailUniquenessViolation(exception))
        {
            return Result.Failure(EmployeeErrors.EmailAlreadyRegistered);
        }
    }

    private static bool IsEmailUniquenessViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException
        {
            SqliteExtendedErrorCode: SqliteUniqueConstraintViolation,
        } sqliteException
        && sqliteException.Message.Contains(EmailUniqueIndexTarget, StringComparison.Ordinal);
}
