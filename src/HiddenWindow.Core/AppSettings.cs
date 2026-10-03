using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HiddenWindow.Core;

public enum AnimationSpeed
{
    Fast,
    Medium,
    Slow
}

public enum LanguageMode
{
    System,
    ChineseSimplified,
    English
}

public sealed class AppSettings
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        // 字符串枚举：将来调整枚举顺序不会破坏旧配置文件
        Converters = { new JsonStringEnumConverter() }
    };

    public int EdgeSensitivityPx { get; set; } = 50;
    public int VisibleEdgePx { get; set; } = 5;
    public int HideDelayMs { get; set; } = 300;
    public AnimationSpeed AnimationSpeed { get; set; } = AnimationSpeed.Medium;

    // 自定义动画时长，0 表示使用 AnimationSpeed 枚举值
    public int AnimationDurationMs { get; set; } = 0;
    public bool HotkeyEnabled { get; set; } = true;
    public bool PauseDocking { get; set; } = false;

    public bool AutoStart { get; set; } = false;
    public LanguageMode Language { get; set; } = LanguageMode.System;

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

    public static AppSettings Load(string? path = null)
    {
        var target = path ?? SettingsPath;
        try
        {
            if (!File.Exists(target))
            {
                var fresh = new AppSettings();
                fresh.Save(target);
                return fresh;
            }

            var json = File.ReadAllText(target);
            var data = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? new AppSettings();
            data.Normalize();
            return data;
        }
        catch
        {
            // 配置文件损坏或不可读时回退默认值，原文件保持不动
            return new AppSettings();
        }
    }

    public void Save(string? path = null)
    {
        var target = path ?? SettingsPath;
        try
        {
            var dir = Path.GetDirectoryName(target);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(this, SerializerOptions);

            // 先写临时文件再原子替换，避免写入中断留下半份 JSON
            var temp = target + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, target, overwrite: true);
        }
        catch
        {
            // 磁盘或权限异常时保留旧设置，托盘程序不应因此崩溃
        }
    }

    /// <summary>校验外部写入/旧版本残留的取值，避免非法数据导致行为异常。</summary>
    private void Normalize()
    {
        EdgeSensitivityPx = Math.Clamp(EdgeSensitivityPx, 1, 500);
        VisibleEdgePx = Math.Clamp(VisibleEdgePx, 1, 200);
        HideDelayMs = Math.Clamp(HideDelayMs, 0, 60_000);
        AnimationDurationMs = Math.Clamp(AnimationDurationMs, 0, 10_000);

        if (!Enum.IsDefined(AnimationSpeed))
        {
            AnimationSpeed = AnimationSpeed.Medium;
        }

        if (!Enum.IsDefined(Language))
        {
            Language = LanguageMode.System;
        }
    }
}
