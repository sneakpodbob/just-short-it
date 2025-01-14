This is my fork of [Just Short It](https://github.com/miawinter98/just-short-it).

I forked it to play around a litte and read the code, adapt it to my code formatting style and maybe adapt some little things.
You might as well go to the original repository and either use that or fork it yourself.

# Just Short It (damnit)!

A KISS single user URL shortener.

## To simply run Just Short It in a container run:
```
docker run -e JSI_BaseUrl=<your-url> \
           -e JSI_Account__Username=<your-username> \
           -e JSI_Account__Password=<your-password> \
           miawinter/just-short-it:latest
```

## In Docker Compose:
```docker-compose
version: '3.4'

services:
  just-short-it:
    container_name: JustShortIt
    image: miawinter/just-short-it:latest
    environment:
      - "JSI_BaseUrl=<your-url>"
      - "JSI_Account__Username=<your-username>"
      - "JSI_Account__Password=<your-password>"
```

## Redis

By default Just Short It saves all the redirects in a in-memory distributed Cache, which get's lost 
whenever the container restarts, so if you want to keep your redirects you wanna use redis.

You can configure the connection to redis using the environment variables `JSI_Redis__ConnectionString` 
and optional `JSI_Redis__InstanceName` (default is "JustShortIt").

If you want to run both with compose, the most simple setup looks like this:

```docker-compose
version: '3.4'

services:
  just-short-it:
    container_name: JustShortIt
    image: miawinter/just-short-it:latest
    environment:
      - "JSI_BaseUrl=<your-url>"
      - "JSI_Account__Username=<your-username>"
      - "JSI_Account__Password=<your-password>"
      - "JSI_Redis__ConnectionString=redis,password=<your-redis-password>"
    depends_on:
      - redis
  redis:
    container_name: Redis
    image: redis:alpine
    environment:
      - "REDIS_PASSWORD=<your-redis-password>"
    volumes:
      - redis:/data

volumes:
  redis:
```

There you go, now your urls survive a restart!

# Https

Just Short It! is not supporting Https, you may consider using a reverse Proxy for hosting
that handles SSL.

e.g.:
[nginx-proxy](https://github.com/nginx-proxy/nginx-proxy) togehter with 
[acme-companion](https://github.com/nginx-proxy/acme-companion), 

Here is an Example of how to use Just Short It! togehter with nginx-proxy:

```docker-compose
version: '3.4'

services:
  # Just Short It
  just-short-it:
    container_name: JustShortIt
    image: miawinter/just-short-it:latest
    environment:
      - "JSI_BaseUrl=<your-url>"
      - "JSI_Account__Username=<your-username>"
      - "JSI_Account__Password=<your-password>"
      - "JSI_Redis__ConnectionString=redis,password=<your-redis-password>"
    environment:
      - "VIRTUAL_HOST=<your-url>"
      - "LETSENCRYPT_HOST=<your-url>"
    depends_on:
      - redis
      - acme-companion
  redis:
    container_name: Redis
    image: redis:alpine
    environment:
      - "REDIS_PASSWORD=<your-redis-password>"
    volumes:
      - redis:/data

  # nginx-proxy with acme-companion
  nginx-proxy:
    container_name: nginx-proxy
    restart: unless-stopped
    image: jwilder/nginx-proxy:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/tmp/docker.sock:ro
      - certs:/etc/nginx/certs:ro
      - conf:/etc/nginx/conf.d
      - vhost:/etc/nginx/vhost.d
      - html:/usr/share/nginx/html
    environment:
      - "DHPARAM_GENERATION=false" # Not sure you need this actually
      - "DISABLE_ACCESS_LOGS" # Always nice to comply with GDPR
  acme-companion:
    container_name: acme-companion
    restart: unless-stopped
    image: nginxproxy/acme-companion
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - certs:/etc/nginx/certs:rw
      - conf:/etc/nginx/conf.d
      - vhost:/etc/nginx/vhost.d
      - html:/usr/share/nginx/html
      - /etc/acme.sh:/etc/acme.sh
    environment:
      - "DEFAULT_EMAIL=<your-email>"
      - "NGINX_PROXY_CONTAINER=nginx-proxy"
    depends_on:
      - nginx

volumes:
  # Just Short It!
  redis:
  # Proxy
  certs:
  conf:
  vhost:
  html:

```

# License and Attribution

Based on:
Just Short It by [Mia Winter](https://miawinter.de/), licensed under the [MIT License](https://en.wikipedia.org/wiki/MIT_License).  
Just Short It uses [Bulma](https://bulma.io/) for styling, Bulma is licensed under the [MIT License](https://github.com/jgthms/bulma/blob/master/LICENSE).

Original Copyright (c) 2023 Mia Winter