# FinePdf

[🇮🇹 Italiano](README.md) · 🇬🇧 English

**Cross-platform desktop PDF editor (Windows + Linux)** in C# / .NET 8: merge, reorder,
delete, "N pages on one sheet" (N-up) imposition, rotation and printing.

## Features

- **Merge**: double-click a PDF in the sources list → appends all of its pages.
- **Reorder**: drag & drop in the preview, *Move up/down* buttons, `Alt+↑/↓`.
- **Delete**: single or multiple selection, context menu or `Del`.
- **N-up**: select several sheets → *Two / Four pages per sheet*; fill order by row or by
  column; each page is auto-rotated 90° when that fills its cell better. Or *Merge onto one
  sheet*.
- **Rotate**: 90° CW / CCW / 180° (`Ctrl+←/→`).
- **Blank page**: from the context menu, inherits the document's size and orientation.
- **Fit** (`FitMode`): document default + per-sheet override (fit / actual size / fill).
- **Print**: the right pane *is* the print preview; an internal dialog covers printer, copies,
  range (`1-3, 5`) and one/two-sided. Linux via CUPS (`lp`/`lpstat`), Windows via GDI.
- Full **undo/redo**; *unsaved changes* prompt on close.

## Quick start

```bash
dotnet build FinePdf.sln
dotnet test
dotnet run --project src/FinePdf.App
```

Requires **.NET SDK 8.0.x**. On Linux/macOS the projects are single-target `net8.0`; on Windows
`FinePdf.App` switches to `net8.0-windows` for GDI printing (`WindowsPrintService`).

## Layout

| Project | Role |
|---|---|
| `src/FinePdf.Core` | PDF logic with no UI dependencies (models, services, undoable operations) |
| `src/FinePdf.App` | Avalonia application (MVVM) |
| `tests/FinePdf.Core.Tests` | xUnit unit tests (sample PDFs generated at runtime) |

Architecture, pinned stack and conventions: [`CLAUDE.md`](CLAUDE.md) (Italian).

## Publishing

```bash
./build/publish.sh linux-x64      # -> artifacts/linux-x64
./build/publish.ps1 -Rid win-x64  # on Windows -> artifacts/win-x64 (GDI printing)
```

Both run `dotnet test` first, then a self-contained single-file `dotnet publish`.
Prebuilt binaries are attached to the GitHub [Releases](../../releases).
