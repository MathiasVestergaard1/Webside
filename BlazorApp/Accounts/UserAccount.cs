namespace BlazorApp.Accounts;

public sealed record UserAccount(ulong Id, string Username, string Email, string PasswordHash);
