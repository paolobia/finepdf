# FinePdf

Editor PDF **desktop cross-platform (Windows + Linux)** in C# / .NET 8: unione, riordino,
cancellazione, imposizione "due pagine in una" (N-up), rotazione e stampa delle pagine.

## Requisiti di sviluppo

- .NET SDK 8.0+
- Template Avalonia (`dotnet new install Avalonia.Templates`) solo per rigenerare scaffold

## Avvio rapido

```bash
dotnet build FinePdf.sln
dotnet test
dotnet run --project src/FinePdf.App
```

## Struttura

| Progetto | Ruolo |
|---|---|
| `src/FinePdf.Core` | logica PDF senza dipendenze UI (modelli, servizi, operazioni undoable) |
| `src/FinePdf.App` | applicazione Avalonia (MVVM) |
| `tests/FinePdf.Core.Tests` | unit test xUnit |

Dettagli architetturali e convenzioni: [`CLAUDE.md`](CLAUDE.md).
Piano di lavoro e milestone: `~/.claude/plans/ciao-questo-e-un-warm-hejlsberg.md`.

## Pubblicazione

```bash
dotnet publish src/FinePdf.App -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
dotnet publish src/FinePdf.App -c Release -r win-x64   --self-contained -p:PublishSingleFile=true
```
