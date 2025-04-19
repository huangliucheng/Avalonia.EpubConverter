using System.Threading.Tasks;

namespace Avalonia.EpubComic.Services;

public interface IDialogService
{
    Task ShowMessageBoxAsync(string message, string title = "提示");
}