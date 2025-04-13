using System.Threading.Tasks;
using Avalonia.Controls;

namespace Avalonia.EpubComic.Services;

public class DialogService : IDialogService
{
    private readonly Window _mainWindow;

    public DialogService(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public async Task ShowMessageBoxAsync(string title, string message)
    {
        var dialog = new MessageBoxWindow(title, message);
        await dialog.ShowDialog(_mainWindow);
    }
}