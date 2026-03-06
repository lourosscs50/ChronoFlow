using System.Security.Cryptography;

namespace ChronoFlow.Modules.Identity.Infrastructure;
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }
    public bool VerifyPassword(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.');
        var iterations = int.Parse(parts[0]);

        var salt = Convert.FromBase64String(parts[1]);
        var key = Convert.FromBase64String(parts[2]);

        var attempt = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            key.Length);

        return CryptographicOperations.FixedTimeEquals(attempt, key);
    }
}