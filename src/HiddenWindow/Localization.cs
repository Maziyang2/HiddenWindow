using System;
using System.Collections.Generic;
using System.Globalization;

namespace HiddenWindow;

internal static class Localization
{
    private static readonly IReadOnlyDictionary<string, (string Zh, string En)> Strings =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["settings"] = ("设置", "Settings"),
            ["settingsTitle"] = ("HiddenWindow 设置", "HiddenWindow Settings"),
            ["settingsSubtitle"] = ("让窗口退到边缘，让注意力回到中心。", "Move windows to the edge. Bring focus back to the center."),
            ["behavior"] = ("窗口行为", "Window behavior"),
            ["behaviorHint"] = ("精细控制吸附触发、露出宽度与动画节奏。", "Tune edge detection, reveal width, and motion timing."),
            ["edgeSensitivity"] = ("边缘检测灵敏度", "Edge sensitivity"),
            ["edgeSensitivityHint"] = ("窗口靠近屏幕边缘时的触发范围", "Trigger range when a window approaches an edge"),
            ["visibleEdge"] = ("可见边缘宽度", "Visible edge width"),
            ["visibleEdgeHint"] = ("窗口隐藏后仍保留在屏幕内的宽度", "Width kept visible after the window is hidden"),
            ["animationDuration"] = ("动画时长", "Animation duration"),
            ["animationDurationHint"] = ("窗口滑入和滑出的动画速度", "Speed of the reveal and hide animation"),
            ["hideDelay"] = ("自动隐藏延迟", "Auto-hide delay"),
            ["hideDelayHint"] = ("鼠标离开窗口后等待隐藏的时间", "Wait time before hiding after the pointer leaves"),
            ["preferences"] = ("偏好设置", "Preferences"),
            ["hotkey"] = ("启用全局快捷键", "Enable global shortcut"),
            ["hotkeyHint"] = ("Ctrl + Alt + H 暂停或恢复吸附", "Ctrl + Alt + H pauses or resumes docking"),
            ["autoStart"] = ("开机自动启动", "Launch at startup"),
            ["autoStartHint"] = ("登录 Windows 后在后台启动 HiddenWindow", "Start HiddenWindow in the background after signing in"),
            ["language"] = ("界面语言", "Interface language"),
            ["languageHint"] = ("默认跟随 Windows 显示语言", "Follows the Windows display language by default"),
            ["languageSystem"] = ("跟随系统", "Follow system"),
            ["languageChinese"] = ("简体中文", "简体中文"),
            ["languageEnglish"] = ("English", "English"),
            ["cancel"] = ("取消", "Cancel"),
            ["save"] = ("保存更改", "Save changes"),
            ["pauseDocking"] = ("暂停吸附", "Pause docking"),
            ["resumeDocking"] = ("恢复吸附", "Resume docking"),
            ["checkUpdates"] = ("检查更新", "Check for updates"),
            ["about"] = ("关于 HiddenWindow", "About HiddenWindow"),
            ["exit"] = ("退出", "Quit"),
            ["paused"] = ("吸附已暂停", "Docking paused"),
            ["resumed"] = ("吸附已恢复", "Docking resumed"),
            ["latest"] = ("当前已是最新版本。", "You are running the latest version."),
            ["updateAvailable"] = ("发现新版本 {0}\n\n访问发布页面：\n{1}", "Version {0} is available.\n\nOpen the release page:\n{1}"),
            ["updateTitle"] = ("HiddenWindow 更新", "HiddenWindow Update"),
            ["updateFailed"] = ("检查更新失败，请稍后重试。", "Update check failed. Please try again later."),
            ["aboutLead"] = ("为专注而生的开源窗口编排工具。", "An intentional open-source window orchestration layer for focused work."),
            ["aboutBody"] = ("将暂时不用的窗口收进屏幕边缘，需要时自然唤回。以克制的设计、清晰的秩序和安静的交互，把注意力留给真正重要的内容。", "Move inactive windows to the screen edge and recall them naturally. Restrained design, clear order, and quiet interactions keep attention on what matters."),
            ["website"] = ("访问产品主页", "Visit product website"),
            ["openSource"] = ("开源 · MIT License · .NET 8", "Open source · MIT License · .NET 8"),
            ["statusActive"] = ("运行中", "ACTIVE"),
            ["brandCaption"] = ("WINDOW ORCHESTRATION / V2.1", "WINDOW ORCHESTRATION / V2.1")
        };

    public static LanguageMode Mode { get; private set; } = LanguageMode.System;

    public static bool IsChinese => ResolveLanguage() == LanguageMode.ChineseSimplified;

    public static void Configure(LanguageMode mode) => Mode = mode;

    public static string Get(string key)
    {
        if (!Strings.TryGetValue(key, out var value))
            return key;
        return IsChinese ? value.Zh : value.En;
    }

    public static LanguageMode ResolveLanguage()
    {
        if (Mode != LanguageMode.System)
            return Mode;

        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase)
            ? LanguageMode.ChineseSimplified
            : LanguageMode.English;
    }
}
