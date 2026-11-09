# ShipBoard

Bacheca di ticket usata nel corso **Modern DevOps** (ITS Mario Volpato).

| Cartella | Cosa c'e' |
| --- | --- |
| `src/ShipBoard.Api` | API .NET 10 (minimal API + EF Core) |
| `tests/ShipBoard.Api.Tests` | Test xUnit con il database in memoria |
| `web` | Frontend Angular |
| `.github/workflows` | Pipeline CI |
| `docs/` | Istruzioni dei lab ed esempi |

## Avvio in locale

```bash
# API su http://localhost:5080 (database in memoria)
dotnet run --project src/ShipBoard.Api

# Frontend su http://localhost:4200 (inoltra /api all'API)
cd web && npm ci && npm start
```

Endpoint utili: `/info` (dove gira l'app), `/health`, `/tickets`.

## Deploy su Azure

La workflow GitHub Actions esegue build e test per ogni pull request e push su
`main`. Dopo il successo dei controlli su `main` (anche con avvio manuale),
pubblica l'immagine `shipboard-api` su Azure Container Registry, con il commit
SHA come tag, e la distribuisce su un App Service Linux per container. Non
distribuisce le pull request o il frontend.

Preparare le risorse e la configurazione prima del primo deploy:

1. Creare un Azure Container Registry e un App Service Linux per container.
   Configurare l'app per scaricare immagini private dal registry con una
   managed identity e il ruolo di lettura appropriato per il registry (ad esempio
   `AcrPull` per un registry senza controllo ABAC). Impostare `WEBSITES_PORT=8080`
   nelle impostazioni dell'app. L'API usa il database in memoria per default:
   i ticket vengono persi al riavvio.
2. Creare un'identita' Azure per GitHub Actions con una credenziale federata
   OIDC per il repository e l'environment GitHub `production`. Assegnarle i
   permessi per push sul registry (ad esempio `AcrPush` senza ABAC) e per
   aggiornare l'App Service. La managed identity dell'app per
   il pull e' separata dall'identita' usata da GitHub per il deploy.
3. In GitHub, creare l'environment `production` e configurare questi secrets
   nell'environment o nel repository: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`,
   `AZURE_SUBSCRIPTION_ID`. Configurare queste variables nello stesso ambito:
   `ACR_NAME` (nome del registry), `ACR_LOGIN_SERVER` (login server completo,
   ad esempio `example.azurecr.io`) e `AZURE_API_APP_NAME` (nome dell'App Service).

La workflow non crea le risorse Azure. Vedere la guida Microsoft per
[OIDC con GitHub Actions](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure-openid-connect)
e per il [deploy di container su App Service](https://learn.microsoft.com/en-us/azure/app-service/deploy-container-github-action).

## Lab di oggi

Le istruzioni sono in [docs/lab-docker.md](docs/lab-docker.md).
Se resti indietro, ogni passo ha un branch con la soluzione: `git remote add prof https://github.com/matteobortolazzo/shipboard-docker`, poi `git fetch prof && git checkout prof/step-02 -- .` e riparti da li'.
