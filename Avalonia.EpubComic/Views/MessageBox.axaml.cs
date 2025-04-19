using Avalonia.Controls;
using Avalonia.EpubComic.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace Avalonia.EpubComic.Views;

public partial class MessageBox : Window
{
    public MessageBox()
    {
        InitializeComponent();
    }

    public MessageBox(string title, string message)
    {
        InitializeComponent();
        DataContext = new MessageBoxViewModel
        {
            Title = title,
            Message = message
        };

        // 注册消息接收器，接收到关闭消息时关闭窗口
        WeakReferenceMessenger.Default.Register<CloseWindowMessage>(this, (r, m) => Close());
    }
}