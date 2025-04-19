using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.EpubComic.Views;

namespace Avalonia.EpubComic.Services;

public class DialogService : IDialogService
{
    private readonly Window _mainWindow;

    public DialogService(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public async Task ShowMessageBoxAsync(string message, string title)
    {
        var messageBox = new MessageBox(title, message);
        await messageBox.ShowDialog(_mainWindow);
    }
}