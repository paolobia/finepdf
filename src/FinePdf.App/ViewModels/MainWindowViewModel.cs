using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinePdf.App.Services;
using FinePdf.Core;
using FinePdf.Core.Models;
using FinePdf.Core.Operations;
using FinePdf.Core.Operations.Actions;
using FinePdf.Core.Services;

namespace FinePdf.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const int PreviewPixelWidth = 640;

    private readonly IFolderSourceService _folder;
    private readonly IPdfDocumentService _documents;
    private readonly ISheetRenderService _renderer;
    private readonly IPdfComposer _composer;
    private readonly IFileDialogService _dialogs;

    private readonly Composition _composition = new();
    private readonly UndoRedoStack _undo = new();
    private readonly ConcurrentDictionary<string, SourceDocument> _loaded = new(StringComparer.Ordinal);
    private readonly Dictionary<Sheet, SheetViewModel> _sheetViewModels = [];
    private readonly SemaphoreSlim _renderGate = new(3);

    private bool _suppressSettingApply;
    private string? _lastSavePath;
    private NUpOrder _nUpOrder = NUpOrder.RowMajor;

    [ObservableProperty]
    private string _folderPath = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private PaperSize _selectedPaperSize = PaperSize.A4;

    [ObservableProperty]
    private PageOrientation _selectedOrientation = PageOrientation.Portrait;

    [ObservableProperty]
    private FitMode _selectedFitMode = FitMode.Fit;

    public MainWindowViewModel(
        IFolderSourceService folder,
        IPdfDocumentService documents,
        ISheetRenderService renderer,
        IPdfComposer composer,
        IFileDialogService dialogs)
    {
        _folder = folder;
        _documents = documents;
        _renderer = renderer;
        _composer = composer;
        _dialogs = dialogs;

        _undo.Changed += (_, _) => RefreshCommandState();
        _folder.FilesChanged += OnFolderFilesChanged;
    }

    public ObservableCollection<SheetViewModel> Sheets { get; } = [];

    public ObservableCollection<SourceFileViewModel> SourceFiles { get; } = [];

    public IReadOnlyList<PaperSize> PaperSizes { get; } = PaperSize.Presets;

    public IReadOnlyList<PageOrientation> Orientations { get; } = [PageOrientation.Portrait, PageOrientation.Landscape];

    public IReadOnlyList<FitMode> FitModes { get; } = [FitMode.Fit, FitMode.ActualSize, FitMode.Fill];

    /// <summary>Test hook: when false, sheet previews are not rendered automatically.</summary>
    public bool EnableAutoRender { get; set; } = true;

    public bool CanUndo => _undo.CanUndo;

    public bool CanRedo => _undo.CanRedo;

    public string UndoLabel => _undo.CanUndo ? $"Annulla — {_undo.NextUndoDescription}" : "Annulla";

    public string RedoLabel => _undo.CanRedo ? $"Ripeti — {_undo.NextRedoDescription}" : "Ripeti";

    public void Initialize(string startFolder)
    {
        try
        {
            _folder.SetFolder(startFolder);
            FolderPath = _folder.FolderPath ?? startFolder;
            ReloadSourceFiles();
            SetStatus($"{SourceFiles.Count} PDF nella cartella.");
        }
        catch (Exception ex) when (ex is DirectoryNotFoundException or IOException or UnauthorizedAccessException)
        {
            SetStatus($"Impossibile aprire la cartella: {ex.Message}");
        }
    }

    public void AddSource(SourceFileViewModel? file)
    {
        if (file is null)
        {
            return;
        }

        try
        {
            SourceDocument document = _loaded.GetOrAdd(file.FullPath, _documents.Load);
            Apply(new AddFilePagesAction(_composition, document));
            SetStatus($"Aggiunte {document.PageCount} pagine da {document.FileName}.");
        }
        catch (PdfLoadException ex)
        {
            SetStatus(ex.IsPasswordProtected
                ? $"{file.FileName}: PDF protetto da password."
                : $"{file.FileName}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _folder.FilesChanged -= OnFolderFilesChanged;
        _folder.Dispose();
        _renderGate.Dispose();
        foreach (SheetViewModel vm in _sheetViewModels.Values)
        {
            vm.Preview?.Dispose();
        }
    }

    [RelayCommand]
    private async Task ChangeFolderAsync()
    {
        string? picked = await _dialogs.PickFolderAsync(FolderPath);
        if (picked is null)
        {
            return;
        }

        try
        {
            _folder.SetFolder(picked);
            FolderPath = _folder.FolderPath ?? picked;
            _loaded.Clear();
            _renderer.InvalidateCache();
            ReloadSourceFiles();
            SetStatus($"{SourceFiles.Count} PDF nella cartella.");
        }
        catch (Exception ex) when (ex is DirectoryNotFoundException or IOException or UnauthorizedAccessException)
        {
            SetStatus($"Impossibile aprire la cartella: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        _undo.Undo();
        AfterMutation();
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        _undo.Redo();
        AfterMutation();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RotateLeft() => RotateSelected(-90);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RotateRight() => RotateSelected(90);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RotateHalfTurn() => RotateSelected(180);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelected()
    {
        List<int> indices = SelectedIndices();
        if (indices.Count > 0)
        {
            Apply(new DeleteSheetsAction(_composition, indices));
        }
    }

    [RelayCommand(CanExecute = nameof(CanMerge))]
    private void MergeSelected()
    {
        List<int> indices = SelectedIndices();
        if (indices.Count >= 2)
        {
            Apply(new MergeSheetsAction(_composition, indices));
        }
    }

    [RelayCommand(CanExecute = nameof(CanMerge))]
    private void NUp2() => ApplyNUp(2);

    [RelayCommand(CanExecute = nameof(CanMerge))]
    private void NUp4() => ApplyNUp(4);

    private void ApplyNUp(int perSheet)
    {
        List<int> indices = SelectedIndices();
        if (indices.Count >= 2)
        {
            Apply(new NUpAction(_composition, indices, perSheet, _nUpOrder));
        }
    }

    [RelayCommand(CanExecute = nameof(HasNUpSelection))]
    private void NUpOrderByRows() => SetNUpOrder(NUpOrder.RowMajor);

    [RelayCommand(CanExecute = nameof(HasNUpSelection))]
    private void NUpOrderByColumns() => SetNUpOrder(NUpOrder.ColumnMajor);

    private void SetNUpOrder(NUpOrder order)
    {
        _nUpOrder = order;
        List<Sheet> targets = SelectedIndices()
            .Select(i => _composition.Sheets[i])
            .Where(s => s.Placements.Count > 1)
            .ToList();

        if (targets.Count > 0)
        {
            Apply(new SetNUpOrderAction(targets, order));
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void SheetFitDefault() => SetSheetFit(null);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void SheetFitFit() => SetSheetFit(FitMode.Fit);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void SheetFitActualSize() => SetSheetFit(FitMode.ActualSize);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void SheetFitFill() => SetSheetFit(FitMode.Fill);

    private void SetSheetFit(FitMode? mode)
    {
        List<Sheet> targets = SelectedIndices().Select(i => _composition.Sheets[i]).ToList();
        if (targets.Count > 0)
        {
            Apply(new SetSheetFitModeAction(targets, mode));
        }
    }

    [RelayCommand(CanExecute = nameof(HasSheets))]
    private void SelectAllSheets()
    {
        foreach (SheetViewModel sheet in Sheets)
        {
            sheet.IsSelected = true;
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void ClearSelection()
    {
        foreach (SheetViewModel sheet in Sheets)
        {
            sheet.IsSelected = false;
        }
    }

    /// <summary>Drag &amp; drop reorder from the preview: move the sheet at <paramref name="fromIndex"/> to <paramref name="toIndex"/>.</summary>
    public void MoveSheetByDrag(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _composition.Sheets.Count ||
            toIndex < 0 || toIndex >= _composition.Sheets.Count ||
            fromIndex == toIndex)
        {
            return;
        }

        Apply(new MoveSheetAction(_composition, fromIndex, toIndex));
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        int index = SingleSelectedIndex();
        if (index > 0)
        {
            Apply(new MoveSheetAction(_composition, index, index - 1));
        }
    }

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        int index = SingleSelectedIndex();
        if (index >= 0 && index < _composition.Sheets.Count - 1)
        {
            Apply(new MoveSheetAction(_composition, index, index + 1));
        }
    }

    [RelayCommand]
    private void InsertBlank()
    {
        List<int> indices = SelectedIndices();
        int at = indices.Count > 0 ? indices[^1] + 1 : _composition.Sheets.Count;
        Apply(new InsertBlankSheetAction(_composition, at));
    }

    [RelayCommand(CanExecute = nameof(HasSheets))]
    private async Task SaveAsync()
    {
        if (_lastSavePath is null)
        {
            await PickAndSaveAsync();
            return;
        }

        SaveTo(_lastSavePath);
    }

    [RelayCommand(CanExecute = nameof(HasSheets))]
    private Task SaveAsAsync() => PickAndSaveAsync();

    private async Task PickAndSaveAsync()
    {
        string? path = await _dialogs.PickSavePdfAsync("documento.pdf", FolderPath);
        if (path is not null)
        {
            SaveTo(path);
        }
    }

    private void SaveTo(string path)
    {
        try
        {
            _composer.Compose(_composition, path);
            _lastSavePath = path;
            HasUnsavedChanges = false;
            SetStatus($"Salvato: {path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            SetStatus($"Salvataggio non riuscito: {ex.Message}");
        }
    }

    private void RotateSelected(int deltaDegrees)
    {
        List<IUndoableAction> rotations = SelectedIndices()
            .Select(i => _composition.Sheets[i])
            .Where(s => !s.IsBlank)
            .Select(s => (IUndoableAction)new RotateSheetAction(s, deltaDegrees))
            .ToList();

        if (rotations.Count == 0)
        {
            return;
        }

        Apply(rotations.Count == 1
            ? rotations[0]
            : new CompositeAction("Ruota le pagine selezionate", rotations));
    }

    private void Apply(IUndoableAction action)
    {
        _undo.Execute(action);
        AfterMutation();
    }

    private void AfterMutation()
    {
        HasUnsavedChanges = true;
        SyncSettingsFromComposition();
        SyncSheets();
        RefreshCommandState();
        OnPropertyChanged(nameof(SelectionSummary));
        if (EnableAutoRender)
        {
            _ = RenderAllAsync();
        }
    }

    private void SyncSheets()
    {
        var present = new HashSet<Sheet>(_composition.Sheets);
        foreach (Sheet gone in _sheetViewModels.Keys.Where(k => !present.Contains(k)).ToList())
        {
            _sheetViewModels[gone].SelectionChanged -= OnSheetSelectionChanged;
            _sheetViewModels[gone].Preview?.Dispose();
            _sheetViewModels.Remove(gone);
        }

        for (int i = 0; i < _composition.Sheets.Count; i++)
        {
            Sheet sheet = _composition.Sheets[i];
            if (!_sheetViewModels.TryGetValue(sheet, out SheetViewModel? vm))
            {
                vm = new SheetViewModel(sheet, (s, w) => _renderer.RenderSheetPng(_composition, s, w));
                vm.SelectionChanged += OnSheetSelectionChanged;
                _sheetViewModels[sheet] = vm;
            }

            if (i >= Sheets.Count)
            {
                Sheets.Add(vm);
            }
            else if (!ReferenceEquals(Sheets[i], vm))
            {
                int existing = Sheets.IndexOf(vm);
                if (existing >= 0)
                {
                    Sheets.Move(existing, i);
                }
                else
                {
                    Sheets.Insert(i, vm);
                }
            }

            vm.UpdateLabels(i + 1);
        }

        while (Sheets.Count > _composition.Sheets.Count)
        {
            Sheets.RemoveAt(Sheets.Count - 1);
        }
    }

    private async Task RenderAllAsync()
    {
        SheetViewModel[] snapshot = [.. Sheets];
        await Task.WhenAll(snapshot.Select(RenderOneAsync));
    }

    private async Task RenderOneAsync(SheetViewModel vm)
    {
        await _renderGate.WaitAsync();
        try
        {
            await vm.RenderAsync(PreviewPixelWidth);
        }
        finally
        {
            _renderGate.Release();
        }
    }

    private void ReloadSourceFiles()
    {
        SourceFiles.Clear();
        foreach (string path in _folder.Files)
        {
            SourceFiles.Add(new SourceFileViewModel(path));
        }

        _ = FillPageCountsAsync([.. SourceFiles]);
    }

    private async Task FillPageCountsAsync(IReadOnlyList<SourceFileViewModel> files)
    {
        foreach (SourceFileViewModel file in files)
        {
            try
            {
                SourceDocument document = await Task.Run(() => _loaded.GetOrAdd(file.FullPath, _documents.Load));
                file.Subtitle = document.PageCount == 1 ? "1 pagina" : $"{document.PageCount} pagine";
            }
            catch (PdfLoadException ex)
            {
                file.Subtitle = ex.IsPasswordProtected ? "protetto" : "non leggibile";
            }
        }
    }

    private void OnFolderFilesChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _renderer.InvalidateCache();
            foreach (string path in _loaded.Keys.Where(p => !File.Exists(p)).ToList())
            {
                _loaded.TryRemove(path, out _);
            }

            ReloadSourceFiles();
            if (EnableAutoRender)
            {
                _ = RenderAllAsync();
            }
        });
    }

    public string SelectionSummary
    {
        get
        {
            int count = Sheets.Count(s => s.IsSelected);
            return count == 0 ? $"{Sheets.Count} fogli" : $"{count}/{Sheets.Count} fogli selezionati";
        }
    }

    private void OnSheetSelectionChanged(object? sender, EventArgs e)
    {
        RefreshCommandState();
        OnPropertyChanged(nameof(SelectionSummary));
    }

    private void SyncSettingsFromComposition()
    {
        _suppressSettingApply = true;
        SelectedPaperSize = PaperSizes.FirstOrDefault(p => p.Name == _composition.PaperSize.Name) ?? _composition.PaperSize;
        SelectedOrientation = _composition.Orientation;
        SelectedFitMode = _composition.DefaultFitMode;
        _suppressSettingApply = false;
    }

    partial void OnSelectedPaperSizeChanged(PaperSize value) => ApplyPaper();

    partial void OnSelectedOrientationChanged(PageOrientation value) => ApplyPaper();

    partial void OnSelectedFitModeChanged(FitMode value)
    {
        if (_suppressSettingApply || _composition.DefaultFitMode == value)
        {
            return;
        }

        Apply(new SetDefaultFitModeAction(_composition, value));
    }

    private void ApplyPaper()
    {
        if (_suppressSettingApply)
        {
            return;
        }

        if (_composition.PaperSize.Name == SelectedPaperSize.Name && _composition.Orientation == SelectedOrientation)
        {
            return;
        }

        Apply(new SetPaperAction(_composition, SelectedPaperSize, SelectedOrientation));
    }

    private void RefreshCommandState()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(UndoLabel));
        OnPropertyChanged(nameof(RedoLabel));

        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        RotateLeftCommand.NotifyCanExecuteChanged();
        RotateRightCommand.NotifyCanExecuteChanged();
        RotateHalfTurnCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
        MergeSelectedCommand.NotifyCanExecuteChanged();
        NUp2Command.NotifyCanExecuteChanged();
        NUp4Command.NotifyCanExecuteChanged();
        NUpOrderByRowsCommand.NotifyCanExecuteChanged();
        NUpOrderByColumnsCommand.NotifyCanExecuteChanged();
        SheetFitDefaultCommand.NotifyCanExecuteChanged();
        SheetFitFitCommand.NotifyCanExecuteChanged();
        SheetFitActualSizeCommand.NotifyCanExecuteChanged();
        SheetFitFillCommand.NotifyCanExecuteChanged();
        SelectAllSheetsCommand.NotifyCanExecuteChanged();
        ClearSelectionCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        SaveAsCommand.NotifyCanExecuteChanged();
    }

    private void SetStatus(string text) => StatusText = text;

    private bool HasSheets() => _composition.Sheets.Count > 0;

    private bool HasSelection() => Sheets.Any(s => s.IsSelected);

    private bool CanMerge() => Sheets.Count(s => s.IsSelected) >= 2;

    private bool HasNUpSelection() =>
        SelectedIndices().Any(i => i < _composition.Sheets.Count && _composition.Sheets[i].Placements.Count > 1);

    private bool CanMoveUp() => SingleSelectedIndex() > 0;

    private bool CanMoveDown()
    {
        int index = SingleSelectedIndex();
        return index >= 0 && index < _composition.Sheets.Count - 1;
    }

    private int SingleSelectedIndex()
    {
        List<int> indices = SelectedIndices();
        return indices.Count == 1 ? indices[0] : -1;
    }

    private List<int> SelectedIndices()
    {
        var result = new List<int>();
        for (int i = 0; i < Sheets.Count; i++)
        {
            if (Sheets[i].IsSelected)
            {
                result.Add(i);
            }
        }

        return result;
    }
}
