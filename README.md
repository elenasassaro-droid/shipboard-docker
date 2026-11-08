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

## Lab di oggi

Le istruzioni sono in [docs/lab-docker.md](docs/lab-docker.md).
Se resti indietro, ogni passo ha un branch con la soluzione: `git fetch origin && git checkout origin/step-02 -- .` e riparti da li'.
