# Just Short It

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/github/actions/workflow/status/sneakpodbob/just-short-it/tests.yml?branch=main&style=for-the-badge&label=tests)](https://github.com/sneakpodbob/just-short-it/actions/workflows/tests.yml)
[![GHCR](https://img.shields.io/badge/ghcr-package-2496ED?style=for-the-badge&logo=github)](https://github.com/sneakpodbob/just-short-it/pkgs/container/just-short-it)

## Table of Contents

- [German](#german)
  - [Änderungen zum Upstream-Projekt](#änderungen-zum-upstream-projekt)
  - [Sass](#sass)
  - [Um Just Short It einfach in einem Container zu starten](#um-just-short-it-einfach-in-einem-container-zu-starten)
  - [In Docker Compose](#in-docker-compose)
  - [SQLite-Persistenz](#sqlite-persistenz)
  - [HTTPS](#https)
  - [Lizenz und Namensnennung](#lizenz-und-namensnennung)
- [English](#english)
  - [Changes compared to upstream](#changes-compared-to-upstream)
  - [Sass](#sass-1)
  - [To simply run Just Short It in a container](#to-simply-run-just-short-it-in-a-container)
  - [In Docker Compose](#in-docker-compose-1)
  - [SQLite persistence](#sqlite-persistence)
  - [HTTPS](#https-1)
  - [License and Attribution](#license-and-attribution)

## German

Ein einfacher Ein-Benutzer-URL-Shortener.
Dieses Projekt ist ursprünglich ein Fork von [Just Short It](https://github.com/miawinter98/just-short-it), hat sich aber mittlerweile einigermaßen davon entfernt.

### Änderungen zum Upstream-Projekt

Das ursprüngliche Projekt nutzte die Visual Studio WebCompiler-Erweiterung, um `wwwroot/css/index.sass` in CSS zu übersetzen, dies wird nun mit Sass-CLI gemacht. Weiterhin habe ich das Projekt auf .NET 10 gehoben und die Redis-Persistenz-Schicht durch SQLite ersetzt.

### Sass

```bash
npm install
# einmalige Builds
npm run build:css
# oder waehrend der Bearbeitung von Styles weiterlaufen lassen
npm run watch:css
```

Der Sass-Einstiegspunkt (`wwwroot/css/index.sass`) verwendet Bulma 1.x und das Dart-Sass-Modulsystem (`@use`). Jeder `dotnet build`/`run`/`publish` ruft jetzt automatisch `npm run build:css` auf, sodass das Stylesheet immer vorhanden ist, bevor Razor Pages ausgeliefert werden.

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

### Sass

```bash
npm install
# one-off builds
npm run build:css
# or keep it running while editing styles
npm run watch:css
```

The Sass entry point (`wwwroot/css/index.sass`) targets Bulma 1.x and the Dart Sass module system (`@use`). Every `dotnet build`/`run`/`publish` now calls `npm run build:css` automatically, so the stylesheet always exists before Razor pages are served.

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

### HTTPS

Just Short It! does not support HTTPS. For hosting, you should therefore use a reverse proxy that terminates SSL.

### License and Attribution

Still under [MIT License](LICENSE)

Based on Just Short It by [Mia Winter](https://miawinter.de/), licensed under the [MIT License](https://en.wikipedia.org/wiki/MIT_License).  
Just Short It uses [Bulma](https://bulma.io/) for styling; Bulma is licensed under the [MIT License](https://github.com/jgthms/bulma/blob/master/LICENSE).
