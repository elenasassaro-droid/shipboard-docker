# syntax=docker/dockerfile:1

# ---- Stage 1: build Angular ----
FROM node:22-alpine AS build
WORKDIR /app

# Prima solo i manifest: finche' non cambiano le dipendenze, `npm ci` resta in cache.
COPY package.json package-lock.json ./
RUN npm ci

# Poi il codice e la build di produzione.
COPY . ./
RUN npm run build

# ---- Stage 2: nginx non privilegiato ----
FROM nginxinc/nginx-unprivileged:1.29-alpine AS final

# L'entrypoint di nginx sostituisce ${API_URL} all'avvio e scrive la config in conf.d.
COPY nginx.conf.template /etc/nginx/templates/default.conf.template

COPY --from=build /app/dist/web/browser /usr/share/nginx/html

ENV API_URL=http://api:8080

EXPOSE 8080
