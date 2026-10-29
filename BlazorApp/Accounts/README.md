# Login med MySQL

Koden bruger den eksisterende tabel `webapp.users` med `id`, `username`,
`email`, `password_hash` og `created_at`. Den opretter eller ændrer ikke tabeller.

- `UserRepository.cs`: SQL med parametre til opslag og oprettelse.
- `AccountEndpoints.cs`: validering, hashing, login-cookie og logout.
- `Components/Pages/Account/`: siderne `/login`, `/register` og `/account`.

## Kør hele projektet med Docker (lokal udvikling)

Installér og start Docker Desktop med Linux-containere. Du behøver ikke
installere MySQL eller .NET på din pc med denne løsning.
Kør fra repositoryets rod (mappen med `compose.yaml`):

```powershell
docker compose up --build -d
```

Åbn http://localhost:8080/register, og opret en testbruger.
Første start downloader images og bygger appen, så den kan tage nogle minutter.
Compose starter MySQL først og venter på, at tabellen er klar, før appen starter.
Databasen er kun tilgængelig internt i Docker, så den konflikter ikke med
en lokal MySQL-installation på port 3306.

Se status og logs:

```powershell
docker compose ps
docker compose logs --tail=100 app db
```

Stop projektet med `docker compose down`. Data bevares i Docker-volumen
`mysql_data` og bruges igen næste gang. `setup.sql` køres automatisk ved
første oprettelse af en tom databasevolumen; senere ændringer i SQL-filen
køres ikke automatisk på en eksisterende database.

Opsætningen bruger Development, lokal HTTP og offentlige testpasswords.
Den er kun til lokal afprøvning. En fælles VM kræver særskilt opsætning
med egne secrets, HTTPS og Production-miljø.

## Opsæt MySQL første gang (uden Docker)

Hver udvikler skal have MySQL Server installeret og kørende lokalt.
Databasen og brugerne følger ikke med, når man henter branchen fra Git.

1. Åbn MySQL Workbench, og forbind til din lokale MySQL-server.
2. Åbn filen [`setup.sql`](setup.sql), og kør hele scriptet.
   Det opretter databasen `webapp` og tabellen `users` med de felter,
   som koden forventer, samt unikke brugernavne og e-mailadresser.
3. Indstil forbindelsen og start appen med kommandoerne nedenfor.
4. Åbn registreringssiden, og opret din egen testbruger.

Scriptet kan køres igen uden at slette eksisterende data. Hvis tabellen
allerede findes, ændrer scriptet ikke dens struktur; kontrollér i så fald,
at den har de samme kolonner og unikke indeks som i `setup.sql`.
Appen kører ikke scriptet automatisk. SQL-filen indeholder ingen passwords
eller testbrugere.

Hvis I bruger en fælles MySQL-server, skal scriptet kun køres én gang på
den server. Brug serverens værtsnavn i stedet for `localhost` i forbindelsen.
I deler så database og testbrugere; med lokal MySQL har alle deres egne data.

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
