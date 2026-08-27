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
            var printService = PrintServiceFactory.Create(new ProcessRunner(), composer);
            var printDialogs = new PrintDialogService(() => window);

            var viewModel = new MainWindowViewModel(
                new FolderSourceService(),
                new PdfSharpDocumentService(),
                new SkiaSheetRenderService(),
                composer,
                dialogs,
                printService,
                printDialogs);

            window.DataContext = viewModel;
            window.Closed += (_, _) => viewModel.Dispose();
            viewModel.Initialize(Directory.GetCurrentDirectory());

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
