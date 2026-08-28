# FinePdf — stampante virtuale CUPS (Linux)

Stampando verso la stampante **FinePdf** da qualsiasi applicazione (Firefox, LibreOffice,
evince…) il documento viene salvato come PDF nella cartella configurata da FinePdf, con un
nome del tipo `2026-08-28_09-15-42__Titolo__Firefox.pdf`.

## Dipendenze

| Pacchetto | Debian/Ubuntu | Note |
|---|---|---|
| CUPS server | `cups` | obbligatorio |
| CUPS client (`lp`, `lpstat`, `lpadmin`) | `cups-client` | obbligatorio |
| Ghostscript (`gs`) | `ghostscript` | obbligatorio (conversione PostScript → PDF) |
| polkit (`pkexec`) | `polkit` / `policykit-1` | per l'installazione dall'app |
| Python 3 | `python3` | il backend è uno script Python |
| poppler (`pdfinfo`) | `poppler-utils` | opzionale: aggiunge `__Origine` al nome file |

**Non supportato:** CUPS installato come *snap* (Ubuntu con `snap install cups`): il backend
sull'host non è visibile a cupsd nello snap. Usare il pacchetto `cups` di sistema.

## Installazione

Dall'app: pulsante **«Stampante virtuale…»** → scegli la cartella → *Installa* (un prompt polkit).

Manuale (dal tarball, cartella `cups/`):

```sh
sudo ./cups/install.sh ~/FinePdf
```

Rimozione:

```sh
sudo ./cups/uninstall.sh
```

## Contratto di configurazione

Il backend legge, in ordine:

1. `~/.config/FinePdf/settings.json` → chiave **`printerOutputFolder`** (scritta dall'app).
2. `/etc/finepdf/backend.conf` → riga `OutputFolder=…` (scritta da `install.sh`).
3. `~/FinePdf`, poi `~/Desktop` / `~/Scrivania`, poi `~`.

Viene usata la prima cartella scrivibile. Se `XDG_CONFIG_HOME` è personalizzato, l'app scrive
altrove e il backend cade sul punto 2/3.

## Limitazioni note

- La conversione PostScript → PDF **ridistilla**: campi modulo, annotazioni e segnalibri di un
  PDF originale si perdono, **tranne** quando l'input è già `%PDF` (es. stampa da evince), dove
  il file viene copiato così com'è.
- Il numero di copie è ignorato (una stampante virtuale produce un solo file).
- Nome file con ora **locale** della macchina.

## Verifica end-to-end

```sh
lpstat -v FinePdf                       # device for FinePdf: finepdf:/
printf 'test\n' | lp -d FinePdf -t "Prova"
ls -lt ~/FinePdf | head                 # 2026-..__Prova.pdf
lp -d FinePdf -o media=A3 -t "A3" prova.pdf ; pdfinfo ~/FinePdf/*A3*.pdf | grep 'Page size'
for i in 1 2 3; do printf x | lp -d FinePdf -t burst & done; wait   # burst + burst_01 + burst_02
```
