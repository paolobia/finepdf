using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinePdf.App.Services;
using FinePdf.Core.Services;

namespace FinePdf.App.ViewModels;

/// <summary>
/// One tab of the left-hand "Sorgenti" panel: a single watched folder, its file list, and
/// (optionally) a "Cambia cartella" command. Double-clicking a file raises
/// <see cref="FileActivated"/>; <see cref="MainWindowViewModel"/> turns that into pages.
/// </summary>
public sealed partial class FolderPaneViewModel : ViewModelBase, IDisposable
{
    private readonly IFolderSourceService _source;
    private readonly IFileDialogService _dialogs;
    private readonly Func<string, Task<string>> _describe;
    private readonly Action<string>? _onFolderChanged;
    private readonly Action<bool>? _onTrashAfterAddChanged;
    private readonly HashSet<string> _hiddenPaths = new(StringComparer.Ordinal);

    [ObservableProperty]
    private string _folderPath = string.Empty;

    [ObservableProperty]
    private string _emptyHint = string.Empty;

    [ObservableProperty]
    private bool _trashAfterAdd;

    public FolderPaneViewModel(
        string title,
        bool canChangeFolder,
        IFolderSourceService source,
        IFileDialogService dialogs,
        Func<string, Task<string>> describe,
        Action<string>? onFolderChanged = null,
        bool canTrashAfterAdd = false,
        bool initialTrashAfterAdd = false,
        Action<bool>? onTrashAfterAddChanged = null)
    {
        Title = title;
        CanChangeFolder = canChangeFolder;
        CanTrashAfterAdd = canTrashAfterAdd;
        _trashAfterAdd = initialTrashAfterAdd;
        _source = source;
        _dialogs = dialogs;
        _describe = describe;
        _onFolderChanged = onFolderChanged;
        _onTrashAfterAddChanged = onTrashAfterAddChanged;
        _source.FilesChanged += OnSourceFilesChanged;
    }

    public string Title { get; }

    public bool CanChangeFolder { get; }

    /// <summary>Whether this pane offers the "trash the PDF after adding it" option.</summary>
    public bool CanTrashAfterAdd { get; }

    public ObservableCollection<SourceFileViewModel> Files { get; } = [];

    /// <summary>Raised when the folder's file set changes on disk (after <see cref="Reload"/>).</summary>
    public event EventHandler? FilesChanged;

    /// <summary>Raised when the user double-clicks a file in this pane.</summary>
    public event EventHandler<SourceFileViewModel>? FileActivated;

    public void RaiseFileActivated(SourceFileViewModel file) => FileActivated?.Invoke(this, file);

    /// <summary>Points the pane at <paramref name="path"/>; silently no-ops on a bad path.</summary>
    public bool TrySetFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            EmptyHint = "Nessuna cartella impostata.";
            return false;
        }

        try
        {
            _source.SetFolder(path);
            FolderPath = _source.FolderPath ?? path;
            EmptyHint = string.Empty;
            Reload();
            _onFolderChanged?.Invoke(FolderPath);
            return true;
        }
        catch (Exception ex) when (ex is DirectoryNotFoundException or IOException or UnauthorizedAccessException)
        {
            EmptyHint = $"Cartella non accessibile: {ex.Message}";
            return false;
        }
    }

    public void Reload()
    {
        Files.Clear();
        foreach (string path in _source.Files)
        {
            if (_hiddenPaths.Contains(path))
            {
                continue;
            }

            Files.Add(new SourceFileViewModel(path));
        }

        _ = FillSubtitlesAsync([.. Files]);
    }

    /// <summary>
    /// Drop a file from the list without touching the disk. Used after a "PDF da stampante" file
    /// has been consumed into the composition: it stays on disk for the whole session (so pages
    /// can still be re-read) and is moved to the recycle bin only when the app closes.
    /// </summary>
    public void Hide(string fullPath)
    {
        _hiddenPaths.Add(fullPath);
        for (int i = Files.Count - 1; i >= 0; i--)
        {
            if (string.Equals(Files[i].FullPath, fullPath, StringComparison.Ordinal))
            {
                Files.RemoveAt(i);
            }
        }
    }

    public void Dispose()
    {
        _source.FilesChanged -= OnSourceFilesChanged;
        _source.Dispose();
    }

    [RelayCommand]
    private async Task ChangeFolderAsync()
    {
        if (!CanChangeFolder)
        {
            return;
        }

        string? picked = await _dialogs.PickFolderAsync(FolderPath);
        if (picked is not null)
        {
            TrySetFolder(picked);
        }
    }

    private async Task FillSubtitlesAsync(IReadOnlyList<SourceFileViewModel> files)
    {
        foreach (SourceFileViewModel file in files)
        {
            file.Subtitle = await _describe(file.FullPath);
        }
    }

    partial void OnTrashAfterAddChanged(bool value) => _onTrashAfterAddChanged?.Invoke(value);

    private void OnSourceFilesChanged(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() =>
        {
            Reload();
            FilesChanged?.Invoke(this, EventArgs.Empty);
        });
}
