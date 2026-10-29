
using BlazorApp.Components;
using BlazorApp.Services;
using BlazorApp.Components.API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// User authentication service
builder.Services.AddScoped<AuthService>();

// HTTP client factory for weather search
builder.Services.AddHttpClient();

// DMI Weather API service
builder.Services.AddHttpClient<Api>(client =>
{
    client.BaseAddress = new Uri("https://opendataapi.dmi.dk");
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true
);

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
