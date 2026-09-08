#!/usr/bin/env bash
set -euo pipefail

DOTNET_VERSION="8.0.400"
DOTNET_DIR="/tmp/dotnet"

echo "==> Instalando .NET SDK ${DOTNET_VERSION}"
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --version "${DOTNET_VERSION}" --install-dir "${DOTNET_DIR}"
export PATH="${DOTNET_DIR}:$PATH"

echo "==> .NET"
dotnet --info

echo "==> Instalando dependencias del frontend"
npm ci --prefix src/ClinicaPro.Client

echo "==> Generando Tailwind CSS"
npm run build --prefix src/ClinicaPro.Client

echo "==> Publicando ClinicaPro.Client"
rm -rf .vercel_publish
dotnet publish src/ClinicaPro.Client/ClinicaPro.Client.csproj \
  -c Release \
  -o .vercel_publish

echo "==> Build Vercel completado"
