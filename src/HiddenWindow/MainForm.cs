using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
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
    private const string GitHubApiUrl = "https://api.github.com/repos/Maziyang2/HiddenWindow/releases/latest";

    // v1.4: 托盘菜单项引用，用于设置关闭后刷新文本
    private ToolStripMenuItem? _pauseMenuItem;

    public MainForm()
    {
        Text = "HiddenWindow";
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        Opacity = 0;

        _settings = AppSettings.Load();
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
            Text = "HiddenWindow"
        };

        _notifyIcon.ContextMenuStrip = BuildMenu();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Hide();

        // 注册全局热键
        if (_settings.HotkeyEnabled)
            RegisterHotkey();

        // 后台检查更新
        Task.Run(CheckForUpdate);
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
        base.OnFormClosing(e);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        // 设置（完整配置入口）
        var settingsItem = new ToolStripMenuItem("设置...");
        settingsItem.Click += (_, _) => ShowSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new ToolStripSeparator());

        // 暂停/恢复（v1.4: 保存引用以便设置关闭后刷新文本）
        _pauseMenuItem = new ToolStripMenuItem(_settings.PauseDocking ? "恢复吸附" : "暂停吸附")
        {
            ShortcutKeyDisplayString = "Ctrl+Alt+H"
        };
        _pauseMenuItem.Click += (_, _) => TogglePause();
        menu.Items.Add(_pauseMenuItem);

        menu.Items.Add(new ToolStripSeparator());

        // 检查更新
        var updateItem = new ToolStripMenuItem("检查更新");
        updateItem.Click += async (_, _) => await CheckForUpdate(manual: true);
        menu.Items.Add(updateItem);

        // 关于
        var aboutItem = new ToolStripMenuItem("关于");
        aboutItem.Click += (_, _) =>
            MessageBox.Show("HiddenWindow v1.4\n智能窗口边缘吸附管理工具\n\n快捷键: Ctrl+Alt+H 暂停/恢复",
                "关于 HiddenWindow", MessageBoxButtons.OK, MessageBoxIcon.Information);
        menu.Items.Add(aboutItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => Close();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void ShowSettings()
    {
        var form = new SettingsForm(_settings, updatedSettings =>
        {
            _settings.PauseDocking = false;
            _dockManager.IsPaused = false;
            _dockManager.UpdateSettings(updatedSettings);

            // 同步开机自启到注册表（v1.4: 托盘菜单已移除该项，统一在设置中管理）
            SetAutoStart(updatedSettings.AutoStart);

            // 热键状态变更
            UnregisterHotkey();
            if (updatedSettings.HotkeyEnabled)
                RegisterHotkey();
        });
        form.ShowDialog();

        // v1.4: 设置窗口关闭后刷新托盘菜单的暂停/恢复文本
        if (_pauseMenuItem != null)
            _pauseMenuItem.Text = _dockManager.IsPaused ? "恢复吸附" : "暂停吸附";
    }

    private void TogglePause()
    {
        _dockManager.IsPaused = !_dockManager.IsPaused;
        _settings.PauseDocking = _dockManager.IsPaused;
        _settings.Save();

        var msg = _dockManager.IsPaused ? "吸附已暂停" : "吸附已恢复";
        _notifyIcon.ShowBalloonTip(1500, "HiddenWindow", msg, ToolTipIcon.Info);
    }

    private void RegisterHotkey()
    {
        WinApi.RegisterHotKey(Handle, WinApi.HOTKEY_ID_PAUSE,
            WinApi.MOD_CONTROL | WinApi.MOD_ALT | WinApi.MOD_NOREPEAT,
            WinApi.VK_H);
    }

    private void UnregisterHotkey()
    {
        WinApi.UnregisterHotKey(Handle, WinApi.HOTKEY_ID_PAUSE);
    }

    // v1.4: 增加 manual 参数 — 手动检查时网络异常弹出提示；增加 HttpClient 超时
    private static async Task CheckForUpdate(bool manual = false)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("HiddenWindow");
            var json = await client.GetStringAsync(GitHubApiUrl);
            using var doc = JsonDocument.Parse(json);
            var tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";

            var currentVersion = "v1.4.0";
            if (string.Compare(tag, currentVersion, StringComparison.OrdinalIgnoreCase) > 0)
            {
                var url = doc.RootElement.GetProperty("html_url").GetString();
                MessageBox.Show($"新版本 {tag} 可用！\n\n下载地址:\n{url}",
                    "更新提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("当前已是最新版本。", "检查更新",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch
        {
            if (manual)
            {
                MessageBox.Show("检查更新失败，请检查网络连接。", "更新提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            // 后台检查时静默忽略
        }
    }

    private static void SetAutoStart(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(AutoStartRegPath);
        if (key == null) return;

        if (enabled)
        {
            var exePath = Application.ExecutablePath;
            key.SetValue(AutoStartValueName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(AutoStartValueName, false);
        }
    }
}
