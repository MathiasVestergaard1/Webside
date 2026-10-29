# Login med MySQL

Koden bruger den eksisterende tabel `webapp.users` med `id`, `username`,
`email`, `password_hash` og `created_at`. Den opretter eller ændrer ikke tabeller.

- `UserRepository.cs`: SQL med parametre til opslag og oprettelse.
- `AccountEndpoints.cs`: validering, hashing, login-cookie og logout.
- `Components/Pages/Account/`: siderne `/login`, `/register` og `/account`.

## Lokal forbindelse

Kør fra projektets rod i din egen terminal (indsæt dit lokale password):

```powershell
dotnet user-secrets set "ConnectionStrings:UsersDatabase" "Server=localhost;Port=3306;Database=webapp;User ID=root;Password=DIT_LOKALE_PASSWORD;" --project BlazorApp
dotnet run --project BlazorApp --launch-profile http
```

User Secrets gemmes uden for repositoryet. Undlad at lægge password i
appsettings.json eller committe det. Til drift bruges en separat MySQL-bruger
med kun nødvendige rettigheder og en secret via `ConnectionStrings__UsersDatabase`.
I Docker refererer `localhost` til containeren, så databaseværten skal tilpasses.

Åbn http://localhost:5118/register og opret en ny testbruger. Log derefter ind,
se `/account`, og log ud. Kontroller også forkert password og dubletoprettelse.
Nye passwords hashes med ASP.NET Cores PasswordHasher (PBKDF2 med tilfældigt salt).
Salt og formatoplysninger er indeholdt i password_hash; der skal ikke tilføjes en saltkolonne.
Eksisterende brugere virker kun, hvis deres hashes er kompatible med PasswordHasher.
Der foretages ingen automatisk konvertering eller nulstilling af eksisterende passwords.

Login bruger en HttpOnly-sessioncookie og udløber senest efter en time.
Fristen forlænges ikke automatisk. Logout sletter browserens login-cookie.
Browsere med sessionsgendannelse kan gendanne sessioncookies efter genstart.
Hvis login senere skal huskes efter lukning, sættes IsPersistent til true i
AccountEndpoints.cs, og levetiden justeres via ExpireTimeSpan i Program.cs.
Eksisterende cookies ændres ikke: log ud og ind igen for at bruge den nye indstilling.
Formularer har CSRF-beskyttelse; kontoendpoints tillader 10 forsøg pr. minut pr. IP
pr. appinstans. Drift kræver HTTPS. Password-reset, mailbekræftelse, MFA og
distribueret begrænsning af loginforsøg er ikke implementeret.

Blazor-kontoformularerne bruger almindelig HTTP POST, så login kan sætte en cookie
før browseren indlæser den nye side:
https://learn.microsoft.com/aspnet/core/blazor/security/server/?view=aspnetcore-10.0
