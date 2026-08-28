using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinePdf.App.Services;
using FinePdf.Core.Services;

namespace FinePdf.App.ViewModels;

public enum PrinterSetupAction
{
    None,
    Install,
    Uninstall,
}

/// <summary>Outcome of the "Stampante virtuale" dialog.</summary>
public sealed record PrinterSetupResult(PrinterSetupAction Action, string Folder);

/// <summary>Backs the small install/remove dialog for the "FinePdf" virtual printer.</summary>
public sealed partial class PrinterSetupDialogViewModel : ViewModelBase
{
    private readonly IFileDialogService _dialogs;

    [ObservableProperty]
    private string _outputFolder;

    public PrinterSetupDialogViewModel(
        PrinterInstallState state,
        string suggestedFolder,
        IFileDialogService dialogs,
        bool advisory = false)
    {
        State = state;
        _outputFolder = suggestedFolder;
        _dialogs = dialogs;
        Advisory = advisory;
    }

    public PrinterInstallState State { get; }

    /// <summary>Windows: FinePdf installs nothing, it only points at a third-party printer's folder.</summary>
    public bool Advisory { get; }

    public bool IsInstalled => State == PrinterInstallState.Installed;

    public bool CanInstall => !IsInstalled && Path.IsPathRooted(OutputFolder);

    public string ConfirmLabel => Advisory ? "Usa questa cartella" : "Installa";

    public string StateText => Advisory
        ? "Cartella della stampante PDF"
        : IsInstalled
            ? "Stampante «FinePdf» installata."
            : "Stampante «FinePdf» non installata.";

    public string Explanation => Advisory
        ? "Su Windows FinePdf non installa una stampante. Installa una stampante PDF gratuita " +
          "(es. Xodo PDF Printer, Documalis Free PDF Printer), impostala per salvare senza " +
          "chiedere il nome file nella cartella qui sotto: FinePdf la osserverà e la mostrerà " +
          "nella scheda «PDF da stampante». Dettagli: docs/virtual-printer-windows.md"
        : "Stampando verso «FinePdf» da qualsiasi applicazione, il PDF viene salvato nella " +
          "cartella qui sotto. Serve un'autorizzazione (password) una volta sola.";

    public PrinterSetupResult Result { get; private set; } = new(PrinterSetupAction.None, string.Empty);

    [RelayCommand]
    private async Task BrowseAsync()
    {
        string? picked = await _dialogs.PickFolderAsync(OutputFolder);
        if (picked is not null)
        {
            OutputFolder = picked;
        }
    }

    public void ConfirmInstall() => Result = new PrinterSetupResult(PrinterSetupAction.Install, OutputFolder);

    public void ConfirmUninstall() => Result = new PrinterSetupResult(PrinterSetupAction.Uninstall, OutputFolder);

    partial void OnOutputFolderChanged(string value) => OnPropertyChanged(nameof(CanInstall));
}
