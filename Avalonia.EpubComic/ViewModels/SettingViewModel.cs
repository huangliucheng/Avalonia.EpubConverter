using System.Threading.Tasks;
using Avalonia.EpubComic.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.EpubComic.ViewModels;

public partial class SettingViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isOutSplitNumberVisible;
    [ObservableProperty] private bool _isTargetResolutionEnabled;
    [ObservableProperty] private SettingModel _setting;

    public SettingViewModel()
    {
        InitializeSettingAsync().Wait();
    }

    private async Task InitializeSettingAsync()
    {
        Setting = await SettingModel.CreateFromJsonAsync().ConfigureAwait(false);
    }
}