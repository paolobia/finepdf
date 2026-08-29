#!/bin/sh
# Installs the "FinePdf" CUPS virtual printer. Run as root:
#     sudo ./install.sh /percorso/cartella-output
# The FinePdf app calls this via pkexec. Exit codes are consumed by LinuxPrinterManager:
#   0 ok | 3 snap-only CUPS | 4 ghostscript missing | 5 cups/permission problem
#   6 registered but the self-test print produced no PDF
set -eu

OUT_FOLDER="${1:-}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PRINTER="FinePdf"
PPD_DEST="/usr/share/ppd/finepdf/finepdf.ppd"
MARK_BEGIN="# >>> FinePdf >>>"
MARK_END="# <<< FinePdf <<<"

[ "$(id -u)" = 0 ] || { echo "ERROR: eseguire come root" >&2; exit 5; }
[ -n "$OUT_FOLDER" ] || { echo "ERROR: cartella di output mancante" >&2; exit 5; }

# Prefer the distro's CUPS tools over any snap shim that might shadow them on PATH.
sysbin() { if [ -x "/usr/bin/$1" ]; then printf '/usr/bin/%s' "$1"; else printf '%s' "$1"; fi; }
LPADMIN="$(sysbin lpadmin)"
LP="$(sysbin lp)"
CUPSENABLE="$(sysbin cupsenable)"
CUPSACCEPT="$(sysbin cupsaccept)"
CUPSTESTPPD="$(sysbin cupstestppd)"
CUPSCONFIG="$(sysbin cups-config)"

command -v gs >/dev/null 2>&1 || { echo "ERROR: ghostscript non installato" >&2; exit 4; }
[ -x "$LPADMIN" ] || command -v lpadmin >/dev/null 2>&1 \
    || { echo "ERROR: cups (lpadmin) non installato" >&2; exit 5; }

SERVERBIN="$("$CUPSCONFIG" --serverbin 2>/dev/null || echo /usr/lib/cups)"
BACKEND_DIR="$SERVERBIN/backend"

# snap-CUPS is a problem ONLY when there is no host CUPS: the snap's cupsd cannot see a
# backend on the host filesystem. If a native cupsd + backend dir exist, proceed regardless
# of a snap 'cups' also being installed.
HOST_CUPSD=""
for c in /usr/sbin/cupsd /usr/lib/cups/daemon/cupsd "$SERVERBIN/daemon/cupsd"; do
    [ -x "$c" ] && HOST_CUPSD="$c" && break
done
command -v cupsd >/dev/null 2>&1 && HOST_CUPSD="${HOST_CUPSD:-cupsd}"
if [ -z "$HOST_CUPSD" ] || [ ! -d "$BACKEND_DIR" ]; then
    if snap list cups >/dev/null 2>&1; then
        echo "ERROR: CUPS disponibile solo come snap: non supportato. Installa il pacchetto 'cups' di sistema." >&2
        exit 3
    fi
    echo "ERROR: CUPS di sistema non trovato ($BACKEND_DIR inesistente)." >&2
    exit 5
fi

# 1. backend BEFORE lpadmin (CUPS validates the URI scheme at lpadmin time)
install -o root -g root -m 0700 "$SCRIPT_DIR/finepdf" "$BACKEND_DIR/finepdf"

# 2. PPD
install -D -o root -g root -m 0644 "$SCRIPT_DIR/finepdf.ppd" "$PPD_DEST"
if [ -x "$CUPSTESTPPD" ] || command -v cupstestppd >/dev/null 2>&1; then
    "$CUPSTESTPPD" -q "$PPD_DEST" || { echo "ERROR: PPD non valida" >&2; exit 5; }
fi

# 3. system-wide fallback config (per-user settings.json still wins).
#    Start pointed at a scratch dir so the self-test in step 7 lands there.
install -d -o root -g root -m 0755 /etc/finepdf
SMOKE_DIR="$(mktemp -d /tmp/finepdf-smoke.XXXXXX)"
chmod 0777 "$SMOKE_DIR"
printf 'OutputFolder=%s\n' "$SMOKE_DIR" > /etc/finepdf/backend.conf
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

# 5. make sure cupsd is reachable (Arch/Manjaro is often only socket-activated)
if command -v systemctl >/dev/null 2>&1; then
    systemctl start cups.socket 2>/dev/null \
        || systemctl start cups.service 2>/dev/null \
        || systemctl start org.cups.cupsd.service 2>/dev/null || true
fi

# 6. register the queue (explicit enable/accept — avoids the position-sensitive -E)
"$LPADMIN" -p "$PRINTER" -v finepdf:/ -P "$PPD_DEST" \
        -D "FinePdf (stampante PDF virtuale)" -L "FinePdf" \
        -o printer-is-shared=false -o printer-error-policy=abort-job
"$CUPSENABLE" "$PRINTER" 2>/dev/null || true
"$CUPSACCEPT" "$PRINTER" 2>/dev/null || true

systemctl try-restart cups.service 2>/dev/null \
    || systemctl try-restart org.cups.cupsd.service 2>/dev/null \
    || service cups restart 2>/dev/null || true

# 7. self-test: push a one-line job and check a PDF really lands in the scratch dir
if [ "${FINEPDF_SKIP_SELFTEST:-}" != "1" ]; then
    printf 'FinePdf: prova di installazione %s\n' "$(date)" \
        | "$LP" -d "$PRINTER" -t "FinePdf self-test" >/dev/null 2>&1 || true
    i=0
    while [ "$i" -lt 25 ]; do
        [ -n "$(find "$SMOKE_DIR" -maxdepth 1 -name '*.pdf' -print -quit 2>/dev/null)" ] && break
        sleep 1
        i=$((i + 1))
    done
    produced="$(find "$SMOKE_DIR" -maxdepth 1 -name '*.pdf' -print -quit 2>/dev/null || true)"
    # restore the real output folder before reporting
    printf 'OutputFolder=%s\n' "$OUT_FOLDER" > /etc/finepdf/backend.conf
    chmod 0644 /etc/finepdf/backend.conf
    rm -rf "$SMOKE_DIR"
    if [ -z "$produced" ]; then
        echo "ERROR: la stampante è registrata ma la stampa di prova non ha prodotto alcun PDF." >&2
        echo "       Controlla:  journalctl -u cups --since '2 min ago'  e  /var/log/cups/error_log" >&2
        exit 6
    fi
    echo "OK: prova di stampa riuscita"
else
    printf 'OutputFolder=%s\n' "$OUT_FOLDER" > /etc/finepdf/backend.conf
    chmod 0644 /etc/finepdf/backend.conf
    rm -rf "$SMOKE_DIR"
fi

echo "OK: stampante $PRINTER installata -> $OUT_FOLDER"
