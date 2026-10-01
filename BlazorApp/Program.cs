using BlazorApp.Components;
<<<<<<< HEAD
using BlazorApp.Services;
=======
using BlazorApp.Components.API;
>>>>>>> origin/MapOfDenmark

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

<<<<<<< HEAD
    

//User Authentication service
builder.Services.AddScoped<AuthService>();
=======
builder.Services.AddHttpClient<Api>(client =>
{
    client.BaseAddress = new Uri("https://opendataapi.dmi.dk");
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
>>>>>>> origin/MapOfDenmark

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
