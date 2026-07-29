using System.Security.Cryptography;
using BigSolutions.VacaFlow.Application.Abstractions;

namespace BigSolutions.VacaFlow.Infrastructure.Security;

/// <summary>
/// PBKDF2-HMAC-SHA256, 210,000 iterations, a random 128-bit salt per password,
/// a 256-bit derived key, constant-time comparison (SAD.md ADR-010,
/// NFR-SEC-001). Parameters are encoded in the stored string so they can be
/// raised later without invalidating existing accounts.
/// </summary>
/// <remarks>
/// Stored format: pbkdf2-sha256$&lt;iterations&gt;$&lt;salt-base64&gt;$&lt;hash-base64&gt;
/// </remarks>
internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Algorithm = "pbkdf2-sha256";
    private const int Iterations = 210_000;
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, KeySizeBytes);

        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('$');
        if (parts.Length != 4 || parts[0] != Algorithm || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expectedKey = Convert.FromBase64String(parts[3]);
        var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithm, expectedKey.Length);

        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
