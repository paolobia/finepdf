# FinePdf

Applicazione **desktop cross-platform (Windows + Linux)** in **C# / .NET 8** per modificare
file PDF: unione, riordino, cancellazione, imposizione "due pagine in una" (N-up), rotazione
e stampa delle pagine.

## Requisiti funzionali (v1)

| # | Requisito | Note |
|---|---|---|
| 1 | **Unire** pagine da uno o più PDF | doppio click su un file nell'elenco sorgenti → accoda tutte le sue pagine alla composizione |
| 2 | **Spostare** / riordinare le pagine | drag & drop nell'anteprima **e** pulsanti Sposta su/giù (+ scorciatoie) |
| 3 | **Cancellare** pagine | selezione singola o multipla; da menu contestuale |
| 4 | **Due pagine in una** (N-up) | seleziono le pagine → "Unisci"; disposizione automatica in base all'orientazione del foglio; generalizzabile a 2/4 per foglio |
| 5 | **Ruotare** le pagine | menu contestuale: 90° oraria / 90° antioraria / 180° |
| 6 | **Stampare** il risultato | il pannello destro **è** l'anteprima di stampa; dialog interno: stampante, copie, intervallo, fronte/retro |
| + | **Inserire pagina vuota** | menu contestuale; eredita formato/orientazione del documento |

**Undo/redo** completo su tutte le operazioni. Avviso "modifiche non salvate" alla chiusura.

## Interfaccia (v1)

Finestra a **due pannelli**:

- **Sinistra — Sorgenti.** Elenco piatto dei `*.pdf` nella **cartella corrente** (all'avvio =
  cartella di lavoro del processo). Pulsante **"Cambia cartella"**; l'app ricorda l'ultima usata.
  Aggiornamento **automatico** della lista (`FileSystemWatcher`). **Doppio click** su un file =
  accoda tutte le sue pagine alla composizione. (Altri tipi di file: previsti in architettura,
  non implementati in v1.)
- **Destra — Anteprima di stampa.** Fogli separati, ciascuno disegnato nel **formato e
  orientazione di stampa** del documento (impostazione **unica di documento**, default A4
  verticale, modificabile dalla toolbar). Dentro ogni foglio si vede il contenuto assegnato:
  1 pagina sorgente per foglio, oppure 2/4 scalate quando si usa "Unisci". Ciò che si vede è
  esattamente ciò che verrà stampato.
  - **Menu contestuale (tasto destro)** sul foglio: ruota 90°/90°/180°, unisci pagine selezionate,
    cancella, inserisci pagina vuota, adattamento contenuto (vedi sotto). Altre voci da definire.
  - **Selezione multipla** per applicare un'operazione a più fogli insieme.
  - **Riordino**: drag & drop nell'anteprima e pulsanti Sposta su/giù.

**Adattamento contenuto al foglio** (`FitMode`): default **"adatta mantenendo le proporzioni"**
(nessun taglio, eventuali margini bianchi); modificabile dall'utente per foglio — alternative
"dimensione reale centrata" e "riempi il foglio".

## Stack (versioni bloccate)

- **.NET SDK 8.0.x** — vincolo dell'ambiente: Roslyn 4.8, C# 12.
  - ⚠️ **Avalonia 12** richiede Roslyn 4.14 (SDK 9/10): usare la **serie 11.3.x**.
  - ⚠️ Le **partial property** `[ObservableProperty] public partial T X { get; set; }` di
    CommunityToolkit.Mvvm richiedono C# 13: usare la sintassi a campo `[ObservableProperty] private T _x;`.
- **Avalonia UI 11.3.0** — interfaccia XAML/MVVM, unica UI per Windows e Linux
  (`Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`,
  `Avalonia.Diagnostics` solo in Debug). DevTools: `this.AttachDevTools()` in `MainWindow`.
- **CommunityToolkit.Mvvm 8.4.2** — `ObservableObject` / `RelayCommand`
- **PdfSharp 6.2.4** (MIT) — id pacchetto `PdfSharp` — merge, riordino, delete, rotate, N-up
- **PDFtoImage 4.1.1 + SkiaSharp 2.88.9** (MIT) — rendering pagina→immagine per miniature/anteprima (PDFium)
  - ⚠️ PDFtoImage 5.x e l'API `SKSamplingOptions` richiedono SkiaSharp 3.x: incompatibile con
    Avalonia 11.3 (SkiaSharp 2.88.x). Restare su PDFtoImage 4.x; per lo scaling usare
    `SKPaint.FilterQuality`. SkiaSharp **2.88.9** (non 2.88.8) per allinearsi ad `Avalonia.Skia`.
- **xUnit** — test con `Assert` nudo (niente FluentAssertions: la v8 è a licenza commerciale);
  i PDF di prova sono generati a runtime con PdfSharp

Non usare **iText** (AGPL/commerciale) né **FluentAssertions ≥ 8** (licenza a pagamento).
PdfSharp copre tutte le operazioni richieste.

`FinePdf.Core` e `FinePdf.App` scelgono il TFM in base al SO (condizione `'$(OS)' == 'Windows_NT'`):
su Windows `FinePdf.Core` è multi-target `net8.0;net8.0-windows` e `FinePdf.App` è `net8.0-windows`
(stampa GDI, `WindowsPrintService` compilato solo con `#if WINDOWS`); altrove entrambi restano
single-target `net8.0`, così `dotnet run`/`dotnet test` non richiedono `-f`. Il `WindowsPrintService`
non viene compilato su Linux: verificarlo su un runner Windows (CI).

## Struttura

```
src/FinePdf.Core            libreria senza dipendenze UI
  Models/                   Composition, Sheet, Placement, SourceDocument/SourcePage,
                            PaperSize, PageOrientation, FitMode, RotationDegrees, NUpOrder,
                            PrintOptions, PageRange, DuplexMode
  Layout/                   Rect, PlacedContent, SheetLayout (matematica di imposizione condivisa)
  Services/                 IPdfDocumentService/PdfSharpDocumentService,
                            IFolderSourceService/FolderSourceService,
                            IPdfComposer/PdfComposer,
                            ISheetRenderService/SkiaSheetRenderService,
                            IProcessRunner/ProcessRunner,
                            IPrintService + PrintServiceFactory
                              (LinuxCupsPrintService, WindowsPrintService [net8.0-windows])
  Operations/               IUndoableAction, IUndoRedoStack/UndoRedoStack, CompositeAction,
                            Actions/ (AddFilePages, MoveSheet, DeleteSheets, RotateSheet,
                            MergeSheets, NUp, SetNUpOrder, InsertBlankSheet, SetSheetFitMode,
                            SetDefaultFitMode, SetPaper)
  PdfLoadException.cs, PrintException.cs
src/FinePdf.App             app Avalonia (MVVM): ViewModels, Views, Services UI
tests/FinePdf.Core.Tests    unit test xUnit (PDF di prova generati a runtime, TestSupport/)
```

Regola di dipendenza: `FinePdf.App` → `FinePdf.Core`. `FinePdf.Core` **non** referenzia Avalonia
né codice UI.

## Comandi

```bash
dotnet new install Avalonia.Templates      # una tantum, per lo scaffolding iniziale
dotnet build FinePdf.sln
dotnet test                                # tutti gli unit test
dotnet run --project src/FinePdf.App       # avvia l'app in locale

# pubblicazione
dotnet publish src/FinePdf.App -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
dotnet publish src/FinePdf.App -c Release -r win-x64   --self-contained -p:PublishSingleFile=true
```

## Concetti chiave

Il documento in lavorazione è una **composizione di fogli**; ogni foglio contiene uno o più
"piazzamenti" di contenuto sorgente. L'anteprima e l'output di stampa usano **lo stesso**
motore di layout.

- **`Composition`** — sessione di lavoro: `PaperSize`, `Orientation`, `FitMode` di default e
  lista ordinata di `Sheet`. Non tocca i file finché non si salva/esporta.
- **`Sheet`** — un foglio di output: `Id`, lista ordinata di `Placement`
  (0 = pagina vuota, 1 = normale, 2/4 = N-up) e `FitMode` opzionale (se nullo eredita quello
  del documento).
- **`Placement`** — un contenuto sorgente su un foglio: `(SourceDocument, SourcePageIndex,
  RotationDegrees)` con `RotationDegrees` ∈ {0, 90, 180, 270}.
- **`SourceDocument`** — un PDF caricato dalla cartella corrente: percorso, numero pagine,
  handle PdfSharp in cache.
- **`FolderSourceService`** — enumera i `*.pdf` di una cartella e ne notifica le variazioni
  (`FileSystemWatcher`).
- **`PdfComposer`** — genera il PDF di output dalla `Composition`: per ogni `Sheet` crea una
  pagina nel formato/orientazione del documento, poi disegna ogni `Placement` come `XPdfForm`
  nel rettangolo calcolato dal `FitMode` (1 piazzamento = foglio intero, 2 = metà, 4 = quarti),
  applicando la rotazione. **Unifica** merge, riordino, delete, rotate e N-up: l'"unione due
  pagine in una" è semplicemente un `Sheet` con più `Placement`, nessun servizio separato.
- **`ISheetRenderService`** — renderizza un `Sheet` in immagine per l'anteprima, con la stessa
  matematica di layout del `PdfComposer` (così l'anteprima è fedele alla stampa). Cache LRU in
  memoria + su disco (cartella temp); rendering asincrono, mai sul thread UI.
- **`IPrintService`** — `PrintServiceFactory` sceglie l'implementazione per piattaforma:
  - Linux → `LinuxCupsPrintService`: `lpstat -e` per l'elenco, `lp` per l'invio; i processi
    passano per `IProcessRunner` (mockabile).
  - Windows → `WindowsPrintService`: `PrinterSettings.InstalledPrinters`, stampa GDI via
    `PrintDocument` renderizzando le pagine con PDFtoImage. Codice sotto
    `OperatingSystem.IsWindows()` e nel TFM `net8.0-windows`.
- **`IUndoRedoStack`** — ogni operazione è un `IUndoableAction` (undo/redo completo):
  `AddFilePagesAction`, `MoveSheetAction`, `DeleteSheetsAction`, `RotateSheetAction`
  (tutti i placement del foglio, o uno solo), `MergeSheetsAction` (unisci i fogli selezionati
  in uno), `NUpAction` (2/4 pagine per foglio, ri-suddivide la selezione in più fogli N-up),
  `SetNUpOrderAction` (ordine di riempimento righe/colonne), `InsertBlankSheetAction`,
  `SetSheetFitModeAction`, `SetDefaultFitModeAction`, `SetPaperAction`.

## Convenzioni

- Codice, nomi e commenti in **inglese**; documentazione utente e `CLAUDE.md` in **italiano**.
- `Nullable` abilitato, `LangVersion` latest, warning trattati come errori.
- MVVM: nessuna logica PDF nei code-behind delle View; la logica sta in `FinePdf.Core`,
  l'orchestrazione nei ViewModel.
- I test non contengono PDF binari: generano i campioni con PDFsharp in fixture condivise.
- Sollevare eccezioni tipizzate da `FinePdf.Core` (`PdfLoadException`, `PrintException`); la UI
  le traduce in dialoghi.

## Limitazioni note (v1)

- La ricomposizione con PdfSharp (`XPdfForm` su fogli nuovi) può perdere **campi form,
  annotazioni, segnalibri e link** interni. Accettabile per la v1: da segnalare nella UI.
- PDF protetti da password: richiesta password all'apertura; se non supportato, errore chiaro.
- Il contenuto sui fogli mantiene il vettoriale (`XPdfForm`, nessuna rasterizzazione);
  l'anteprima è raster, solo per visualizzazione.
- Formato/orientazione di stampa **unico per tutto il documento** in v1 (override per pagina: dopo).

## Stato di avanzamento

Piano completo e milestone: `~/.claude/plans/ciao-questo-e-un-warm-hejlsberg.md`.

- [x] M0 — Scaffolding solution, `Directory.Build.props`, `.editorconfig`, `git init` — build + test verdi
- [x] M1 — Modello (`Composition`/`Sheet`/`Placement`/`SourceDocument`), `FolderSourceService`, `PdfSharpDocumentService`, `PdfComposer` (fit + 1/2/4-up), `UndoRedoStack` + `AddFilePages`/`MoveSheet`/`DeleteSheets`
- [x] M2 — Operazioni `RotateSheet`, `MergeSheets`, `InsertBlankSheet`, `SetSheetFitMode`, `SetDefaultFitMode`, `SetPaper` + undo/redo — 45 test verdi in totale
- [x] M3 — Shell UI Avalonia a due pannelli: elenco sorgenti (watch cartella, doppio click) + anteprima di stampa (`SkiaSheetRenderService`, menu contestuale, toolbar formato/fit, undo/redo, avviso modifiche non salvate alla chiusura). Drag&drop rimandato a M4.
- [x] M4 — Rifinitura anteprima: N-up 2/4 con ordine righe/colonne (`NUpAction`, `SetNUpOrderAction`, `Sheet.NUpOrder`, `NUpOrder`), `FitMode` per foglio nel menu contestuale, comandi selezione (tutti/nessuno), scorciatoie da tastiera, drag & drop riordino fogli — 53 test verdi
- [x] M5 — Stampa: `IProcessRunner`/`ProcessRunner`, `IPrintService` + `PrintServiceFactory`, `LinuxCupsPrintService` (`lpstat -e`/`lp`, testato con `FakeProcessRunner`), `WindowsPrintService` GDI (`net8.0-windows`, non compilato su Linux), `PrintException`, `PageRange`/`PrintOptions`/`DuplexMode`, dialog di stampa interno (stampante, copie, intervallo, fronte/retro) + toolbar/Ctrl+P — 70 test verdi
- [ ] M6 — Rifinitura + packaging
