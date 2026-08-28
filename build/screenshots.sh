#!/usr/bin/env bash
# Rigenera gli screenshot del README (docs/screenshots/*.png) su un display virtuale.
# Richiede: Xvfb, xdotool, ImageMagick (import). I PDF di esempio stanno in
# docs/screenshots/samples/.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOCS="$ROOT/docs/screenshots"
DISPLAY_NUM=":97"
CROP="1160x690+0+0"
APP=(dotnet run --project "$ROOT/src/FinePdf.App" -c Release)

command -v Xvfb    >/dev/null || { echo "manca Xvfb";   exit 1; }
command -v xdotool >/dev/null || { echo "manca xdotool"; exit 1; }
command -v import  >/dev/null || { echo "manca ImageMagick (import)"; exit 1; }

cleanup() { pkill -f FinePdf.App 2>/dev/null || true; pkill -f "Xvfb $DISPLAY_NUM" 2>/dev/null || true; }
trap cleanup EXIT

Xvfb "$DISPLAY_NUM" -screen 0 1400x950x24 >/dev/null 2>&1 &
sleep 2
export DISPLAY="$DISPLAY_NUM"

shot() { import -window root "$1"; convert "$1" -crop "$CROP" +repage "$1"; }

# settings.json pulito -> parte dalla cartella corrente (i sample)
rm -f "$HOME/.config/FinePdf/settings.json"

cd "$DOCS/samples"
"${APP[@]}" >/tmp/finepdf-screenshots.log 2>&1 &
sleep 14

# scheda "Cartella di lavoro": doppio click su relazione.pdf (3ª voce, sotto il pulsante
# "Cambia cartella" e le intestazioni delle schede)
xdotool mousemove 130 300 click --repeat 2 --delay 70 1
sleep 4
shot "$DOCS/overview.png"

# "2 pagine per foglio" su tutta la selezione (auto-rotazione)
xdotool key ctrl+a
sleep 1
xdotool mousemove 594 185 click 1
sleep 4
shot "$DOCS/nup.png"

echo "Screenshot aggiornati in $DOCS/"
