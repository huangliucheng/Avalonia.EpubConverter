using Avalonia.EpubComic.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.EpubComic.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ComicBooksListViewModel _comicBooksListVM;
    [ObservableProperty] private ComicFileSelectViewModel _comicFileSelectVM;
    [ObservableProperty] private SettingViewModel _settingVM;

    public MainWindowViewModel()
    {
        SettingVM = new SettingViewModel();
        var dispatcher = new DispatchCenter(SettingVM.Setting);
        ComicFileSelectVM = new ComicFileSelectViewModel(dispatcher);
        ComicBooksListVM = new ComicBooksListViewModel(dispatcher);
    }
}