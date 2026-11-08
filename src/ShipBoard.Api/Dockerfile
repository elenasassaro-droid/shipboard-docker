# syntax=docker/dockerfile:1

# ---- Stage 1: build con l'SDK completo (circa 900 MB, non finisce nell'immagine finale)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Prima solo il .csproj: il restore resta in cache finche' non cambiano le dipendenze
COPY ShipBoard.Api.csproj .
RUN dotnet restore

# Poi il resto del codice, che cambia spesso
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# ---- Stage 2: solo il runtime ASP.NET, versione "chiseled" (niente shell, niente package manager)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app .

# Utente non root (UID 1654, gia' definito nelle immagini .NET)
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ShipBoard.Api.dll"]
