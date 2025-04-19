using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.EpubComic.Models;

public partial class SettingModel : ObservableObject
{
    [ObservableProperty] private bool _disProcess;
    [ObservableProperty] private int _everyChaptersNum = 1;
    [ObservableProperty] private bool _grayMode;
    [ObservableProperty] private bool _mangeMode;
    [ObservableProperty] private bool _marginCrop;
    [ObservableProperty] private int _maxEveryChaptersNum;
    [ObservableProperty] private List<string> _outputMode;
    private bool _rotate;
    [ObservableProperty] private int _selectedDevice;
    [ObservableProperty] private int _selectedOutputMode;

    private bool _split;

    [ObservableProperty] private int _targetHeight;
    [ObservableProperty] private int _targetWidth;

    [ObservableProperty] private bool _usePNG;

    public SettingModel()
    {
        UsePNG = true;
        GrayMode = false;
        MarginCrop = false;
        MangeMode = false;
        DisProcess = false;
        Split = true;
        Rotate = false;
        SelectedDevice = 0;
        DeviceList =
        [
            "BooxPoke5",
            "BooxPoke5s",
            "BooxLeafSeris",
            "BooxNovaNote",
            "Other"
        ];
        DeviceProfile = new Dictionary<string, int[]>
        {
            ["BooxPoke5"] = [1072, 1448],
            ["BooxPoke5s"] = [758, 1024],
            ["BooxLeafSeris"] = [1264, 1680],
            ["BooxNovaNote"] = [1404, 1872],
            ["Other"] = [600, 800]
        };
        var deviceName = DeviceList[SelectedDevice];
        if (DeviceProfile.TryGetValue(deviceName, out var resolution))
        {
            TargetWidth = resolution[0];
            TargetHeight = resolution[1];
        }

        OutputMode = ["单本模式", "连载模式", "分卷模式"];
    }

    public bool Split
    {
        get => _split;
        set
        {
            SetProperty(ref _split, value);
            SetProperty(ref _rotate, !value);
        }
    }

    public bool Rotate
    {
        get => _rotate;
        set
        {
            SetProperty(ref _rotate, value);
            SetProperty(ref _split, !value);
        }
    }

    public List<string> DeviceList { get; set; }
    public Dictionary<string, int[]> DeviceProfile { get; set; }

    // 静态工厂方法，从JSON创建实例
    public static async Task<SettingModel> CreateFromJsonAsync(string? filePath = null)
    {
        filePath ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "settings.json");

        // 创建默认实例
        var model = new SettingModel();

        try
        {
            if (File.Exists(filePath))
            {
                // 读取JSON文件
                var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var loadedModel = JsonSerializer.Deserialize<SettingModel>(json, options);

                // 如果成功加载，复制值到当前实例
                if (loadedModel != null)
                {
                    model.UsePNG = loadedModel.UsePNG;
                    model.GrayMode = loadedModel.GrayMode;
                    model.MarginCrop = loadedModel.MarginCrop;
                    model.MangeMode = loadedModel.MangeMode;
                    model.DisProcess = loadedModel.DisProcess;
                    model.Split = loadedModel.Split;
                    model.Rotate = loadedModel.Rotate;
                    model.TargetWidth = loadedModel.TargetWidth;
                    model.TargetHeight = loadedModel.TargetHeight;
                    model.SelectedDevice = loadedModel.SelectedDevice;
                    model.SelectedOutputMode = loadedModel.SelectedOutputMode;
                }
            }
            else
            {
                // 保存默认设置到文件
                await model.SaveSettingsAsync(filePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载设置时出错: {ex.Message}");
        }

        return model;
    }

    // 保存设置到JSON文件
    public async Task SaveSettingsAsync(string? filePath = null)
    {
        try
        {
            filePath ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "settings.json");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(this, options);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存设置时出错: {ex.Message}");
        }
    }

    public event EventHandler<int> EveryChaptersNumChanged;
    public event EventHandler<int> SelectedOutputModeChanged;

    partial void OnEveryChaptersNumChanged(int value)
    {
        EveryChaptersNumChanged?.Invoke(this, value);
    }

    partial void OnSelectedOutputModeChanged(int value)
    {
        SelectedOutputModeChanged?.Invoke(this, value);
    }
}