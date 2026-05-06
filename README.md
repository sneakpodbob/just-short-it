# Just Short It

**This is my fork of [Just Short It](https://github.com/miawinter98/just-short-it).**

I forked it to play around a litte and read the code, adapt it to my code formatting style and maybe adapt some little things.
You might as well go to the original repository and either use that or fork it yourself.

## Just Short It (damnit)

A KISS single user URL shortener.

## Local development

The original project relied on the Visual Studio WebCompiler extension to turn `wwwroot/css/index.sass` into CSS.
That tooling is Windows-only, so on macOS/Linux (and in VS Code) install the Node dependencies and run the cross-platform Sass CLI instead:

```bash
npm install
# one-off builds
npm run build:css
# or keep it running while editing styles
npm run watch:css
```

The Sass entry point (`wwwroot/css/index.sass`) targets Bulma 1.x and the Dart Sass module system (`@use`). Every `dotnet build`/`run`/`publish` now calls `npm run build:css` automatically, so the stylesheet always exists before Razor pages are served.

## To simply run Just Short It in a container run

```bash
docker run -p 8081:8081 -e JSI_BaseUrl=<your-url> \
           -e JSI_Account__Username=<your-username> \
           -e JSI_Account__Password=<your-password> \
           miawinter/just-short-it:latest
```

The container listens on port `8081`.

## In Docker Compose

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

## SQLite persistence

Just Short It now stores redirects in a SQLite database.

By default the database is created at `data/justshortit.db` relative to the application directory.
You can override this with the environment variable `JSI_Sqlite__Path`.

In containers, `/app/data` is exposed as a volume and `JSI_Sqlite__Path` defaults to `/app/data/justshortit.db`.
If you want to keep redirects across container restarts, mount that volume to the host.

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

There you go, now your urls survive a restart!

If NGINX runs on the same host and forwards to the container port, use
`proxy_pass http://127.0.0.1:8081;`.

## Https

Just Short It! is not supporting Https, you may consider using a reverse Proxy for hosting
that handles SSL.

e.g.:
[nginx-proxy](https://github.com/nginx-proxy/nginx-proxy) togehter with

## License and Attribution

Based on:
Just Short It by [Mia Winter](https://miawinter.de/), licensed under the [MIT License](https://en.wikipedia.org/wiki/MIT_License).  
Just Short It uses [Bulma](https://bulma.io/) for styling, Bulma is licensed under the [MIT License](https://github.com/jgthms/bulma/blob/master/LICENSE).

Original Copyright (c) 2023 Mia Winter
