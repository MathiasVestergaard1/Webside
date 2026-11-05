# Use case-tests: Login

Formål: En oprettet bruger skal kunne logge ind, se sin konto og logge ud. Forkerte oplysninger må ikke give adgang.

Dette er manuelle testcases baseret på koden. De er ikke kørt endnu. Udfyld resultatkolonnen efter afprøvning.

## Forberedelse

- Start appen med forbindelse til testdatabasen som beskrevet i Accounts/README.md.
- Opret en testbruger via /register, fx brugernavn login_test og password TestLogin123!.
- Brug kun testoplysninger. Start hver test logget ud, medmindre andet er angivet.
- Vent mindst et minut efter mange loginforsøg, da login, oprettelse og logout deler en grænse på 10 forespørgsler pr. minut pr. IP.

## Use case: Log ind

**Aktør:** En oprettet bruger.

**Forudsætning:** Brugeren findes i databasen, og appen kan forbinde til databasen.

**Hovedforløb:**

1. Brugeren åbner /login.
2. Brugeren skriver sit brugernavn og password.
3. Brugeren trykker på Log ind.
4. Systemet kontrollerer oplysningerne og opretter en login-cookie.
5. Brugeren sendes til /account og ser sit brugernavn.

**Alternative forløb:** Forkerte oplysninger viser en fejl. Tomme felter afvises. Ved databasefejl vises en besked, og brugeren bliver ikke logget ind.

## Testcases

| ID | Test | Trin | Forventet resultat | Resultat |
| --- | --- | --- | --- | --- |
| L01 | Korrekt login | Åbn /login. Skriv login_test og TestLogin123!. Tryk Log ind. | /account åbnes og viser: Du er logget ind som login_test. | Ikke kørt |
| L02 | Forkert password | Log ind med login_test og Forkert123!. | Beskeden Forkert brugernavn eller password. vises. Brugeren er ikke logget ind. | Ikke kørt |
| L03 | Ukendt bruger | Log ind med et brugernavn, der ikke findes, og et vilkårligt password. | Samme fejl som L02. Brugeren er ikke logget ind. | Ikke kørt |
| L04 | Tomme felter | Prøv at sende formularen med begge felter tomme. Gentag med ét tomt felt ad gangen. | Browseren kræver, at feltet udfyldes. Ingen login-cookie oprettes. | Ikke kørt |
| L05 | Adgang uden login | Åbn /account uden at være logget ind. | Siden viser et link til login. Intet brugernavn eller andet kontoindhold vises. | Ikke kørt |
| L06 | Login bevares ved genindlæsning | Log ind som i L01. Genindlæs /account, og besøg siden igen i samme browser. | Brugeren er stadig logget ind, indtil login udløber eller brugeren logger ud. | Ikke kørt |
| L07 | Log ud | Log ind. Tryk Log ud. Åbn derefter /account igen, og genindlæs siden. | Brugeren sendes til /login efter logout. /account viser derefter beskeden om at logge ind. | Ikke kørt |
| L08 | Mellemrum omkring brugernavn | Log ind med brugernavnet ' login_test ' og korrekt password. | Login lykkes, fordi mellemrum i starten og slutningen af brugernavnet fjernes. | Ikke kørt |
| L09 | Database utilgængelig | I et lokalt testmiljø: stop testdatabasen, og forsøg login. Start den igen bagefter. | Beskeden Der kunne ikke oprettes forbindelse til brugerdatabasen. Prøv igen senere. vises. Brugeren bliver ikke logget ind. | Ikke kørt |
| L10 | For mange forsøg | I et lokalt testmiljø: send mere end 10 konto-POST-forespørgsler inden for ét minut fra samme IP. Loginforsøgene kan bruge forkert password. | Når grænsen er brugt, returneres HTTP 429. Forsøg kan foretages igen efter begrænsningsvinduet. | Ikke kørt |
| L11 | Login udløber | Log ind. Vent over en time uden at logge ind igen. Genindlæs /account. | Brugeren er ikke længere logget ind og ser linket til login. | Ikke kørt |

L01–L07 er de vigtigste tests til en enkel demonstration. L08–L11 dækker ekstra fejlsituationer og sessionens levetid. L10 kan kontrolleres i browserens netværksfane eller med et testværktøj; hver POST skal have et gyldigt antiforgery-token.

## Forslag til forbedringer

1. **En mere præcis fejl ved tomme eller ugyldige loginfelter.** Den fælles fejltekst nævner i dag regler for oprettelse af en bruger. Login bør fx vise: Udfyld brugernavn og password.
2. **En forståelig besked ved for mange forsøg.** Koden returnerer HTTP 429. Tilføj fx: For mange forsøg. Vent lidt, og prøv igen. Det gør begrænsningen lettere at forstå.
3. **Vis/skjul password.** En lille knap ved passwordfeltet gør det lettere at opdage tastefejl. Password skal være skjult som standard.
4. **Opdatér password-hash ved behov.** Hvis PasswordHasher returnerer SuccessRehashNeeded, bør den nye hash gemmes efter et vellykket login. I dag tillades login, men hashen opdateres ikke.
5. **Serverbeskyttelse ved fremtidige kontofunktioner.** Kontosiden skjuler i dag indhold med AuthorizeView. Hvis der tilføjes private data eller handlinger, skal endpoints også kræve autorisation, fx med RequireAuthorization().

Til denne enkle løsning ville jeg prioritere punkt 1 og 2 først og beholde resten som videreudvikling. Koden har allerede password-hashing, SQL-parametre, CSRF-beskyttelse, login-cookie med udløb og begrænsning af forsøg.
