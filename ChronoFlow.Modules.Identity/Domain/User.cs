namespace ChronoFlow.Modules.Identity.Domain;
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }

    public User(Guid id, string email, string passwordHash)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
    }

    public static User Create(string email, string passwordHash)
    {
        if (string .IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password hash is required.");
        
        email = email.Trim().ToLowerInvariant();

        return new User(Guid.NewGuid(), email, passwordHash);
    }
}