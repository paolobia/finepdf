using System.Collections.Concurrent;
using System.Collections.ObjectModel;
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

    private readonly IPdfDocumentService _documents;
    private readonly ISheetRenderService _renderer;
    private readonly IPdfComposer _composer;
    private readonly IFileDialogService _dialogs;
    private readonly IPrintService _printService;
    private readonly IPrintDialogService _printDialogs;
    private readonly IDocumentImportService _import;
    private readonly IAppSettingsService _settings;
    private readonly IPrinterManager _printers;
    private readonly IPrinterSetupDialogService _printerDialog;

    private readonly Composition _composition = new();
    private readonly UndoRedoStack _undo = new();
    private readonly ConcurrentDictionary<string, SourceDocument> _loaded = new(StringComparer.Ordinal);
    private readonly Dictionary<Sheet, SheetViewModel> _sheetViewModels = [];
    private readonly SemaphoreSlim _renderGate = new(3);

    private bool _suppressSettingApply;
    private bool _limitationsDismissed;
    private string? _lastSavePath;
    private NUpOrder _nUpOrder = NUpOrder.RowMajor;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private bool _isLimitationsNoticeOpen;

    [ObservableProperty]
    private PaperSize _selectedPaperSize = PaperSize.A4;

    [ObservableProperty]
    private PageOrientation _selectedOrientation = PageOrientation.Portrait;

    [ObservableProperty]
    private FitMode _selectedFitMode = FitMode.Fit;

    [ObservableProperty]
    private double _selectedMarginMm;

    public MainWindowViewModel(
        IFolderSourceService workFolder,
        IFolderSourceService printerFolder,
        IPdfDocumentService documents,
        ISheetRenderService renderer,
        IPdfComposer composer,
        IFileDialogService dialogs,
        IPrintService printService,
        IPrintDialogService printDialogs,
        IDocumentImportService import,
        IAppSettingsService settings,
        IPrinterManager printers,
        IPrinterSetupDialogService printerDialog)
    {
        _documents = documents;
        _renderer = renderer;
        _composer = composer;
        _dialogs = dialogs;
        _printService = printService;
        _printDialogs = printDialogs;
        _import = import;
        _settings = settings;
        _printers = printers;
        _printerDialog = printerDialog;

        WorkPane = new FolderPaneViewModel(
            "Cartella di lavoro", canChangeFolder: true, workFolder, dialogs,
            DescribeFileAsync, onFolderChanged: _settings.SetLastFolder);
        PrinterPane = new FolderPaneViewModel(
            "PDF da stampante", canChangeFolder: false, printerFolder, dialogs, DescribeFileAsync)
        {
            EmptyHint = "Installa la stampante virtuale per scegliere una cartella.",
        };

        WorkPane.FileActivated += (_, file) => AddSource(file);
        PrinterPane.FileActivated += (_, file) => AddSource(file);
        WorkPane.FilesChanged += (_, _) => OnPaneFilesChanged();
        PrinterPane.FilesChanged += (_, _) => OnPaneFilesChanged();

        _undo.Changed += (_, _) => RefreshCommandState();
    }

    public FolderPaneViewModel WorkPane { get; }

    public FolderPaneViewModel PrinterPane { get; }

    public IReadOnlyList<FolderPaneViewModel> Panes => [WorkPane, PrinterPane];

    public ObservableCollection<SheetViewModel> Sheets { get; } = [];

    public IReadOnlyList<PaperSize> PaperSizes { get; } = PaperSize.Presets;

    public IReadOnlyList<PageOrientation> Orientations { get; } = [PageOrientation.Portrait, PageOrientation.Landscape];

    public IReadOnlyList<FitMode> FitModes { get; } = [FitMode.Fit, FitMode.ActualSize, FitMode.Fill];

    public bool IsPrinterManagerSupported => _printers.IsSupported;

    /// <summary>Test hook: when false, sheet previews are not rendered automatically.</summary>
    public bool EnableAutoRender { get; set; } = true;

    public bool CanUndo => _undo.CanUndo;

    public bool CanRedo => _undo.CanRedo;

    public string UndoLabel => _undo.CanUndo ? $"Annulla — {_undo.NextUndoDescription}" : "Annulla";

    public string RedoLabel => _undo.CanRedo ? $"Ripeti — {_undo.NextRedoDescription}" : "Ripeti";

    public void Initialize(string workFolder)
    {
        if (WorkPane.TrySetFolder(workFolder))
        {
            _settings.SetLastFolder(WorkPane.FolderPath);
            SetStatus($"{WorkPane.Files.Count} file nella cartella di lavoro.");
        }
        else
        {
            SetStatus("Impossibile aprire la cartella di lavoro.");
        }

        PrinterPane.TrySetFolder(_settings.PrinterOutputFolder);
    }

    public void AddSource(SourceFileViewModel? file)
    {
        if (file is null)
        {
            return;
        }

        string pdfPath;
        try
        {
            pdfPath = _import.EnsurePdf(file.FullPath);
        }
        catch (DocumentImportException ex)
        {
            SetStatus($"{file.FileName}: {ex.Message}");
            return;
        }

        try
        {
            SourceDocument document = _loaded.GetOrAdd(file.FullPath, _ => _documents.Load(pdfPath));
            Apply(new AddFilePagesAction(_composition, document));
            SetStatus($"Aggiunte {document.PageCount} pagine da {file.FileName}.");
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
        WorkPane.Dispose();
        PrinterPane.Dispose();
        _import.CleanupSession();
        _renderGate.Dispose();
        foreach (SheetViewModel vm in _sheetViewModels.Values)
        {
            vm.Preview?.Dispose();
        }
    }

    [RelayCommand(CanExecute = nameof(IsPrinterManagerSupported))]
    private async Task VirtualPrinterAsync()
    {
        PrinterInstallState state;
        try
        {
            state = await Task.Run(_printers.GetState);
        }
        catch (PrinterManagerException)
        {
            state = PrinterInstallState.Unsupported;
        }

        string suggested = _settings.PrinterOutputFolder
            ?? (PrinterPane.FolderPath.Length > 0 ? PrinterPane.FolderPath : DefaultPrinterFolder());

        PrinterSetupResult? result = await _printerDialog.RequestAsync(state, suggested);
        if (result is null || result.Action == PrinterSetupAction.None)
        {
            return;
        }

        try
        {
            if (result.Action == PrinterSetupAction.Uninstall)
            {
                await Task.Run(_printers.Uninstall);
                SetStatus("Stampante «FinePdf» rimossa.");
            }
            else
            {
                SetStatus("Installazione stampante «FinePdf»… (potrebbe chiedere la password)");
                await Task.Run(() => _printers.Install(result.Folder));
                _settings.SetPrinterOutputFolder(result.Folder);
                PrinterPane.TrySetFolder(result.Folder);
                SetStatus($"Stampante «FinePdf» pronta → {result.Folder}");
            }
        }
        catch (PrinterManagerException ex)
        {
            SetStatus(ex.Fault switch
            {
                PrinterManagerFault.PkexecMissing => "Manca pkexec (polkit): installalo e riprova.",
                PrinterManagerFault.PermissionDenied => "Autorizzazione negata o annullata.",
                PrinterManagerFault.SnapCupsUnsupported => "CUPS installato come snap: non supportato.",
                PrinterManagerFault.GhostscriptMissing => "Manca ghostscript: «sudo apt install ghostscript».",
                PrinterManagerFault.CupsUnavailable => "CUPS non disponibile: «sudo apt install cups cups-client».",
                PrinterManagerFault.Unsupported => "Funzione disponibile solo su Linux.",
                _ => $"Operazione non riuscita: {ex.Message}",
            });
        }
    }

    private static string DefaultPrinterFolder() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify),
            "FinePdf");

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

    [RelayCommand(CanExecute = nameof(HasContentSelection))]
    private void FitOrientation()
    {
        List<Sheet> targets = SelectedIndices()
            .Select(i => _composition.Sheets[i])
            .Where(s => !s.IsBlank)
            .ToList();

        if (targets.Count > 0)
        {
            Apply(new FitOrientationAction(_composition, targets));
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
    private void DismissLimitations()
    {
        _limitationsDismissed = true;
        IsLimitationsNoticeOpen = false;
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

    [RelayCommand(CanExecute = nameof(HasSheets))]
    private async Task PrintAsync()
    {
        IReadOnlyList<PrinterInfo> printers;
        try
        {
            printers = await Task.Run(_printService.GetPrinters);
        }
        catch (PrintException ex)
        {
            SetStatus($"Stampa non disponibile: {ex.Message}");
            return;
        }

        if (printers.Count == 0)
        {
            SetStatus("Nessuna stampante trovata.");
            return;
        }

        PrintOptions? options = await _printDialogs.RequestAsync(printers);
        if (options is null)
        {
            return;
        }

        try
        {
            SetStatus($"Invio a «{options.PrinterName}»…");
            await Task.Run(() => _printService.Print(_composition, options));
            SetStatus($"Inviato a «{options.PrinterName}» ({options.Copies} copia/e).");
        }
        catch (PrintException ex)
        {
            SetStatus($"Stampa non riuscita: {ex.Message}");
        }
    }

    private async Task PickAndSaveAsync()
    {
        string? path = await _dialogs.PickSavePdfAsync("documento.pdf", WorkPane.FolderPath);
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

        if (!_limitationsDismissed && _composition.Sheets.Count > 0)
        {
            IsLimitationsNoticeOpen = true;
        }

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

    private async Task<string> DescribeFileAsync(string path)
    {
        try
        {
            SourceDocument document = await Task.Run(() =>
                _loaded.GetOrAdd(path, p => _documents.Load(_import.EnsurePdf(p))));
            return document.PageCount == 1 ? "1 pagina" : $"{document.PageCount} pagine";
        }
        catch (PdfLoadException ex)
        {
            return ex.IsPasswordProtected ? "protetto" : "non leggibile";
        }
        catch (DocumentImportException)
        {
            return "non convertibile";
        }
    }

    private void OnPaneFilesChanged()
    {
        _renderer.InvalidateCache();
        foreach (string path in _loaded.Keys.Where(p => !File.Exists(p)).ToList())
        {
            _loaded.TryRemove(path, out _);
        }

        if (EnableAutoRender)
        {
            _ = RenderAllAsync();
        }
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
        SelectedMarginMm = Math.Round(_composition.Margins.MaxEdgeMillimeters, 1);
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

    partial void OnSelectedMarginMmChanged(double value)
    {
        if (_suppressSettingApply)
        {
            return;
        }

        PageMargins target = PageMargins.FromMillimeters(Math.Max(0, value));
        if (MarginsRoughlyEqual(_composition.Margins, target))
        {
            return;
        }

        Apply(new SetMarginsAction(_composition, target));
    }

    private static bool MarginsRoughlyEqual(PageMargins a, PageMargins b) =>
        Math.Abs(a.Left - b.Left) < 0.5 && Math.Abs(a.Top - b.Top) < 0.5 &&
        Math.Abs(a.Right - b.Right) < 0.5 && Math.Abs(a.Bottom - b.Bottom) < 0.5;

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
        FitOrientationCommand.NotifyCanExecuteChanged();
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
        PrintCommand.NotifyCanExecuteChanged();
        VirtualPrinterCommand.NotifyCanExecuteChanged();
    }

    private void SetStatus(string text) => StatusText = text;

    private bool HasSheets() => _composition.Sheets.Count > 0;

    private bool HasSelection() => Sheets.Any(s => s.IsSelected);

    private bool CanMerge() => Sheets.Count(s => s.IsSelected) >= 2;

    private bool HasNUpSelection() =>
        SelectedIndices().Any(i => i < _composition.Sheets.Count && _composition.Sheets[i].Placements.Count > 1);

    private bool HasContentSelection() =>
        SelectedIndices().Any(i => i < _composition.Sheets.Count && !_composition.Sheets[i].IsBlank);

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
