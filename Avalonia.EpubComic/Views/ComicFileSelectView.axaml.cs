using Avalonia.Controls;
using Avalonia.EpubComic.ViewModels;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.EpubComic.Views;

public partial class ComicFileSelectView : UserControl
{
    public ComicFileSelectView()
    {
        InitializeComponent();
    }

    private void TextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) CallReplaceCommand();
    }

    private void TextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        CallReplaceCommand();
    }

    private void CallReplaceCommand()
    {
        var viewModel = DataContext as ComicFileSelectViewModel;
        viewModel?.ReplaceCommand.Execute(null);
    }
}