namespace BigSolutions.VacaFlow.Application.Abstractions;

/// <summary>Hashing is a technical concern (SAD.md §6.3), implemented in Infrastructure.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
