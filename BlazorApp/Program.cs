
using BlazorApp.Components;
using BlazorApp.Services;
using BlazorApp.Components.API;
using BlazorApp.Accounts;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ===== Blazor Components =====

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ===== Existing Services =====

// Keep existing authentication-related service
builder.Services.AddScoped<AuthService>();

// HTTP Client Factory
builder.Services.AddHttpClient();

// ===== DMI Weather API =====

builder.Services.AddHttpClient<Api>(client =>
{
    client.BaseAddress = new Uri("https://opendataapi.dmi.dk");

    client.DefaultRequestHeaders.Accept.Clear();

    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(
            "application/json"
        )
    );
});

// ===== Cookie Authentication =====

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";

        options.Cookie.Name = "BlazorApp.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Cookie.SecurePolicy =
            builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;

        // Session expires after one hour
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = false;
    });

// ===== Authorization =====

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// ===== User and Favorites Database =====

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<FavoriteRepository>();

builder.Services.AddScoped<
    IPasswordHasher<UserAccount>,
    PasswordHasher<UserAccount>
>();

// ===== Rate Limiting =====

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy(
        "accounts",
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }
        )
    );
});

var app = builder.Build();

// ===== HTTP Request Pipeline =====

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true
    );

    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true
);

app.UseHttpsRedirection();

// Authentication must run before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();
app.UseAntiforgery();

// ===== Account Endpoints =====

app.MapAccountEndpoints();

// ===== Static Files =====

app.UseStaticFiles();
app.MapStaticAssets();

// ===== Blazor Routes =====

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
