#!/usr/bin/env bash
# Pubblica FinePdf come singolo eseguibile self-contained.
#   ./build/publish.sh            -> runtime dell'host (linux-x64)
#   ./build/publish.sh win-x64    -> build Windows (eseguire su Windows per la stampa GDI)
#   ./build/publish.sh linux-x64  -> build Linux esplicita
set -euo pipefail

RID="${1:-linux-x64}"
CONFIG="${2:-Release}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="$ROOT/artifacts/$RID"

echo "==> dotnet test"
dotnet test "$ROOT/FinePdf.sln" -c "$CONFIG" --nologo

echo "==> dotnet publish ($RID, $CONFIG) -> $OUT"
dotnet publish "$ROOT/src/FinePdf.App/FinePdf.App.csproj" \
  -c "$CONFIG" -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$OUT"

echo "==> fatto: $OUT"
