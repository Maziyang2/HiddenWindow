using System;
using System.IO;
using System.Text.Json;

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
    public AnimationSpeed AnimationSpeed { get; set; } = AnimationSpeed.Medium;
    public bool AutoStart { get; set; } = false;
    public int VisibleEdgePx { get; set; } = 5;

    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HiddenWindow",
        "settings.json");

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
