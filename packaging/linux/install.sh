#!/bin/sh
# Installs the "FinePdf" CUPS virtual printer. Run as root:
#     sudo ./install.sh /percorso/cartella-output
# The FinePdf app calls this via pkexec. Exit codes are consumed by LinuxPrinterManager:
#   0 ok | 3 snap-CUPS | 4 ghostscript missing | 5 cups/permission problem
set -eu

OUT_FOLDER="${1:-}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PRINTER="FinePdf"
PPD_DEST="/usr/share/ppd/finepdf/finepdf.ppd"
MARK_BEGIN="# >>> FinePdf >>>"
MARK_END="# <<< FinePdf <<<"

[ "$(id -u)" = 0 ] || { echo "ERROR: eseguire come root" >&2; exit 5; }
[ -n "$OUT_FOLDER" ] || { echo "ERROR: cartella di output mancante" >&2; exit 5; }

if snap list cups >/dev/null 2>&1; then
    echo "ERROR: CUPS installato come snap: non supportato. Usa il pacchetto 'cups' di sistema." >&2
    exit 3
fi

command -v gs >/dev/null 2>&1      || { echo "ERROR: ghostscript non installato" >&2; exit 4; }
command -v lpadmin >/dev/null 2>&1 || { echo "ERROR: cups (lpadmin) non installato" >&2; exit 5; }

SERVERBIN="$(cups-config --serverbin 2>/dev/null || echo /usr/lib/cups)"
BACKEND_DIR="$SERVERBIN/backend"
[ -d "$BACKEND_DIR" ] || { echo "ERROR: $BACKEND_DIR inesistente" >&2; exit 5; }

# 1. backend BEFORE lpadmin (CUPS validates the URI scheme at lpadmin time)
install -o root -g root -m 0700 "$SCRIPT_DIR/finepdf" "$BACKEND_DIR/finepdf"

# 2. PPD
install -D -o root -g root -m 0644 "$SCRIPT_DIR/finepdf.ppd" "$PPD_DEST"
if command -v cupstestppd >/dev/null 2>&1; then
    cupstestppd -q "$PPD_DEST" || { echo "ERROR: PPD non valida" >&2; exit 5; }
fi

# 3. system-wide fallback config (per-user settings.json still wins)
install -d -o root -g root -m 0755 /etc/finepdf
printf 'OutputFolder=%s\n' "$OUT_FOLDER" > /etc/finepdf/backend.conf
chmod 0644 /etc/finepdf/backend.conf

# 4. AppArmor: only needed if the cupsd profile is hardened (no third_party transition)
if [ -d /etc/apparmor.d ] && command -v apparmor_parser >/dev/null 2>&1 && aa-enabled --quiet 2>/dev/null; then
    PROFILE=/etc/apparmor.d/usr.sbin.cupsd
    if [ -f "$PROFILE" ] && ! grep -Eq 'backend/\*[[:space:]]+(Cx[[:space:]]*->|PUx|Pix|Ux)' "$PROFILE"; then
        install -d -m 0755 /etc/apparmor.d/local
        LOCAL=/etc/apparmor.d/local/usr.sbin.cupsd
        [ -f "$LOCAL" ] && sed -i "/$MARK_BEGIN/,/$MARK_END/d" "$LOCAL"
        {
            echo "$MARK_BEGIN"
            echo "  /usr/lib/cups/backend/finepdf rmix,"
            echo "  /home/** rwk,"
            echo "  /root/** rwk,"
            echo "  /etc/finepdf/** r,"
            echo "  /tmp/** rwk,"
            echo "  capability chown, capability dac_override, capability dac_read_search,"
            echo "  capability setuid, capability setgid, capability fowner,"
            echo "$MARK_END"
        } >> "$LOCAL"
        apparmor_parser -r "$PROFILE" 2>/dev/null || true
    fi
fi

# 5. register the queue (explicit enable/accept — avoids the position-sensitive -E)
lpadmin -p "$PRINTER" -v finepdf:/ -P "$PPD_DEST" \
        -D "FinePdf (stampante PDF virtuale)" -L "FinePdf" \
        -o printer-is-shared=false -o printer-error-policy=abort-job
cupsenable "$PRINTER" 2>/dev/null || true
cupsaccept "$PRINTER" 2>/dev/null || true

systemctl try-restart cups 2>/dev/null || service cups restart 2>/dev/null || true

echo "OK: stampante $PRINTER installata -> $OUT_FOLDER"
