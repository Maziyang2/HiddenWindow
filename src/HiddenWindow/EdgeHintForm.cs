using System;
using System.Drawing;
using System.Windows.Forms;
using HiddenWindow.Core;

namespace HiddenWindow;

/// <summary>
/// 鼠标悬停在隐藏窗口边缘时，显示窗口标题的提示小窗
/// </summary>
internal sealed class EdgeHintForm : Form
{
    private readonly Label _titleLabel;
    private readonly System.Windows.Forms.Timer _hideTimer;
    private const int ShowDelayMs = 400;
    private const int AutoHideMs = 2000;
    private DateTime _hoverStart = DateTime.MinValue;

    public EdgeHintForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = UiTheme.SurfaceRaised;
        ForeColor = UiTheme.Text;
        Opacity = 0;
        AutoSize = true;
        Padding = new Padding(14, 8, 14, 8);

        _titleLabel = new Label
        {
            AutoSize = true,
            ForeColor = UiTheme.Text,
            Font = UiTheme.Font(9f, FontStyle.Bold),
            MaximumSize = new Size(380, 0)
        };
        Controls.Add(_titleLabel);

        Paint += (_, e) =>
        {
            using var pen = new Pen(UiTheme.LineStrong);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };

        _hideTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _hideTimer.Tick += (_, _) =>
        {
            if ((DateTime.UtcNow - _hoverStart).TotalMilliseconds > AutoHideMs)
                HideHint();
        };
    }

    public void ShowHint(POINT screenPos, string windowTitle)
    {
        if (string.IsNullOrWhiteSpace(windowTitle))
        {
            HideHint();
            return;
        }

        var now = DateTime.UtcNow;
        if (_hoverStart == DateTime.MinValue)
            _hoverStart = now;

        if ((now - _hoverStart).TotalMilliseconds < ShowDelayMs)
            return;

        // 提示窗可能正显示着另一个窗口的标题，标题变化时立即刷新
        if (_titleLabel.Text != windowTitle)
            _titleLabel.Text = windowTitle;

        // 先强制布局计算，确保 Width/Height 已根据最新文本更新，再定位
        PerformLayout();

        var workArea = Screen.FromPoint(new Point(screenPos.X, screenPos.Y)).WorkingArea;
        var area = new RECT
        {
            Left = workArea.Left,
            Top = workArea.Top,
            Right = workArea.Right,
            Bottom = workArea.Bottom
        };

        // 默认在光标右上方；顶部边缘改到下方，越界时贴边
        var location = HintPlacement.Compute(screenPos, Width, Height, area);
        Location = new Point(location.X, location.Y);

        if (!Visible)
        {
            Opacity = 0.96;
            Show();
            _hideTimer.Start();
        }
    }

    public void HideHint()
    {
        _hoverStart = DateTime.MinValue;
        Opacity = 0;
        Hide();
        _hideTimer.Stop();
    }
}
