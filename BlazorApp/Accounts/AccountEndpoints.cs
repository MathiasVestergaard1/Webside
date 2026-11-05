using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using MySqlConnector;

namespace BlazorApp.Accounts;

public static class AccountEndpoints
{
    // Opret HTTP POST-ruten til kontoformularerne.
    public static void MapAccountEndpoints(this WebApplication app)
    {
        // HTTP POST: Modtag login, oprettelse eller logout med begrænset antal forsøg.
        app.MapPost("/account/{action}", HandleAsync).RequireRateLimiting("accounts");
    }

    // Kontrollér formularen og udfør login, oprettelse eller logout.
    private static async Task<IResult> HandleAsync(string action, HttpContext context,
        IAntiforgery antiforgery, UserRepository users, IPasswordHasher<UserAccount> hasher,
        IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        if (action is not ("login" or "register" or "logout")) return Results.NotFound();
        // Kontrollér formularens sikkerhedstoken (CSRF-beskyttelse).
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest("Ugyldig formular. Genindlæs siden og prøv igen."); }

        if (action == "logout")
        {
            // Log ud ved at fjerne login-cookien fra browseren.
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.LocalRedirect("/login");
        }

        var page = action == "register" ? "/register" : "/login";
        IResult Error(string code) => Results.LocalRedirect($"{page}?error={code}");
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("UsersDatabase"))) return Error("database");

        // Læs og kontrollér brugernavn og password fra HTTP POST.
        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var username = form["username"].ToString().Trim();
        var password = form["password"].ToString();
        if (username.Length is < 1 or > 50 || password.Length is < 1 or > 128) return Error("invalid");

        try
        {
            if (action == "register")
            {
                var email = form["email"].ToString().Trim();
                if (email.Length > 254 || !new EmailAddressAttribute().IsValid(email)
                    || password.Length < 12 || password != form["confirmPassword"].ToString()) return Error("invalid");
                var account = new UserAccount(0, username, email, "");
                // Lav en hash af passwordet.
                var hash = hasher.HashPassword(account, password);
                // Repository kører SQL INSERT og gemmer brugeren.
                await users.CreateAsync(username, email, hash, context.RequestAborted);
                return Results.LocalRedirect("/login?registered=true");
            }

            // Repository kører SQL SELECT og henter brugeren.
            var user = await users.FindAsync(username, context.RequestAborted);
            // Udfør også hashing ved ukendt bruger for at mindske tidsforskellen.
            if (user is null)
            {
                hasher.HashPassword(new UserAccount(0, "", "", ""), password);
                return Error("credentials");
            }
            // Sammenlign det indtastede password med den gemte hash.
            PasswordVerificationResult verified;
            try { verified = hasher.VerifyHashedPassword(user, user.PasswordHash, password); }
            catch (FormatException) { verified = PasswordVerificationResult.Failed; }
            if (verified == PasswordVerificationResult.Failed) return Error("credentials");

            // Opret brugerens identitet med id og navn.
            var identity = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Name, user.Username)
            }, CookieAuthenticationDefaults.AuthenticationScheme);
            // Opret login-cookien.
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                // Sessioncookie med udløbstid fra Program.cs.
                new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = false });
            // Send browseren til kontosiden, som hentes med HTTP GET.
            return Results.LocalRedirect("/account");
        }
        // MySQL-fejl 1062 betyder, at brugernavn eller e-mail allerede findes.
        catch (MySqlException ex) when (action == "register" && ex.Number == 1062) { return Error("duplicate"); }
        catch (Exception ex) when (ex is MySqlException or InvalidOperationException)
        {
            loggerFactory.CreateLogger("Accounts").LogError("Account database operation failed ({ErrorType}).", ex.GetType().Name);
            return Error("database");
        }
    }
}
