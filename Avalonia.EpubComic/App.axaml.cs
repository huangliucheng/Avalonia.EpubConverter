using System.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.EpubComic.Services;
using Avalonia.EpubComic.ViewModels;
using Avalonia.EpubComic.Views;
using Avalonia.Markup.Xaml;

namespace Avalonia.EpubComic;

public class App : Application
{
    public static IDialogService DialogService { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // 创建主窗口
            var mainWindow = new MainWindow();

            // 创建 DialogService
            DialogService = new DialogService(mainWindow);

            // 创建主视图模型并传入对话框服务
            var mainViewModel = new MainWindowViewModel();

            // 设置主窗口的上下文
            mainWindow.DataContext = mainViewModel;

            // 设置主窗口
            desktop.MainWindow = mainWindow;

            desktop.Exit += async (_, _) =>
            {
                if (mainViewModel.SettingVM?.Setting != null)
                    await mainViewModel.SettingVM.Setting.SaveSettingsAsync().ConfigureAwait(false);
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove) BindingPlugins.DataValidators.Remove(plugin);
    }
}