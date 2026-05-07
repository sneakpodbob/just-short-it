# Just Short It

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/github/actions/workflow/status/sneakpodbob/just-short-it/tests.yml?branch=main&style=for-the-badge&label=tests)](https://github.com/sneakpodbob/just-short-it/actions/workflows/tests.yml)
[![GHCR](https://img.shields.io/badge/ghcr-package-2496ED?style=for-the-badge&logo=github)](https://github.com/sneakpodbob/just-short-it/pkgs/container/just-short-it)

## Table of Contents

- [German](#german)
  - [Änderungen zum Upstream-Projekt](#änderungen-zum-upstream-projekt)
  - [Architektur](#architektur)
  - [Voraussetzungen](#voraussetzungen)
  - [Schnellstart (Lokale Entwicklung)](#schnellstart-lokale-entwicklung)
  - [Konfigurations-Referenz](#konfigurations-referenz)
  - [Laufzeitmodi (Development vs. Production)](#laufzeitmodi-development-vs-production)
  - [Authentifizierung und Zugriff](#authentifizierung-und-zugriff)
  - [Redirect-Lebenszyklus und Wartung](#redirect-lebenszyklus-und-wartung)
  - [Sass](#sass)
  - [Tests und CI](#tests-und-ci)
  - [Um Just Short It einfach in einem Container zu starten](#um-just-short-it-einfach-in-einem-container-zu-starten)
  - [In Docker Compose](#in-docker-compose)
  - [SQLite-Persistenz](#sqlite-persistenz)
  - [Reverse Proxy und Forwarded Headers](#reverse-proxy-und-forwarded-headers)
  - [Sicherheits-Hinweise](#sicherheits-hinweise)
  - [HTTPS](#https)
  - [Lizenz und Namensnennung](#lizenz-und-namensnennung)
- [English](#english)
  - [Changes compared to upstream](#changes-compared-to-upstream)
  - [Architecture](#architecture)
  - [Prerequisites](#prerequisites)
  - [Quick Start (Local Development)](#quick-start-local-development)
  - [Configuration Reference](#configuration-reference)
  - [Runtime Modes (Development vs Production)](#runtime-modes-development-vs-production)
  - [Authentication and Access](#authentication-and-access)
  - [Redirect Lifecycle and Maintenance](#redirect-lifecycle-and-maintenance)
  - [Sass](#sass-1)
  - [Tests and CI](#tests-and-ci)
  - [To simply run Just Short It in a container](#to-simply-run-just-short-it-in-a-container)
  - [In Docker Compose](#in-docker-compose-1)
  - [SQLite persistence](#sqlite-persistence)
  - [Reverse proxy and forwarded headers](#reverse-proxy-and-forwarded-headers)
  - [Security notes](#security-notes)
  - [HTTPS](#https-1)
  - [License and Attribution](#license-and-attribution)

## German

Ein einfacher Ein-Benutzer-URL-Shortener.
Dieses Projekt ist ursprünglich ein Fork von [Just Short It](https://github.com/miawinter98/just-short-it), hat sich aber mittlerweile einigermaßen davon entfernt.

### Änderungen zum Upstream-Projekt

Das ursprüngliche Projekt nutzte die Visual Studio WebCompiler-Erweiterung, um `wwwroot/css/index.sass` in CSS zu übersetzen, dies wird nun mit Sass-CLI gemacht. Weiterhin habe ich das Projekt auf .NET 10 gehoben und die Redis-Persistenz-Schicht durch SQLite ersetzt.

### Architektur

- ASP.NET Core Razor Pages für UI und Routing.
- SQLite über EF Core (`JustShortItDbContext`) als Persistenz.
- Coravel Scheduler für Aufräum- und Wartungsjobs.
- Serilog (Console Sink) für strukturierte Logs.

### Voraussetzungen

- .NET SDK 10.x
- Node.js 22.x und npm
- Optional: Docker (für Containerbetrieb)

### Schnellstart (Lokale Entwicklung)

```bash
cd just-short-it
npm ci
cd ..
dotnet run --project ./just-short-it/JustShortIt.csproj
```

Danach ist die App standardmäßig unter `http://localhost:5128` erreichbar.

Im Development-Modus werden Test-Zugangsdaten verwendet:

- Benutzername: `test`
- Passwort: `test`

### Konfigurations-Referenz

Alle App-spezifischen Umgebungsvariablen verwenden das Präfix `JSI_`.

| Konfigurationsschlüssel | Umgebungsvariable | Pflicht in Production | Standard | Beschreibung |
| --- | --- | --- | --- | --- |
| `BaseUrl` | `JSI_BaseUrl` | Ja | leer | Externe Basis-URL für generierte Short-Links (Production). |
| `Account:Username` | `JSI_Account__Username` | Ja | leer | Login-Benutzername. |
| `Account:Password` | `JSI_Account__Password` | Ja | leer | Login-Passwort. |
| `Sqlite:Path` | `JSI_Sqlite__Path` | Nein | `data/justshortit.db` | Pfad zur SQLite-Datei. |
| `Sqlite:ExpiredIdReuseBlockSeconds` | `JSI_Sqlite__ExpiredIdReuseBlockSeconds` | Nein | `5184000` | Sperrfrist in Sekunden fuer IDs nach natuerlichem Ablauf eines Redirects (Standard: 60 Tage). |

Zusätzlich wichtig:

- `ASPNETCORE_URLS` steuert den Listen-Port (im Container standardmäßig `http://+:8081`).

### Laufzeitmodi (Development vs. Production)

- Development:
  - `BaseUrl` wird dynamisch aus dem Request abgeleitet.
  - Standard-Testzugang (`test` / `test`) ist aktiv.
  - CORS erlaubt lokale Origins (`http://localhost:5128`, `https://localhost:7128`).
- Production:
  - Gültige absolute `BaseUrl` ist erforderlich, sonst Startfehler.
  - Account-Credentials sind erforderlich, sonst Startfehler.
  - CORS emittiert keine Cross-Origin-Header.

### Authentifizierung und Zugriff

- Login über Cookie-Authentifizierung.
- Authentifizierte Seiten:
  - URL-Verwaltung (`/Urls`)
  - Redirect-Inspektion/Löschen (`/Inspect`)
- Cookie-Verhalten:
  - Lebensdauer: 24 Stunden
  - Sliding Expiration: aktiv
  - In Production nur sichere Cookies (`SecurePolicy=Always`)

### Redirect-Lebenszyklus und Wartung

- Redirects können ablaufen (Ablaufzeitpunkt in UTC gespeichert).
- Abgelaufene Redirects werden stündlich bereinigt.
- Nach natürlichem Ablauf bleibt die ID für die konfigurierte Sperrfrist blockiert.
- Nach Ablauf dieser Sperrfrist kann die ID wiederverwendet werden.
- Manuelles Löschen entfernt Redirect und Sperre sofort, die ID ist dann direkt wieder frei.
- SQLite-Wartung (`VACUUM`) läuft wöchentlich.

### Sass

```bash
npm install
# einmalige Builds
npm run build:css
# oder waehrend der Bearbeitung von Styles weiterlaufen lassen
npm run watch:css
```

Der Sass-Einstiegspunkt (`wwwroot/css/index.sass`) verwendet Bulma 1.x und das Dart-Sass-Modulsystem (`@use`). Jeder `dotnet build`/`run`/`publish` ruft jetzt automatisch `npm run build:css` auf, sodass das Stylesheet immer vorhanden ist, bevor Razor Pages ausgeliefert werden.

### Tests und CI

Lokale Testausführung:

```bash
cd just-short-it
npm ci
cd ..
dotnet test --solution JustShortIt.slnx -c Release
```

Hinweise:

- Das Testprojekt verwendet TUnit auf .NET 10.
- Der Build der Web-App beinhaltet den CSS-Build (`npm run build:css`), daher installiert die CI vor `dotnet build` Node-Abhängigkeiten.
- GitHub Actions Workflow für Tests: `.github/workflows/tests.yml`.

### Um Just Short It einfach in einem Container zu starten

```bash
docker run -p 8081:8081 -e JSI_BaseUrl=<deine-url> \
           -e JSI_Account__Username=<dein-benutzername> \
           -e JSI_Account__Password=<dein-passwort> \
           sneakpodbob/just-short-it:latest
```

Der Container lauscht auf Port `8081`.

### In Docker Compose

```docker-compose
version: '3.4'

services:
  just-short-it:
    container_name: JustShortIt
    image: sneakpodbob/just-short-it:latest
    ports:
      - "8081:8081"
    environment:
      - "JSI_BaseUrl=<deine-url>"
      - "JSI_Account__Username=<dein-benutzername>"
      - "JSI_Account__Password=<dein-passwort>"
```

### SQLite-Persistenz

Just Short It speichert Redirects in einer SQLite-Datenbank.

Standardmässig wird die Datenbank relativ zum Anwendungsverzeichnis unter `data/justshortit.db` erstellt.
Das kann über die Umgebungsvariable `JSI_Sqlite__Path` überschrieben werden.

In Containern wird `/app/data` als Volume bereitgestellt und `JSI_Sqlite__Path` ist standardmässig auf `/app/data/justshortit.db` gesetzt. Wenn Redirects über Container-Neustarts hinweg erhalten bleiben sollen muss dieses Volume auf den Host gemountet werden.

Das einfachste Compose-Setup sieht so aus:

```docker-compose
version: '3.4'

services:
  just-short-it:
    container_name: JustShortIt
    image: sneakpodbob/just-short-it:latest
    ports:
      - "8081:8081"
    environment:
      - "JSI_BaseUrl=<deine-url>"
      - "JSI_Account__Username=<dein-benutzername>"
      - "JSI_Account__Password=<dein-passwort>"
      - "JSI_Sqlite__Path=/app/data/justshortit.db"
    volumes:
      - jsi-data:/app/data

volumes:
  jsi-data:
```

### Reverse Proxy und Forwarded Headers

Die App ist auf Betrieb hinter einem Reverse Proxy ausgelegt:

- verarbeitet `X-Forwarded-For`, `X-Forwarded-Proto` und `X-Forwarded-Host`
- vertraut auf einen Proxy-Hop (`ForwardLimit = 1`)
- TLS-Terminierung soll am Proxy erfolgen (z. B. NGINX, Traefik, Caddy)

### Sicherheits-Hinweise

- CSP mit Request-gebundenem Nonce für Skripte
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: no-referrer`
- `Permissions-Policy` für nicht genutzte Browser-APIs
- `Cross-Origin-Opener-Policy` und `Cross-Origin-Resource-Policy`
- HSTS in Production

### HTTPS

Just Short It! unterstützt kein HTTPS. Im Hosting sollte daher ein Reverse Proxy verwendet werden der SSL übernimmt.

### Lizenz und Namensnennung

Weiterhin unter [MIT License](LICENSE)

Basierend auf Just Short It von [Mia Winter](https://miawinter.de/), lizenziert unter der [MIT License](https://en.wikipedia.org/wiki/MIT_License).  
Just Short It verwendet [Bulma](https://bulma.io/) fuer das Styling; Bulma ist unter der [MIT License](https://github.com/jgthms/bulma/blob/master/LICENSE) lizenziert.

---

## English

An easy single-user URL shortener.
This project originally started as a fork of [Just Short It](https://github.com/miawinter98/just-short-it), but has drifted from it to a fair degree.

### Changes compared to upstream

The original project used the Visual Studio WebCompiler extension to compile `wwwroot/css/index.sass` into CSS, this is now handled via Sass CLI. I also upgraded the project to .NET 10 and replaced the Redis persistence layer with SQLite.

### Architecture

- ASP.NET Core Razor Pages for UI and routing.
- SQLite via EF Core (`JustShortItDbContext`) for persistence.
- Coravel scheduler for cleanup and maintenance jobs.
- Serilog (console sink) for structured logging.

### Prerequisites

- .NET SDK 10.x
- Node.js 22.x and npm
- Optional: Docker (for containerized runs)

### Quick Start (Local Development)

```bash
cd just-short-it
npm ci
cd ..
dotnet run --project ./just-short-it/JustShortIt.csproj
```

After startup, the app is available at `http://localhost:5128` by default.

In Development mode, built-in test credentials are used:

- Username: `test`
- Password: `test`

### Configuration Reference

All app-specific environment variables use the `JSI_` prefix.

| Configuration key | Environment variable | Required in Production | Default | Description |
| --- | --- | --- | --- | --- |
| `BaseUrl` | `JSI_BaseUrl` | Yes | empty | Public base URL used for generated short links (Production). |
| `Account:Username` | `JSI_Account__Username` | Yes | empty | Login username. |
| `Account:Password` | `JSI_Account__Password` | Yes | empty | Login password. |
| `Sqlite:Path` | `JSI_Sqlite__Path` | No | `data/justshortit.db` | Path to the SQLite database file. |
| `Sqlite:ExpiredIdReuseBlockSeconds` | `JSI_Sqlite__ExpiredIdReuseBlockSeconds` | No | `5184000` | Cooldown in seconds before an expired redirect ID becomes reusable again (default: 60 days). |

Also important:

- `ASPNETCORE_URLS` controls the listen address (inside container defaults to `http://+:8081`).

### Runtime Modes (Development vs Production)

- Development:
  - `BaseUrl` is derived dynamically from the incoming request.
  - Built-in test credentials (`test` / `test`) are active.
  - CORS allows local origins (`http://localhost:5128`, `https://localhost:7128`).
- Production:
  - A valid absolute `BaseUrl` is required, otherwise startup fails.
  - Account credentials are required, otherwise startup fails.
  - CORS emits no cross-origin headers.

### Authentication and Access

- Login uses cookie authentication.
- Authenticated pages:
  - URL management (`/Urls`)
  - Redirect inspection/deletion (`/Inspect`)
- Cookie behavior:
  - Lifetime: 24 hours
  - Sliding expiration: enabled
  - Secure cookies in Production (`SecurePolicy=Always`)

### Redirect Lifecycle and Maintenance

- Redirects may expire (expiration is stored in UTC).
- Expired redirects are cleaned hourly.
- After natural expiry, the ID stays blocked for the configured cooldown period.
- Once that cooldown expires, the ID becomes reusable again.
- Manual deletion removes both the redirect and any cooldown block immediately.
- SQLite maintenance (`VACUUM`) runs weekly.

### Sass

```bash
npm install
# one-off builds
npm run build:css
# or keep it running while editing styles
npm run watch:css
```

The Sass entry point (`wwwroot/css/index.sass`) targets Bulma 1.x and the Dart Sass module system (`@use`). Every `dotnet build`/`run`/`publish` now calls `npm run build:css` automatically, so the stylesheet always exists before Razor pages are served.

### Tests and CI

Run tests locally:

```bash
cd just-short-it
npm ci
cd ..
dotnet test --solution JustShortIt.slnx -c Release
```

Notes:

- The test project uses TUnit on .NET 10.
- Building the web project includes CSS compilation (`npm run build:css`), so CI installs Node dependencies before `dotnet build`.
- GitHub Actions test workflow: `.github/workflows/tests.yml`.

### To simply run Just Short It in a container

```bash
docker run -p 8081:8081 -e JSI_BaseUrl=<your-url> \
           -e JSI_Account__Username=<your-username> \
           -e JSI_Account__Password=<your-password> \
           sneakpodbob/just-short-it:latest
```

The container listens on port `8081`.

### In Docker Compose

```docker-compose
version: '3.4'

services:
  just-short-it:
    container_name: JustShortIt
    image: sneakpodbob/just-short-it:latest
    ports:
      - "8081:8081"
    environment:
      - "JSI_BaseUrl=<your-url>"
      - "JSI_Account__Username=<your-username>"
      - "JSI_Account__Password=<your-password>"
```

### SQLite persistence

Just Short It stores redirects in a SQLite database.

By default the database is created at `data/justshortit.db` relative to the application directory.
This can be overridden via the `JSI_Sqlite__Path` environment variable.
The post-expiry ID cooldown is configured via `JSI_Sqlite__ExpiredIdReuseBlockSeconds`.

In containers, `/app/data` is exposed as a volume and `JSI_Sqlite__Path` defaults to `/app/data/justshortit.db`. If redirects should survive container restarts, this volume must be mounted to the host.

The simplest compose setup looks like this:

```docker-compose
version: '3.4'

services:
  just-short-it:
    container_name: JustShortIt
    image: sneakpodbob/just-short-it:latest
    ports:
      - "8081:8081"
    environment:
      - "JSI_BaseUrl=<your-url>"
      - "JSI_Account__Username=<your-username>"
      - "JSI_Account__Password=<your-password>"
      - "JSI_Sqlite__Path=/app/data/justshortit.db"
    volumes:
      - jsi-data:/app/data

volumes:
  jsi-data:
```

### Reverse proxy and forwarded headers

The app is designed to run behind a reverse proxy:

- handles `X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Host`
- trusts one proxy hop (`ForwardLimit = 1`)
- expects TLS termination at the proxy (for example NGINX, Traefik, Caddy)

### Security notes

- CSP with a per-request script nonce
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: no-referrer`
- `Permissions-Policy` for unused browser capabilities
- `Cross-Origin-Opener-Policy` and `Cross-Origin-Resource-Policy`
- HSTS in Production

### HTTPS

Just Short It! does not support HTTPS. For hosting, you should therefore use a reverse proxy that terminates SSL.

### License and Attribution

Still under [MIT License](LICENSE)

Based on Just Short It by [Mia Winter](https://miawinter.de/), licensed under the [MIT License](https://en.wikipedia.org/wiki/MIT_License).  
Just Short It uses [Bulma](https://bulma.io/) for styling; Bulma is licensed under the [MIT License](https://github.com/jgthms/bulma/blob/master/LICENSE).
