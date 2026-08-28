# Stampante virtuale "FinePdf" (Linux)

> **Windows:** FinePdf non installa una stampante; ci si appoggia a una stampante PDF gratuita
> di terzi e FinePdf ne osserva la cartella. Vedi [`virtual-printer-windows.md`](virtual-printer-windows.md).

Registra una stampante di sistema chiamata **FinePdf**. Stampando lì da qualsiasi applicazione
(Firefox, LibreOffice, evince…) il documento viene salvato come PDF nella cartella scelta, con
un nome tipo `2026-08-28_09-15-42__Relazione__cairo.pdf`.

Nell'app, la scheda **«PDF da stampante»** del pannello sinistro mostra quella cartella in tempo
reale. Se **«Cestina i PDF dopo l'aggiunta»** è attivo (impostazione della scheda, predefinito
sì), dopo aver accodato le pagine il PDF di origine viene spostato nel **cestino** (recuperabile;
le pagine restano nella composizione perché copiate in cache). Dopo aver stampato, il nuovo PDF
compare subito e con un doppio click lo aggiungi alla
composizione.

## Installazione

**Dall'app:** toolbar → **«Stampante virtuale…»** → scegli la cartella → **Installa**. Una sola
richiesta di autorizzazione (polkit / password). Lo stesso pulsante, a stampante installata,
mostra **Rimuovi**.

**Manuale** (dal tarball, cartella `cups/`):

```sh
sudo ./cups/install.sh ~/FinePdf      # installa e imposta la cartella
sudo ./cups/uninstall.sh              # rimuove tutto
```

`install.sh` termina con una **stampa di prova**: registra la coda, invia un job di una
riga e verifica che un PDF venga davvero prodotto; se non compare esce con codice 6 e
indica i log da controllare. Per saltarla: `FINEPDF_SKIP_SELFTEST=1 sudo ./cups/install.sh …`.

### Dipendenze

| Componente | Debian/Ubuntu | Arch/Manjaro | Obbligatorio |
|---|---|---|---|
| CUPS server | `cups` | `cups` | sì |
| CUPS client (`lp`, `lpstat`, `lpadmin`) | `cups-client` | incluso in `cups` | sì |
| Ghostscript (`gs`) | `ghostscript` | `ghostscript` | sì |
| polkit (`pkexec`) | `policykit-1` / `polkit` | `polkit` + un agent (`polkit-kde-agent-1` / `polkit-gnome`) | per l'installazione dall'app |
| Python 3 | `python3` | `python` | sì (il backend è uno script Python) |
| poppler (`pdfinfo`) | `poppler-utils` | `poppler` | opzionale (arricchisce `__Origine`) |

Su **Arch/Manjaro** assicurati che CUPS sia abilitato: `sudo systemctl enable --now cups.socket`
(`install.sh` prova comunque ad avviarlo). Manjaro non usa AppArmor di default → il blocco
relativo in `install.sh` viene saltato e il backend gira senza confinamento (nessun problema).

## Come funziona

`/usr/lib/cups/backend/finepdf` (script Python, `0700 root:root`) viene eseguito da CUPS come
root: risolve l'utente proprietario del job, legge la sua configurazione, poi **rilascia i
privilegi** (`fork` + `setuid`) prima di toccare il filesystem o Ghostscript.

- Se i dati in arrivo iniziano con `%PDF` (es. stampa da evince) vengono **copiati così come
  sono**.
- Altrimenti CUPS li filtra in PostScript e il backend li converte in PDF con
  `gs -sDEVICE=pdfwrite` (nessun `-sPAPERSIZE`: A4/A3 e formati misti nello stesso job sono
  preservati).

### Cartella di output — ordine di ricerca

1. `~/.config/FinePdf/settings.json` → chiave **`printerOutputFolder`** (scritta dall'app).
2. `/etc/finepdf/backend.conf` → `OutputFolder=…` (scritta da `install.sh`).
3. `~/FinePdf`, poi `~/Desktop` / `~/Scrivania`, poi `~`.

Viene usata la **prima cartella scrivibile**. Cambiare la cartella nel dialogo re-installa la
stampante (aggiorna il punto 2); il backend legge comunque il punto 1 a ogni stampa, quindi non
serve riavviare nulla.

### Schema del nome file

`AAAA-MM-GG_HH-MM-SS` (ora locale) + `__<Titolo>` (dal titolo del job, se significativo) +
`__<Origine>` (app d'origine, da `%%Creator:` del PostScript o dai metadati del PDF; assente se
non rilevabile). Collisioni nello stesso secondo: `_01`, `_02`, …

## Limitazioni note

- **CUPS installato come snap** (Ubuntu con `snap install cups`): non supportato — cupsd nello
  snap non vede il backend sull'host. `install.sh` si interrompe con un errore. Usare il
  pacchetto `cups` di sistema.
- **`XDG_CONFIG_HOME` personalizzato**: l'app scrive `settings.json` altrove; il backend cerca
  in `~/.config/FinePdf/` e ripiega su `/etc/finepdf/backend.conf` / `~/FinePdf`.
- **Conversione PostScript → PDF**: campi modulo, annotazioni e segnalibri di un PDF originale
  si perdono, **tranne** con il percorso rapido `%PDF` (copia integrale).
- **Copie multiple** ignorate (una stampante virtuale produce un solo file).
- **AppArmor**: sul profilo `usr.sbin.cupsd` standard di Ubuntu/Debian il backend è "third
  party" e non confinato per il filesystem. `install.sh` aggiunge un frammento locale solo se
  rileva un profilo indurito.

## Verifica

```sh
lpstat -v FinePdf                                  # device for FinePdf: finepdf:/
printf 'ciao\n' | lp -d FinePdf -t "Prova"
ls -lt ~/FinePdf | head                            # 2026-..__Prova.pdf
lp -d FinePdf -o media=A3 -t "A3" doc.pdf
pdfinfo ~/FinePdf/*A3*.pdf | grep 'Page size'      # ~842 x 1191
for i in 1 2 3; do printf x | lp -d FinePdf -t burst & done; wait   # burst + burst_01 + burst_02
```
