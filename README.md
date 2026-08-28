# FinePdf

🇮🇹 Italiano · [🇬🇧 English](README.en.md)

Editor PDF **desktop cross-platform (Windows + Linux)** in C# / .NET 8: unione, riordino,
cancellazione, imposizione "N pagine in una" (N-up), rotazione e stampa delle pagine.

## Funzionalità

- **Pannello sorgenti a 2 schede**: *Cartella di lavoro* (navigabile; accetta PDF **e immagini**
  `png/jpg/bmp/gif/webp`, convertite in PDF una pagina per immagine) e *PDF da stampante*
  (cartella della stampante virtuale, sola lettura). Doppio click su un file di una qualsiasi
  scheda → accoda le pagine alla composizione. Ultima cartella e cartella stampante ricordate.
- **Stampante virtuale «FinePdf»**: su **Linux** registra una vera stampante di sistema
  (un prompt di autorizzazione) → [`docs/virtual-printer.md`](docs/virtual-printer.md); su
  **Windows** ci si appoggia a una stampante PDF gratuita di terzi e FinePdf ne osserva la
  cartella → [`docs/virtual-printer-windows.md`](docs/virtual-printer-windows.md). In entrambi
  i casi i PDF stampati compaiono nella scheda *PDF da stampante*.
- **Unione**: doppio click su un PDF nell'elenco sorgenti → accoda tutte le sue pagine.
- **Riordino**: drag & drop nell'anteprima, pulsanti *Sposta su/giù*, `Alt+↑/↓`.
- **Cancellazione**: selezione singola o multipla, menu contestuale o `Canc`.
- **N-up**: seleziona più fogli → *Due / Quattro pagine per foglio*; ordine di riempimento
  per righe o per colonne; oppure *Unisci in un unico foglio*.
- **Rotazione**: 90° oraria / antioraria / 180° (`Ctrl+←/→`).
- **Pagina vuota**: dal menu contestuale, eredita formato e orientazione del documento.
- **Adattamento** (`FitMode`): predefinito di documento + override per foglio
  (adatta / dimensione reale / riempi).
- **Margini** di pagina uniformi (mm), applicati a ogni foglio; guida tratteggiata nell'anteprima.
- **Stampa**: il pannello destro **è** l'anteprima di stampa; dialog interno con stampante,
  copie, intervallo (`1-3, 5`), fronte/retro e **opzioni avanzate** libere `chiave=valore`
  (passate a CUPS come `-o chiave=valore`: `media`, `Resolution`, `ColorModel`, …).
  Linux via CUPS (`lp`/`lpstat`), Windows via GDI.
- **Undo/redo** completo; avviso *modifiche non salvate* alla chiusura.

## Screenshot

Anteprima di stampa a due pannelli — ogni foglio è disegnato nel formato di stampa del documento:

![Panoramica](docs/screenshots/overview.png)

"Due pagine per foglio" con auto-rotazione: le pagine verticali vengono ruotate di 90° per
riempire le celle:

![N-up](docs/screenshots/nup.png)

Gli screenshot si rigenerano con `./build/screenshots.sh` (richiede Xvfb, xdotool, ImageMagick).

## Avvio rapido

```bash
dotnet build FinePdf.sln
dotnet test
dotnet run --project src/FinePdf.App
```

Richiede **.NET SDK 8.0.x**. Su Linux/macOS i progetti sono single-target `net8.0`; su Windows
`FinePdf.App` passa a `net8.0-windows` per la stampa GDI (`WindowsPrintService`).

La **stampante virtuale** (Linux) richiede a runtime: `cups`, `cups-client`, `ghostscript`,
`python3`, `polkit`/`policykit-1` (e opzionalmente `poppler-utils`). Dettagli in
[`docs/virtual-printer.md`](docs/virtual-printer.md).

## Struttura

| Progetto | Ruolo |
|---|---|
| `src/FinePdf.Core` | logica PDF senza dipendenze UI (modelli, servizi, operazioni undoable) |
| `src/FinePdf.App` | applicazione Avalonia (MVVM) |
| `tests/FinePdf.Core.Tests` | unit test xUnit (PDF di prova generati a runtime) |

Dettagli architetturali, stack bloccato e convenzioni: [`CLAUDE.md`](CLAUDE.md).

## Pubblicazione

```bash
./build/publish.sh linux-x64      # -> artifacts/linux-x64
./build/publish.ps1 -Rid win-x64  # su Windows -> artifacts/win-x64 (stampa GDI)
```

Entrambi eseguono prima `dotnet test`, poi `dotnet publish` self-contained a singolo file.
Gli eseguibili già compilati sono allegati alle [Release](../../releases) di GitHub.
