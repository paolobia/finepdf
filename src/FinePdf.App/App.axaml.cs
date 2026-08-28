using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FinePdf.App.Services;
using FinePdf.App.ViewModels;
using FinePdf.App.Views;
using FinePdf.Core.Services;

namespace FinePdf.App;

public partial class App : Application
{
    private static readonly string[] WorkFolderExtensions =
        [".pdf", ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp"];

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            var dialogs = new FileDialogService(() => window);
            var composer = new PdfComposer();
            var processRunner = new ProcessRunner();
            var settings = new AppSettingsService();

            var viewModel = new MainWindowViewModel(
                new FolderSourceService(extensions: WorkFolderExtensions),
                new FolderSourceService(),
                new PdfSharpDocumentService(),
                new SkiaSheetRenderService(),
                composer,
                dialogs,
                PrintServiceFactory.Create(processRunner, composer),
                new PrintDialogService(() => window),
                new DocumentImportService(),
                settings,
                PrinterManagerFactory.Create(processRunner),
                new PrinterSetupDialogService(() => window, dialogs));

            window.DataContext = viewModel;
            window.Closed += (_, _) => viewModel.Dispose();
            viewModel.Initialize(settings.LastFolder ?? Directory.GetCurrentDirectory());

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
