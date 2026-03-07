using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace HiddenWindow;

internal sealed class MainForm : Form
{
    private readonly NotifyIcon _notifyIcon;
    private readonly DockManager _dockManager;
    private readonly AppSettings _settings;

    private const string AutoStartRegPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string AutoStartValueName = "HiddenWindow";

    public MainForm()
    {
        Text = "HiddenWindow";
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        Opacity = 0;

        _settings = AppSettings.Load();
        _dockManager = new DockManager(_settings);

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
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _dockManager.Dispose();
        base.OnFormClosing(e);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        var autoStartItem = new ToolStripMenuItem("开机自启")
        {
            Checked = _settings.AutoStart,
            CheckOnClick = true
        };
        autoStartItem.CheckedChanged += (_, _) =>
        {
            _settings.AutoStart = autoStartItem.Checked;
            SetAutoStart(_settings.AutoStart);
            _settings.Save();
        };

        var sensitivityMenu = new ToolStripMenuItem("边缘灵敏度");
        sensitivityMenu.DropDownItems.Add(CreateSensitivityItem("20 px", 20));
        sensitivityMenu.DropDownItems.Add(CreateSensitivityItem("50 px", 50));
        sensitivityMenu.DropDownItems.Add(CreateSensitivityItem("70 px", 70));

        var speedMenu = new ToolStripMenuItem("动画速度");
        speedMenu.DropDownItems.Add(CreateSpeedItem("快", AnimationSpeed.Fast));
        speedMenu.DropDownItems.Add(CreateSpeedItem("适中", AnimationSpeed.Medium));
        speedMenu.DropDownItems.Add(CreateSpeedItem("慢", AnimationSpeed.Slow));

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => Close();

        menu.Items.Add(autoStartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(sensitivityMenu);
        menu.Items.Add(speedMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        return menu;
    }

    private ToolStripMenuItem CreateSensitivityItem(string label, int value)
    {
        var item = new ToolStripMenuItem(label)
        {
            Checked = _settings.EdgeSensitivityPx == value,
            CheckOnClick = true
        };

        item.Click += (_, _) =>
        {
            _settings.EdgeSensitivityPx = value;
            UncheckSiblings(item);
            _settings.Save();
            _dockManager.UpdateSettings(_settings);
        };

        return item;
    }

    private ToolStripMenuItem CreateSpeedItem(string label, AnimationSpeed speed)
    {
        var item = new ToolStripMenuItem(label)
        {
            Checked = _settings.AnimationSpeed == speed,
            CheckOnClick = true
        };

        item.Click += (_, _) =>
        {
            _settings.AnimationSpeed = speed;
            UncheckSiblings(item);
            _settings.Save();
            _dockManager.UpdateSettings(_settings);
        };

        return item;
    }

    private static void UncheckSiblings(ToolStripMenuItem item)
    {
        if (item.GetCurrentParent() is not ToolStripDropDownMenu parent)
        {
            return;
        }

        foreach (ToolStripItem sibling in parent.Items)
        {
            if (sibling is ToolStripMenuItem menuItem && menuItem != item)
            {
                menuItem.Checked = false;
            }
        }
    }

    private static void SetAutoStart(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(AutoStartRegPath);
        if (key == null)
        {
            return;
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
    }
}
