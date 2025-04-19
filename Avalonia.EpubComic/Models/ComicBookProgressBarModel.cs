using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.EpubComic.Models;

public partial class ComicBookProgressBarModel : ObservableObject
{
    [ObservableProperty] private int _currentValue;
    [ObservableProperty] private bool _isIndeterminate;
    [ObservableProperty] private int _maxValue;

    public ComicBookProgressBarModel()
    {
        CurrentValue = 0;
        MaxValue = 1;
    }
}