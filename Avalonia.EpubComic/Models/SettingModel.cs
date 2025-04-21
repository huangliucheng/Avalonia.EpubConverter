using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.EpubComic.Models;

public class SettingModel : ObservableObject
{
    private bool _usePNG;
    private bool _disProcess;
    private bool _grayMode;
    private bool _mangeMode;
    private bool _marginCrop;    
    private bool _rotate;
    private bool _split;
    
    private int _everyChaptersNum = 1;
    private int _maxEveryChaptersNum;
    private int _selectedDevice;
    private int _selectedOutputMode;
    private int _targetHeight;
    private int _targetWidth;

    private List<string> _outputMode; 
    
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
    
    public bool UsePNG
    {
        get => _usePNG;
        set => SetProperty(ref _usePNG, value);
    }
    public bool GrayMode
    {
        get => _grayMode;
        set => SetProperty(ref _grayMode, value);
    }
    public bool MarginCrop
    {
        get => _marginCrop;
        set => SetProperty(ref _marginCrop, value);
    }
    public bool MangeMode
    {
        get => _mangeMode;
        set => SetProperty(ref _mangeMode, value);
    }
    public bool DisProcess
    {
        get => _disProcess;
        set => SetProperty(ref _disProcess, value);
    }

    public int SelectedDevice
    {
        get => _selectedDevice;
        set => SetProperty(ref _selectedDevice, value);
    }
    public int TargetWidth
    {
        get => _targetWidth;
        set => SetProperty(ref _targetWidth, value);
    }
    public int TargetHeight
    {
        get => _targetHeight;
        set => SetProperty(ref _targetHeight, value);
    }
    public int SelectedOutputMode
    {
        get => _selectedOutputMode;
        set
        {
           SetProperty(ref _selectedOutputMode, value); 
           OnSelectedOutputModeChanged(value);
        } 
    }

    public List<string> OutputMode
    {
        get => _outputMode;
        set => SetProperty(ref _outputMode, value);
    }

    public int EveryChaptersNum
    {
        get => _everyChaptersNum;
        set
        {
            SetProperty(ref _everyChaptersNum, value);
            OnEveryChaptersNumChanged(value);
        }
    }

    public int MaxEveryChaptersNum
    {
        get => _maxEveryChaptersNum;
        set => SetProperty(ref _maxEveryChaptersNum, value);
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

// 从JSON文件加载设置到当前实例
public async Task LoadFromJsonAsync()
{
    var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    try
    {
        if (File.Exists(filePath))
        {
            // 读取JSON文件
            var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            var loadedModel = JsonSerializer.Deserialize(json, SettingModelJsonContext.Default.SettingModel);

            // 如果成功加载，复制值到当前实例
            if (loadedModel != null)
            {
                UsePNG = loadedModel.UsePNG;
                GrayMode = loadedModel.GrayMode;
                MarginCrop = loadedModel.MarginCrop;
                MangeMode = loadedModel.MangeMode;
                DisProcess = loadedModel.DisProcess;
                Split = loadedModel.Split;
                Rotate = loadedModel.Rotate;
                TargetWidth = loadedModel.TargetWidth;
                TargetHeight = loadedModel.TargetHeight;
                SelectedDevice = loadedModel.SelectedDevice;
                SelectedOutputMode = loadedModel.SelectedOutputMode;
            }
        }
        else
        {
            // 保存默认设置到文件
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }
    catch (Exception ex)
    {
        throw new Exception($"加载设置时出错: {ex.Message}");
    }
}

    // 保存设置到JSON文件
public async Task SaveSettingsAsync()
    {
        try
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            var json = JsonSerializer.Serialize(this, SettingModelJsonContext.Default.SettingModel);
            await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new Exception("保存出错: " + ex.Message);
        }
    }

    public event EventHandler<int> EveryChaptersNumChanged;
    public event EventHandler<int> SelectedOutputModeChanged;

    private void OnEveryChaptersNumChanged(int value)
    {
        EveryChaptersNumChanged?.Invoke(this, value);
    }

    private void OnSelectedOutputModeChanged(int value)
    {
        SelectedOutputModeChanged?.Invoke(this, value);
    }
}

[JsonSerializable(typeof(SettingModel))]
public partial class SettingModelJsonContext : JsonSerializerContext
{
}