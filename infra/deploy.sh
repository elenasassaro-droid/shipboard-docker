#!/usr/bin/env bash
# Primo deploy su Azure: registry (ACR) + Container Apps.
# Uso:  TEAM=team01 ./infra/deploy.sh
# Requisiti: az CLI con `az login` fatto. Docker locale NON serve: le immagini le costruisce ACR.
set -euo pipefail

TEAM="${TEAM:?Imposta TEAM, es. TEAM=team01}"
LOCATION="${LOCATION:-westeurope}"
RG="${RG:-rg-shipboard-$TEAM}"
ACR="${ACR:-acrshipboard${TEAM//[^a-z0-9]/}}"   # solo lettere minuscole e numeri
ENV_NAME="cae-shipboard"
TAG="${TAG:-$(git rev-parse --short HEAD)}"

echo "==> Resource group $RG ($LOCATION)"
az group create -n "$RG" -l "$LOCATION" -o none

echo "==> Azure Container Registry $ACR (admin user disabilitato: si entra solo con identita')"
az acr create -n "$ACR" -g "$RG" --sku Basic --admin-enabled false -o none

echo "==> Build delle immagini nel cloud con ACR Tasks (tag $TAG)"
az acr build -r "$ACR" -t "shipboard-api:$TAG" src/ShipBoard.Api
az acr build -r "$ACR" -t "shipboard-web:$TAG" web

echo "==> Container Apps environment"
az containerapp env create -n "$ENV_NAME" -g "$RG" -l "$LOCATION" -o none

# --registry-identity system: l'app usa la sua Managed Identity per scaricare le immagini (ruolo AcrPull).
# Niente password del registry da nessuna parte.
echo "==> API (ingress interno: raggiungibile solo dentro l'environment)"
az containerapp create -n shipboard-api -g "$RG" --environment "$ENV_NAME" \
  --image "$ACR.azurecr.io/shipboard-api:$TAG" \
  --registry-server "$ACR.azurecr.io" --registry-identity system \
  --ingress internal --target-port 8080 \
  --env-vars APP_VERSION="$TAG" \
  --min-replicas 0 --max-replicas 2 -o none

echo "==> Frontend (ingress esterno, inoltra /api all'API per nome)"
az containerapp create -n shipboard-web -g "$RG" --environment "$ENV_NAME" \
  --image "$ACR.azurecr.io/shipboard-web:$TAG" \
  --registry-server "$ACR.azurecr.io" --registry-identity system \
  --ingress external --target-port 8080 \
  --env-vars API_URL=http://shipboard-api \
  --min-replicas 0 --max-replicas 2 -o none

URL=$(az containerapp show -n shipboard-web -g "$RG" --query properties.configuration.ingress.fqdn -o tsv)
echo
echo "Fatto: https://$URL"
echo "Ricorda a fine giornata: az group delete -n $RG --yes --no-wait"
