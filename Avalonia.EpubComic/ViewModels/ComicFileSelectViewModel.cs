using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.EpubComic.Models;
using Avalonia.EpubComic.Services;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.EpubComic.ViewModels;

public partial class ComicFileSelectViewModel(DispatchCenter dispatcher) : ViewModelBase
{
    private readonly IDialogService _dialogService = App.DialogService;
    private string? _comicInputPath;
    [ObservableProperty] private string? _comicSavePath;


    [ObservableProperty] private string? _comicTitle;
    [ObservableProperty] private bool _startButtonEnabled = true;

    public string? ComicInputPath
    {
        get => _comicInputPath;
        set
        {
            dispatcher.ClearBookList();
            if (string.IsNullOrEmpty(value)) return;
            if (_comicInputPath == value) return;
            ComicTitle = Path.GetFileName(Path.TrimEndingDirectorySeparator(value));
            dispatcher.GetBooksPath(value);
            dispatcher.CreateParseFactorAsync(ComicTitle).GetAwaiter();
            SetProperty(ref _comicInputPath, value);
            if (Path.GetDirectoryName(value) != null)
                ComicSavePath = Path.GetDirectoryName(value);
        }
    }

    [RelayCommand]
    private async Task SelectInputPath()
    {
        ComicInputPath = await FolderPickerAsync("选择漫画文件路径");
    }

    [RelayCommand]
    private async Task SelectSavePath()
    {
        ComicSavePath = await FolderPickerAsync("选择保存路径");
    }

    [RelayCommand]
    private void Replace()
    {
        if (string.IsNullOrEmpty(ComicTitle)) return;
        dispatcher.ReplaceBookTitle(ComicTitle);
    }

    [RelayCommand]
    private async Task TaskStart()
    {
        StartButtonEnabled = false;
        OnPropertyChanged(nameof(StartButtonEnabled));
        try
        {
            await dispatcher.BuildComicBookAsync(ComicInputPath, ComicSavePath);
            await _dialogService.ShowMessageBoxAsync("漫画书制作完毕!", "运行结果");
        }
        catch (OperationCanceledException)
        {
            await _dialogService.ShowMessageBoxAsync("任务已取消!", "运行结果");
        }
        catch (Exception e)
        {
            await _dialogService.ShowMessageBoxAsync("漫画制作出错: " + e.Message, "错误");
        }
        finally
        {
            ComicTitle = string.Empty;
            ComicSavePath = string.Empty;
            dispatcher.ClearBookList();
            StartButtonEnabled = true;
            OnPropertyChanged(nameof(StartButtonEnabled));
        }
    }

    [RelayCommand]
    private async Task TaskStop()
    {
        try
        {
            await dispatcher.CancelComicBookAsync();
        }
        catch (Exception e)
        {
            await _dialogService.ShowMessageBoxAsync("任务出错: " + e.Message, "错误");
        }

        StartButtonEnabled = true;
        OnPropertyChanged(nameof(StartButtonEnabled));
    }

    // 获得文件夹路径
    private static async Task<string?> FolderPickerAsync(string title)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow == null)
            return null;

        var files = await desktop.MainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return files.Count > 0 ? Path.TrimEndingDirectorySeparator(files[0].Path.LocalPath) : null;
    }
}