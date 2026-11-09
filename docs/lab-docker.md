# Lab: Docker, dalle basi al deploy su Azure

Oggi ShipBoard passa da "gira sul mio PC" a "gira ovunque": container, dipendenze in Docker, test con database veri, Aspire e infine Azure.

**Regole**
- Lavorate a coppie, una issue per esercizio sul vostro board, una PR per esercizio. La CI deve restare verde.
- Se restate indietro, ogni esercizio ha un branch con la soluzione (`step-01` ... `step-05`). Per riallinearvi:
  ```bash
  git remote add prof https://github.com/matteobortolazzo/shipboard-docker   # una volta sola
  git fetch prof
  git checkout prof/step-02 -- .     # copia i file della soluzione nella vostra branch
  ```
- LLM: fatevi spiegare gli errori, non fatevi scrivere il Dockerfile.

## 0. Prima di iniziare (10 min)

```bash
docker version          # client e server devono rispondere entrambi
docker login            # IMPORTANTE: senza login Docker Hub limita i download a pochi all'ora per tutta l'aula
dotnet --version        # 10.x
dotnet test             # la CI di partenza deve passare anche in locale
```

## 1. Dockerfile per l'API (step-01)

Obiettivo: un'immagine piccola, veloce da ricostruire, che non gira come root.

1. Costruite prima l'esempio sbagliato e guardatelo:
   ```bash
   docker build -f docs/esempi/Dockerfile.sbagliato -t shipboard-api:sbagliato src/ShipBoard.Api
   docker images shipboard-api
   docker history shipboard-api:sbagliato
   ```
2. Scrivete `src/ShipBoard.Api/Dockerfile` **multi-stage**:
   - stage `build` da `mcr.microsoft.com/dotnet/sdk:10.0`
   - copiate **prima** il `.csproj` e fate `dotnet restore`, **poi** il resto del codice
   - `dotnet publish -c Release -o /app`
   - stage finale da `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`, solo il risultato del publish
   - `USER $APP_UID`, porta 8080
3. Aggiungete `.dockerignore` (`bin/`, `obj/`).
4. Verifica:
   ```bash
   docker build -t shipboard-api src/ShipBoard.Api
   docker run --rm -p 5080:8080 shipboard-api
   curl localhost:5080/info          # "host" e' l'id del container, "user" e' app
   docker inspect shipboard-api --format '{{.Config.User}}'
   ```
5. **Sfida cache**: cambiate una riga in `Program.cs` e ricostruite. Il passo `dotnet restore` deve dire `CACHED`. Se non lo dice, l'ordine delle istruzioni e' sbagliato.
6. **Sfida image diet**: chi ha l'immagine piu' piccola che risponde ancora a `/health`? Riferimento: SDK 1,3 GB, soluzione circa 190 MB.

Domande per il debrief: perche' `docker exec -it <id> sh` non funziona sull'immagine chiseled? E' un problema o un vantaggio?

## 2. Frontend e docker compose (step-02)

Obiettivo: `docker compose up --build` avvia PostgreSQL, API e frontend.

1. `web/Dockerfile` multi-stage: build con `node:22-alpine` (`npm ci` dopo aver copiato solo `package*.json`), poi `nginxinc/nginx-unprivileged:1.29-alpine` con i file di `dist/web/browser`.
2. nginx deve inoltrare `/api/` all'API. Usate un template in `/etc/nginx/templates/` con `${API_URL}`: l'immagine nginx lo sostituisce all'avvio.
3. `compose.yaml` con tre servizi:
   - `db`: `postgres:17-alpine`, volume per i dati, healthcheck con `pg_isready`
   - `api`: build da `src/ShipBoard.Api`, variabile `ConnectionStrings__shipboard` che punta all'host `db`
   - `web`: build da `web`, `API_URL=http://api:8080`, porta 8080
4. Verifica: aprite http://localhost:8080, create un ticket, poi `docker compose restart api` e ricaricate: il ticket c'e' ancora. Poi `docker compose down` e `up`: c'e' ancora? E dopo `docker compose down -v`?

Domande: perche' nella connection string l'host e' `db` e non `localhost`? Cosa succede se togliete `condition: service_healthy`?

## 3. Testcontainers (step-03)

Obiettivo: test di integrazione contro un PostgreSQL vero, creato e distrutto dal test.

1. Nuovo progetto `tests/ShipBoard.Api.IntegrationTests` (xUnit), aggiungetelo alla solution.
2. Pacchetti: `Testcontainers.PostgreSql`, `Microsoft.AspNetCore.Mvc.Testing`.
3. Una fixture che avvia `PostgreSqlBuilder("postgres:17-alpine")` e una `WebApplicationFactory<Program>` che imposta `ConnectionStrings:shipboard` con `GetConnectionString()` del container.
4. Almeno due test: `/info` dice `postgresql`, e un ticket creato da un'istanza dell'API si legge da un'altra istanza.
5. `dotnet test` in locale, poi PR: la CI su GitHub li esegue senza configurare nulla, perche' i runner Ubuntu hanno gia' Docker.

Domanda: cosa trovate in `docker ps` mentre i test girano?

## 4. .NET Aspire (step-04)

Obiettivo: un solo comando per avviare tutto, con una dashboard di log, tracce e metriche.

1. Progetto `src/ShipBoard.ServiceDefaults` con `AddServiceDefaults()` (OpenTelemetry, service discovery, resilienza), referenziato dall'API.
2. Progetto `src/ShipBoard.AppHost` (`Aspire.AppHost.Sdk`) con:
   - `AddPostgres("postgres").WithDataVolume().WithPgAdmin()` e `AddDatabase("shipboard")`
   - `AddProject<Projects.ShipBoard_Api>("api").WithReference(db).WaitFor(db)`
   - `AddDockerfile("web", "../../web")` con `API_URL` uguale all'endpoint dell'API
3. `dotnet run --project src/ShipBoard.AppHost`, aprite la dashboard.
4. Create un ticket dal frontend e cercate la traccia: quanti span vedete? C'e' la query SQL?

Domanda: cosa fa Aspire che compose non fa, e viceversa?

## 5. Registry e deploy su Azure (step-05)

Obiettivo: l'app gira su Azure Container Apps, con le immagini in un Azure Container Registry.

1. `az login`, poi lanciate `TEAM=teamXX ./infra/deploy.sh` e leggetelo riga per riga mentre gira:
   - ACR con **admin user disabilitato**
   - `az acr build`: le immagini si costruiscono nel cloud
   - Container Apps con `--registry-identity system`: l'app scarica le immagini con la sua Managed Identity, senza password
   - API con ingress **interno**, frontend con ingress **esterno**
2. Aprite l'URL stampato alla fine. Cosa dice `/info` adesso (host, database)?
3. Pipeline: aggiungete `.github/workflows/deploy.yml`. Il docente vi da' gli id per il login OIDC; salvateli come **variabili** dell'environment `dev` (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `ACR_NAME`, `RESOURCE_GROUP`). Non sono secret: senza la federated credential non servono a nulla.
4. Fate una modifica visibile (titolo della pagina), PR, merge: dopo la CI parte il deploy. Controllate che `/info` mostri il nuovo SHA.
5. A fine giornata: `az group delete -n rg-shipboard-teamXX --yes --no-wait`.

Bonus: in Azure l'API usa il database in memoria. Aggiungete PostgreSQL (un container app con ingress TCP interno, oppure Azure Database for PostgreSQL) e passate la connection string.
