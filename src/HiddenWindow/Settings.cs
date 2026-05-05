using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HiddenWindow;

internal enum AnimationSpeed
{
    Fast,
    Medium,
    Slow
}

internal sealed class AppSettings
{
    public int EdgeSensitivityPx { get; set; } = 50;
    public int VisibleEdgePx { get; set; } = 5;
    public int HideDelayMs { get; set; } = 300;
    public AnimationSpeed AnimationSpeed { get; set; } = AnimationSpeed.Medium;

    // 自定义动画时长，0 表示使用 AnimationSpeed 枚举值
    public int AnimationDurationMs { get; set; } = 0;
    public bool HotkeyEnabled { get; set; } = true;
    public bool PauseDocking { get; set; } = false;

    public bool AutoStart { get; set; } = false;

    [JsonIgnore]
    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HiddenWindow",
        "settings.json");

    // 获取实际动画时长
    [JsonIgnore]
    public int EffectiveAnimationDurationMs => AnimationDurationMs > 0
        ? AnimationDurationMs
        : AnimationSpeed switch
        {
            AnimationSpeed.Fast => 120,
            AnimationSpeed.Medium => 240,
            _ => 360
        };

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                var settings = new AppSettings();
                settings.Save();
                return settings;
            }

            var json = File.ReadAllText(SettingsPath);
            var data = JsonSerializer.Deserialize<AppSettings>(json);
            return data ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(SettingsPath, json);
    }
}
