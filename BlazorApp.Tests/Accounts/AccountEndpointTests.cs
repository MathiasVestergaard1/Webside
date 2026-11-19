using System.Net;
using System.Text.RegularExpressions;
using BlazorApp.Accounts;
using BlazorApp.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlazorApp.Tests.Accounts;

// Runs the real app in memory and posts the login/register/logout forms, with the database replaced by a fake.
// A new app per test keeps the rate limiter (10 requests per minute) from leaking between tests.
public sealed partial class AccountEndpointTests : IDisposable
{
    private const string ValidPassword = "correct horse battery";

    private readonly FakeUserRepository _users = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AccountEndpointTests() : this("Server=fake") { }

    private AccountEndpointTests(string connectionString)
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:UsersDatabase"] = connectionString }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserRepository>();
                services.AddSingleton<IUserRepository>(_users);
            });
        });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [GeneratedRegex("""<input[^>]*name="__RequestVerificationToken"[^>]*>""")]
    private static partial Regex TokenInput();

    [GeneratedRegex("value=\"([^\"]+)\"")]
    private static partial Regex ValueAttribute();

    // Loads the login page like a browser would, to get the antiforgery cookie and form token.
    private async Task<string> GetAntiforgeryTokenAsync()
    {
        var html = await _client.GetStringAsync("/login");
        var input = TokenInput().Match(html).Value;
        return WebUtility.HtmlDecode(ValueAttribute().Match(input).Groups[1].Value);
    }

    private async Task<HttpResponseMessage> PostAsync(string action, Dictionary<string, string> form, bool withToken = true)
    {
        if (withToken)
        {
            form["__RequestVerificationToken"] = await GetAntiforgeryTokenAsync();
        }
        return await _client.PostAsync($"/account/{action}", new FormUrlEncodedContent(form));
    }

    private Task<HttpResponseMessage> RegisterAsync(string username = "anna", string email = "anna@example.com",
        string password = ValidPassword, string? confirmPassword = null) =>
        PostAsync("register", new()
        {
            ["username"] = username,
            ["email"] = email,
            ["password"] = password,
            ["confirmPassword"] = confirmPassword ?? password,
        });

    private Task<HttpResponseMessage> LoginAsync(string username, string password) =>
        PostAsync("login", new() { ["username"] = username, ["password"] = password });

    private void SeedUser(string username, string password)
    {
        var hash = new PasswordHasher<UserAccount>().HashPassword(new UserAccount(0, username, "", ""), password);
        _users.Users[username] = new UserAccount(1, username, $"{username}@example.com", hash);
    }

    private static string? AuthCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.FirstOrDefault(c => c.StartsWith("BlazorApp.Auth="))
            : null;

    private static void AssertRedirect(HttpResponseMessage response, string location)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(location, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task UnknownAction_ReturnsNotFound()
    {
        var response = await PostAsync("delete-everything", []);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MissingAntiforgeryToken_ReturnsBadRequest()
    {
        SeedUser("anna", ValidPassword);

        var response = await PostAsync("login", new() { ["username"] = "anna", ["password"] = ValidPassword }, withToken: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(AuthCookie(response));
    }

    [Fact]
    public async Task Register_CreatesUser_WithHashedPassword()
    {
        var response = await RegisterAsync();

        AssertRedirect(response, "/login?registered=true");
        var user = _users.Users["anna"];
        Assert.Equal("anna@example.com", user.Email);
        Assert.NotEqual(ValidPassword, user.PasswordHash);
        Assert.NotEqual(PasswordVerificationResult.Failed,
            new PasswordHasher<UserAccount>().VerifyHashedPassword(user, user.PasswordHash, ValidPassword));
    }

    [Theory]
    [InlineData("anna", "anna@example.com", "short", "short")]                         // under 12 characters
    [InlineData("anna", "anna@example.com", ValidPassword, "something else entirely")] // passwords don't match
    [InlineData("anna", "not-an-email", ValidPassword, ValidPassword)]
    [InlineData("   ", "anna@example.com", ValidPassword, ValidPassword)]              // blank username
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "anna@example.com", ValidPassword, ValidPassword)] // 51 characters
    public async Task Register_RejectsInvalidInput(string username, string email, string password, string confirmPassword)
    {
        var response = await RegisterAsync(username, email, password, confirmPassword);

        AssertRedirect(response, "/register?error=invalid");
        Assert.Empty(_users.Users);
    }

    [Fact]
    public async Task Register_ExistingUser_ReturnsDuplicateError()
    {
        SeedUser("anna", ValidPassword);

        var response = await RegisterAsync();

        AssertRedirect(response, "/register?error=duplicate");
    }

    [Fact]
    public async Task Login_WithCorrectPassword_SetsSecureSessionCookie()
    {
        SeedUser("anna", ValidPassword);

        var response = await LoginAsync("anna", ValidPassword);

        AssertRedirect(response, "/account");
        var cookie = AuthCookie(response);
        Assert.NotNull(cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        // A session cookie: it should disappear when the browser closes.
        Assert.DoesNotContain("expires=", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("anna", "wrong password!!")]
    [InlineData("nobody", ValidPassword)]
    public async Task Login_WrongPasswordOrUnknownUser_GivesSameError(string username, string password)
    {
        SeedUser("anna", ValidPassword);

        var response = await LoginAsync(username, password);

        // Same message either way, so attackers can't tell which usernames exist.
        AssertRedirect(response, "/login?error=credentials");
        Assert.Null(AuthCookie(response));
    }

    [Fact]
    public async Task Login_DatabaseFailure_ShowsDatabaseError()
    {
        _users.Failure = new InvalidOperationException("Database is down");

        var response = await LoginAsync("anna", ValidPassword);

        AssertRedirect(response, "/login?error=database");
    }

    [Fact]
    public async Task Login_WithoutConnectionString_ShowsDatabaseError()
    {
        using var test = new AccountEndpointTests(connectionString: "");
        test.SeedUser("anna", ValidPassword);

        var response = await test.LoginAsync("anna", ValidPassword);

        AssertRedirect(response, "/login?error=database");
    }

    [Fact]
    public async Task Logout_RemovesAuthCookie()
    {
        SeedUser("anna", ValidPassword);
        await LoginAsync("anna", ValidPassword);

        // The antiforgery token is tied to the logged-in user, so a fresh one is fetched inside PostAsync.
        var response = await PostAsync("logout", []);

        AssertRedirect(response, "/login");
        var cookie = AuthCookie(response);
        Assert.NotNull(cookie);
        Assert.Contains("expires=Thu, 01 Jan 1970", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TooManyAttempts_AreRateLimited_WithMessage()
    {
        SeedUser("anna", ValidPassword);
        for (var i = 0; i < 10; i++)
        {
            AssertRedirect(await LoginAsync("anna", "wrong password!!"), "/login?error=credentials");
        }

        // Even the correct password is blocked until the one-minute window has passed.
        var blocked = await LoginAsync("anna", ValidPassword);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal("For mange forsøg. Vent lidt, og prøv igen.", await blocked.Content.ReadAsStringAsync());
        Assert.Null(AuthCookie(blocked));
    }

    [Fact]
    public async Task RateLimit_AlsoCountsRequestsWithoutToken()
    {
        for (var i = 0; i < 10; i++)
        {
            await PostAsync("login", [], withToken: false);
        }

        var blocked = await PostAsync("login", [], withToken: false);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }
}
