namespace BigSolutions.VacaFlow.Domain.Requests;

/// <summary>The two possible outcomes of a manager's decision on a request (FRD.md §3).</summary>
public enum DecisionType
{
    Approved = 1,
    Rejected = 2,
}
