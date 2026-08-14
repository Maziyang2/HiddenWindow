using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace HiddenWindow;

internal sealed class AboutForm : Form
{
    private const string WebsiteUrl = "https://github.maziyang.top";

    public AboutForm()
    {
        Text = Localization.Get("about");
        ClientSize = new Size(560, 440);
        BackColor = UiTheme.Canvas;
        ForeColor = UiTheme.Text;
        Font = UiTheme.Font(9f);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;

        var mark = new BrandMarkControl { Location = new Point(38, 36), Size = new Size(56, 56) };
        Controls.Add(mark);

        Controls.Add(new Label
        {
            AutoSize = true,
            Text = "HIDDENWINDOW",
            ForeColor = UiTheme.Text,
            Font = UiTheme.DisplayFont(23f, FontStyle.Bold),
            Location = new Point(120, 35)
        });
        Controls.Add(new Label
        {
            AutoSize = true,
            Text = "VERSION 2.1.0 / OPEN SOURCE",
            ForeColor = UiTheme.TextDim,
            Font = UiTheme.Font(8f, FontStyle.Bold),
            Location = new Point(122, 77)
        });

        var divider = new Panel { BackColor = UiTheme.Line, Location = new Point(38, 122), Size = new Size(484, 1) };
        Controls.Add(divider);

        Controls.Add(new Label
        {
            AutoSize = false,
            Text = Localization.Get("aboutLead"),
            ForeColor = UiTheme.Text,
            Font = UiTheme.DisplayFont(16f, FontStyle.Bold),
            Location = new Point(38, 154),
            Size = new Size(484, 58)
        });
        Controls.Add(new Label
        {
            AutoSize = false,
            Text = Localization.Get("aboutBody"),
            ForeColor = UiTheme.TextMuted,
            Font = UiTheme.Font(10f),
            Location = new Point(38, 224),
            Size = new Size(470, 68)
        });

        var website = new ModernButton(primary: true)
        {
            Text = $"{Localization.Get("website")}  ↗",
            Location = new Point(38, 316),
            Width = 180
        };
        website.Click += (_, _) => OpenWebsite();
        Controls.Add(website);

        Controls.Add(new Label
        {
            AutoSize = true,
            Text = Localization.Get("openSource"),
            ForeColor = UiTheme.TextDim,
            Font = UiTheme.Font(8f, FontStyle.Bold),
            Location = new Point(38, 392)
        });

        Shown += (_, _) => UiTheme.ApplyTitleBar(this);
    }

    private static void OpenWebsite()
    {
        try
        {
            Process.Start(new ProcessStartInfo(WebsiteUrl) { UseShellExecute = true });
        }
        catch
        {
            // 浏览器不可用时保持静默，不影响主程序。
        }
    }
}
