using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Avalonia.EpubComic.ViewModels;

public partial class MessageBoxViewModel : ViewModelBase
{
    [ObservableProperty] private string _message;
    [ObservableProperty] private string _title;

    [RelayCommand]
    private void Close()
    {
        WeakReferenceMessenger.Default.Send(new CloseWindowMessage());
    }
}

public class CloseWindowMessage
{
}