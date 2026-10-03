using HiddenWindow.Core;

namespace HiddenWindow.Tests;

public sealed class AppSettingsTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "HiddenWindowTests", Guid.NewGuid().ToString("N"));

    private string SettingsFile => Path.Combine(_dir, "settings.json");

    public AppSettingsTests()
    {
        Directory.CreateDirectory(_dir);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsValues()
    {
        var settings = new AppSettings
        {
            EdgeSensitivityPx = 42,
            VisibleEdgePx = 7,
            HideDelayMs = 900,
            AnimationDurationMs = 300,
            HotkeyEnabled = false,
            AutoStart = true,
            PauseDocking = true,
            Language = LanguageMode.ChineseSimplified
        };

        settings.Save(SettingsFile);
        var loaded = AppSettings.Load(SettingsFile);

        Assert.Equal(42, loaded.EdgeSensitivityPx);
        Assert.Equal(7, loaded.VisibleEdgePx);
        Assert.Equal(900, loaded.HideDelayMs);
        Assert.Equal(300, loaded.AnimationDurationMs);
        Assert.False(loaded.HotkeyEnabled);
        Assert.True(loaded.AutoStart);
        Assert.True(loaded.PauseDocking);
        Assert.Equal(LanguageMode.ChineseSimplified, loaded.Language);
    }

    [Fact]
    public void Save_WritesEnumsAsStrings()
    {
        new AppSettings { Language = LanguageMode.English, AnimationSpeed = AnimationSpeed.Fast }
            .Save(SettingsFile);

        var json = File.ReadAllText(SettingsFile);

        Assert.Contains("\"English\"", json);
        Assert.Contains("\"Fast\"", json);
    }

    [Fact]
    public void Load_AcceptsLegacyNumericEnums()
    {
        File.WriteAllText(SettingsFile, """{ "Language": 2, "AnimationSpeed": 1 }""");

        var loaded = AppSettings.Load(SettingsFile);

        Assert.Equal(LanguageMode.English, loaded.Language);
        Assert.Equal(AnimationSpeed.Medium, loaded.AnimationSpeed);
    }

    [Fact]
    public void Load_ResetsUnknownEnumValues()
    {
        File.WriteAllText(SettingsFile, """{ "Language": 99, "AnimationSpeed": 99 }""");

        var loaded = AppSettings.Load(SettingsFile);

        Assert.Equal(LanguageMode.System, loaded.Language);
        Assert.Equal(AnimationSpeed.Medium, loaded.AnimationSpeed);
    }

    [Fact]
    public void Load_ClampsOutOfRangeNumbers()
    {
        File.WriteAllText(SettingsFile,
            """{ "EdgeSensitivityPx": 9999, "VisibleEdgePx": 0, "HideDelayMs": -5, "AnimationDurationMs": 999999 }""");

        var loaded = AppSettings.Load(SettingsFile);

        Assert.Equal(500, loaded.EdgeSensitivityPx);
        Assert.Equal(1, loaded.VisibleEdgePx);
        Assert.Equal(0, loaded.HideDelayMs);
        Assert.Equal(10_000, loaded.AnimationDurationMs);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaultsAndCreatesFile()
    {
        var loaded = AppSettings.Load(SettingsFile);

        Assert.Equal(50, loaded.EdgeSensitivityPx);
        Assert.Equal(LanguageMode.System, loaded.Language);
        Assert.True(File.Exists(SettingsFile));
    }

    [Fact]
    public void Load_CorruptFile_FallsBackToDefaults()
    {
        File.WriteAllText(SettingsFile, "{ this is not json");

        var loaded = AppSettings.Load(SettingsFile);

        Assert.Equal(50, loaded.EdgeSensitivityPx);
        Assert.True(File.Exists(SettingsFile));
    }

    [Fact]
    public void EffectiveAnimationDuration_UsesEnumWhenNoCustomValue()
    {
        Assert.Equal(120, new AppSettings { AnimationSpeed = AnimationSpeed.Fast }.EffectiveAnimationDurationMs);
        Assert.Equal(240, new AppSettings { AnimationSpeed = AnimationSpeed.Medium }.EffectiveAnimationDurationMs);
        Assert.Equal(360, new AppSettings { AnimationSpeed = AnimationSpeed.Slow }.EffectiveAnimationDurationMs);
        Assert.Equal(500, new AppSettings { AnimationDurationMs = 500 }.EffectiveAnimationDurationMs);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, recursive: true);
            }
        }
        catch
        {
            // 清理失败不影响测试结果
        }
    }
}
