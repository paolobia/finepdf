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
        IFileDialogService dialogs)
    {
        State = state;
        _outputFolder = suggestedFolder;
        _dialogs = dialogs;
    }

    public PrinterInstallState State { get; }

    public bool IsInstalled => State == PrinterInstallState.Installed;

    public bool CanInstall => !IsInstalled && Path.IsPathRooted(OutputFolder);

    public string StateText => IsInstalled
        ? "Stampante «FinePdf» installata."
        : "Stampante «FinePdf» non installata.";

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
