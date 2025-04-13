using System.Threading.Tasks;

namespace Avalonia.EpubComic.Tools;

public interface IDialogService
{
    Task ShowMessageBoxAsync(string message, string title = "提示", string buttonText = "确定");
}