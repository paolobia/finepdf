#!/bin/sh
# Removes the "FinePdf" CUPS virtual printer. Run as root (the FinePdf app calls this via pkexec).
set -u

PRINTER="FinePdf"
MARK_BEGIN="# >>> FinePdf >>>"
MARK_END="# <<< FinePdf <<<"

[ "$(id -u)" = 0 ] || { echo "ERROR: eseguire come root" >&2; exit 5; }

SERVERBIN="$(cups-config --serverbin 2>/dev/null || echo /usr/lib/cups)"

lpadmin -x "$PRINTER" 2>/dev/null || true
rm -f "$SERVERBIN/backend/finepdf" "/etc/cups/ppd/$PRINTER.ppd"
rm -rf /usr/share/ppd/finepdf /etc/finepdf

LOCAL=/etc/apparmor.d/local/usr.sbin.cupsd
if [ -f "$LOCAL" ]; then
    sed -i "/$MARK_BEGIN/,/$MARK_END/d" "$LOCAL"
    [ -d /etc/apparmor.d ] && command -v apparmor_parser >/dev/null 2>&1 \
        && apparmor_parser -r /etc/apparmor.d/usr.sbin.cupsd 2>/dev/null || true
fi

systemctl try-restart cups 2>/dev/null || service cups restart 2>/dev/null || true

echo "OK: stampante $PRINTER rimossa"
