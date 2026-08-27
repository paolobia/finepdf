using CommunityToolkit.Mvvm.ComponentModel;

namespace FinePdf.App.ViewModels;

/// <summary>One PDF in the left-hand "sources" list. Page count is filled in asynchronously.</summary>
public partial class SourceFileViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _subtitle = "…";

    public SourceFileViewModel(string fullPath)
    {
        FullPath = fullPath;
        FileName = Path.GetFileName(fullPath);
    }

    public string FullPath { get; }

    public string FileName { get; }
}
