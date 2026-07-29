namespace BigSolutions.VacaFlow.Domain.Employees;

/// <summary>
/// The two application roles (Intent.md §4). Values are explicit because they
/// are persisted as an integer — leaving them implicit would make inserting a
/// role in the middle of the enum a silent data migration.
/// </summary>
public enum EmployeeRole
{
    Employee = 1,
    Manager = 2,
}
