namespace BlazorApp.Accounts;

// Brugerdatabasen. Gør det muligt at teste login uden en rigtig MySQL-database.
public interface IUserRepository
{
    // Find en bruger ud fra brugernavn. Returnér null, hvis brugeren ikke findes.
    Task<UserAccount?> FindAsync(string username, CancellationToken cancellationToken);

    // Gem en ny bruger. Kaster DuplicateUserException, hvis brugernavn eller e-mail allerede findes.
    Task CreateAsync(string username, string email, string hash, CancellationToken cancellationToken);
}

public sealed class DuplicateUserException(Exception? innerException = null)
    : Exception("Brugernavn eller e-mail findes allerede.", innerException);
