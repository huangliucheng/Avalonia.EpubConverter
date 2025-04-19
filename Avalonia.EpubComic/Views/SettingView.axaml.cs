using Avalonia.Controls;
using Avalonia.EpubComic.ViewModels;
using Avalonia.Interactivity;

namespace Avalonia.EpubComic.Views;

public partial class SettingView : UserControl
{
    public SettingView()
    {
        InitializeComponent();
    }

    private void CustomDeviceProfileSelected(object? sender, SelectionChangedEventArgs e)
    {
        var comBox = sender as ComboBox;
        var selected = comBox?.SelectedItem as string;
        if (selected == null) return;
        var viewModel = DataContext as SettingViewModel;
        if (viewModel == null) return;
        viewModel.IsTargetResolutionEnabled = selected == "Other";
        viewModel.Setting.TargetWidth = viewModel.Setting.DeviceProfile[selected][0];
        viewModel.Setting.TargetHeight = viewModel.Setting.DeviceProfile[selected][1];
    }

    private void TargetWidthChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        var numberBox = sender as NumericUpDown;
        if (numberBox == null) return;
        var viewModel = DataContext as SettingViewModel;
        if (numberBox.Value == null) return;
        var value = (int)numberBox.Value;
        var deviceName = viewModel.Setting.DeviceList[viewModel.Setting.SelectedDevice];
        viewModel.Setting.DeviceProfile[deviceName][0] = value;
    }

    private void TargetHeightChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        var numberBox = sender as NumericUpDown;
        if (numberBox == null) return;
        var viewModel = DataContext as SettingViewModel;
        if (numberBox.Value == null) return;
        var value = (int)numberBox.Value;
        var deviceName = viewModel.Setting.DeviceList[viewModel.Setting.SelectedDevice];
        viewModel.Setting.DeviceProfile[deviceName][1] = value;
    }

    private void OutputModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        var comboBox = sender as ComboBox;
        var selectedIndex = comboBox?.SelectedIndex;
        if (selectedIndex == null) return;
        var viewModel = DataContext as SettingViewModel;
        viewModel.IsOutSplitNumberVisible = selectedIndex == 1;
    }

    private void EveryChapterNumber_LostFocus(object? sender, RoutedEventArgs e)
    {
        var numerBox = sender as NumericUpDown;
        if (numerBox == null) return;
        var viewModel = DataContext as SettingViewModel;
        viewModel.Setting.EveryChaptersNum = (int)numerBox.Value;
    }
}