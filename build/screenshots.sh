#!/usr/bin/env bash
# Rigenera gli screenshot del README (docs/screenshots/*.png) su un display virtuale.
# Richiede: Xvfb, xdotool, ImageMagick (import). I PDF di esempio stanno in
# docs/screenshots/samples/ e sono generati da questo script se mancano.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOCS="$ROOT/docs/screenshots"
DISPLAY_NUM=":97"
APP=(dotnet run --project "$ROOT/src/FinePdf.App" -c Release)

command -v Xvfb   >/dev/null || { echo "manca Xvfb";   exit 1; }
command -v xdotool>/dev/null || { echo "manca xdotool";exit 1; }
command -v import >/dev/null || { echo "manca ImageMagick (import)"; exit 1; }

cleanup() { pkill -f FinePdf.App 2>/dev/null || true; pkill -f "Xvfb $DISPLAY_NUM" 2>/dev/null || true; }
trap cleanup EXIT

Xvfb "$DISPLAY_NUM" -screen 0 1400x900x24 >/dev/null 2>&1 &
sleep 2
export DISPLAY="$DISPLAY_NUM"

cd "$DOCS/samples"
"${APP[@]}" >/tmp/finepdf-screenshots.log 2>&1 &
sleep 13

# aggiungi le 4 pagine di relazione.pdf (3ª voce nell'elenco sorgenti)
xdotool mousemove 120 215 click --repeat 2 --delay 60 1
sleep 4
import -window root "$DOCS/overview.png"

# 2 pagine per foglio su tutta la selezione (auto-rotazione)
xdotool key ctrl+a
sleep 1
xdotool mousemove 592 143 click 1
sleep 4
import -window root "$DOCS/nup.png"

echo "Screenshot aggiornati in $DOCS/"
