using BlazorApp.Accounts;

namespace BlazorApp.Tests.Helpers;

// In-memory replacement for the MySQL user table.
public sealed class FakeUserRepository : IUserRepository
{
    public Dictionary<string, UserAccount> Users { get; } = [];

    // Set to simulate a database outage.
    public Exception? Failure { get; set; }

    public Task<UserAccount?> FindAsync(string username, CancellationToken cancellationToken)
    {
        if (Failure is not null) throw Failure;
        return Task.FromResult(Users.GetValueOrDefault(username));
    }

    public Task CreateAsync(string username, string email, string hash, CancellationToken cancellationToken)
    {
        if (Failure is not null) throw Failure;
        if (Users.ContainsKey(username) || Users.Values.Any(u => u.Email == email)) throw new DuplicateUserException();

        Users[username] = new UserAccount((ulong)Users.Count + 1, username, email, hash);
        return Task.CompletedTask;
    }
}
