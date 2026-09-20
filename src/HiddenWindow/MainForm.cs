using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using HiddenWindow.Core;
using Microsoft.Win32;

namespace HiddenWindow;

internal sealed class MainForm : Form
{
    private readonly NotifyIcon _notifyIcon;
    private readonly DockManager _dockManager;
    private readonly AppSettings _settings;
    private readonly EdgeHintForm _hintForm;

    private const string AutoStartRegPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string AutoStartValueName = "HiddenWindow";
    private const string GitHubApiUrl = "https://api.github.com/repos/" + AppInfo.RepositorySlug + "/releases/latest";

    // v1.4: 托盘菜单项引用，用于设置关闭后刷新文本
    private ToolStripMenuItem? _pauseMenuItem;

    public MainForm()
    {
        Text = AppInfo.Name;
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        Opacity = 0;

        _settings = AppSettings.Load();
        Localization.Configure(_settings.Language);
        _hintForm = new EdgeHintForm();
        _dockManager = new DockManager(_settings, _hintForm);

        // 恢复暂停状态
        if (_settings.PauseDocking)
            _dockManager.IsPaused = true;

        var trayIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        _notifyIcon = new NotifyIcon
        {
            Icon = trayIcon,
            Visible = true,
            Text = AppInfo.Name
        };

        _notifyIcon.ContextMenuStrip = BuildMenu();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Hide();

        // 注册全局热键
        if (_settings.HotkeyEnabled && !RegisterHotkey())
        {
            Notify(Localization.Get("hotkeyFailed"), ToolTipIcon.Warning);
        }

        // 后台检查更新
        _ = CheckForUpdate(manual: false);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WinApi.WM_HOTKEY && m.WParam == (IntPtr)WinApi.HOTKEY_ID_PAUSE)
        {
            TogglePause();
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        UnregisterHotkey();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _dockManager.Dispose();
        _hintForm.Dispose();
        base.OnFormClosing(e);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip
        {
            BackColor = UiTheme.Surface,
            ForeColor = UiTheme.Text,
            Font = UiTheme.Font(9.5f),
            ShowImageMargin = false,
            Padding = new Padding(6),
            Renderer = new ToolStripProfessionalRenderer(new HiddenWindowColorTable())
        };

        // 设置（完整配置入口）
        var settingsItem = new ToolStripMenuItem($"{Localization.Get("settings")}...")
        {
            Padding = new Padding(8, 6, 8, 6)
        };
        settingsItem.Click += (_, _) => ShowSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new ToolStripSeparator());

        // 暂停/恢复（v1.4: 保存引用以便设置关闭后刷新文本）
        _pauseMenuItem = new ToolStripMenuItem(_settings.PauseDocking
            ? Localization.Get("resumeDocking")
            : Localization.Get("pauseDocking"))
        {
            ShortcutKeyDisplayString = "Ctrl+Alt+H"
        };
        _pauseMenuItem.Click += (_, _) => TogglePause();
        menu.Items.Add(_pauseMenuItem);

        menu.Items.Add(new ToolStripSeparator());

        // 检查更新
        var updateItem = new ToolStripMenuItem(Localization.Get("checkUpdates"));
        updateItem.Click += async (_, _) => await CheckForUpdate(manual: true);
        menu.Items.Add(updateItem);

        // 关于
        var aboutItem = new ToolStripMenuItem(Localization.Get("about"));
        aboutItem.Click += (_, _) => new AboutForm().ShowDialog();
        menu.Items.Add(aboutItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem(Localization.Get("exit"));
        exitItem.Click += (_, _) => Close();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void ShowSettings()
    {
        var form = new SettingsForm(_settings, updatedSettings =>
        {
            // 保存设置视为恢复吸附：内存与配置文件一起更新，避免重启后状态不一致
            _dockManager.IsPaused = false;
            if (_settings.PauseDocking)
            {
                _settings.PauseDocking = false;
                _settings.Save();
            }

            _dockManager.UpdateSettings(updatedSettings);

            // 同步开机自启到注册表（v1.4: 托盘菜单已移除该项，统一在设置中管理）
            if (updatedSettings.AutoStart && !SetAutoStart(enabled: true))
            {
                Notify(Localization.Get("autoStartFailed"), ToolTipIcon.Warning);
            }
            else
            {
                SetAutoStart(updatedSettings.AutoStart);
            }

            // 热键状态变更
            UnregisterHotkey();
            if (updatedSettings.HotkeyEnabled && !RegisterHotkey())
            {
                Notify(Localization.Get("hotkeyFailed"), ToolTipIcon.Warning);
            }

            Localization.Configure(updatedSettings.Language);
            var oldMenu = _notifyIcon.ContextMenuStrip;
            _notifyIcon.ContextMenuStrip = BuildMenu();
            oldMenu?.Dispose();
        });
        form.ShowDialog();

        // v1.4: 设置窗口关闭后刷新托盘菜单的暂停/恢复文本
        if (_pauseMenuItem != null)
            _pauseMenuItem.Text = _dockManager.IsPaused
                ? Localization.Get("resumeDocking")
                : Localization.Get("pauseDocking");
    }

    private void TogglePause()
    {
        _dockManager.IsPaused = !_dockManager.IsPaused;
        _settings.PauseDocking = _dockManager.IsPaused;
        _settings.Save();

        var msg = _dockManager.IsPaused ? Localization.Get("paused") : Localization.Get("resumed");
        Notify(msg);
    }

    private bool RegisterHotkey() =>
        WinApi.RegisterHotKey(Handle, WinApi.HOTKEY_ID_PAUSE,
            WinApi.MOD_CONTROL | WinApi.MOD_ALT | WinApi.MOD_NOREPEAT,
            WinApi.VK_H);

    private void UnregisterHotkey() =>
        WinApi.UnregisterHotKey(Handle, WinApi.HOTKEY_ID_PAUSE);

    private void Notify(string message, ToolTipIcon icon = ToolTipIcon.Info) =>
        _notifyIcon.ShowBalloonTip(3000, AppInfo.Name, message, icon);

    // v1.4: 增加 manual 参数 — 手动检查时网络异常弹出提示；增加 HttpClient 超时
    // 网络请求在线程池执行，弹窗始终回到 UI 线程
    private static async Task CheckForUpdate(bool manual)
    {
        string? tag = null;
        string? url = null;
        var failed = false;

        try
        {
            (tag, url) = await Task.Run(async () =>
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                client.DefaultRequestHeaders.UserAgent.ParseAdd(AppInfo.Name);
                var json = await client.GetStringAsync(GitHubApiUrl).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                var latestTag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
                var releaseUrl = doc.RootElement.TryGetProperty("html_url", out var html) ? html.GetString() : null;
                return (latestTag, releaseUrl);
            });
        }
        catch
        {
            failed = true;
        }

        if (failed)
        {
            if (manual)
            {
                MessageBox.Show(Localization.Get("updateFailed"), Localization.Get("updateTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            // 后台检查时静默忽略
            return;
        }

        var currentVersion = typeof(MainForm).Assembly.GetName().Version ?? new Version(0, 0, 0);
        var hasNewerVersion = Version.TryParse(tag?.TrimStart('v', 'V'), out var latestVersion)
            && latestVersion > currentVersion;

        if (hasNewerVersion)
        {
            MessageBox.Show(string.Format(Localization.Get("updateAvailable"), tag, url),
                Localization.Get("updateTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else if (manual)
        {
            MessageBox.Show(Localization.Get("latest"), Localization.Get("checkUpdates"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    /// <summary>写入/移除开机自启项；失败时返回 false（注册表可能被安全软件锁定）。</summary>
    private static bool SetAutoStart(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(AutoStartRegPath);
            if (key == null)
            {
                return false;
            }

            if (enabled)
            {
                var exePath = Application.ExecutablePath;
                key.SetValue(AutoStartValueName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AutoStartValueName, false);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
