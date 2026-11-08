# syntax=docker/dockerfile:1

# ---- Stage 1: build di Angular con Node
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

# ---- Stage 2: solo i file statici, serviti da nginx come utente non root (porta 8080)
FROM nginxinc/nginx-unprivileged:1.29-alpine AS final
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist/web/browser /usr/share/nginx/html

# Indirizzo dell'API: nginx sostituisce ${API_URL} all'avvio del container
ENV API_URL=http://api:8080
EXPOSE 8080
