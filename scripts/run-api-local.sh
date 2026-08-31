#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"
cd "$REPOSITORY_ROOT"

if [[ -x "$HOME/.dotnet/dotnet" ]]; then
  export PATH="$HOME/.dotnet:$PATH"
fi

if [[ ! -f .env.local ]]; then
  echo "Error: .env.local was not found in the repository root." >&2
  exit 1
fi

set -a
# shellcheck disable=SC1091
source .env.local
set +a

if [[ "${SPC_LOCAL_DOCKER:-}" != "1" ]]; then
  echo "Error: local Docker activation requires SPC_LOCAL_DOCKER=1. Add SPC_LOCAL_DOCKER=1 to .env.local." >&2
  exit 1
fi

if [[ -n "${ASPNETCORE_ENVIRONMENT:-}" && "${ASPNETCORE_ENVIRONMENT}" != "Development" ]]; then
  echo "Error: refusing to launch with ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}. Set it to Development or remove it in .env.local." >&2
  exit 1
fi

export ASPNETCORE_ENVIRONMENT=Development

# Local Argentine SME deployment uses the dual-line current account (L2 for quotes).
export Licensing__Features__DualLineCurrentAccount=true

if [[ -z "${MSSQL_SA_PASSWORD:-}" ]]; then
  echo "Error: MSSQL_SA_PASSWORD must be set in .env.local." >&2
  exit 1
fi

export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=SPC;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True"

echo "Starting SPC API with local Docker SQL Server..."
exec dotnet run --project SPC.API/SPC.API.csproj

SPC_LOCAL_DOCKER=1
Licensing__Features__DualLineCurrentAccount=true
