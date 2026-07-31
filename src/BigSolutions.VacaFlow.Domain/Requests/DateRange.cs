using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests.Errors;

namespace BigSolutions.VacaFlow.Domain.Requests;

/// <summary>
/// The inclusive span of calendar dates a request covers (SAD.md §5.2).
/// RULE-01 (the end date cannot precede the start date) is unbreakable by
/// construction — a single-day absence where both dates are equal is valid
/// (FR-REQ-002). RULE-02 (start date not in the past) is deliberately not
/// enforced here: it depends on the clock, and a value object never reads it
/// (TE-004).
/// </summary>
public sealed class DateRange : ValueObject
{
    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public DateOnly Start { get; }

    public DateOnly End { get; }

    public static Result<DateRange> Create(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            return Result.Failure<DateRange>(RequestErrors.EndDateBeforeStartDate);
        }

        return Result.Success(new DateRange(start, end));
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Start;
        yield return End;
    }
}
