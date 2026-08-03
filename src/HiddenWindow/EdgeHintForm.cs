using System;
using System.Drawing;
using System.Windows.Forms;

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
        BackColor = Color.FromArgb(32, 32, 32);
        ForeColor = Color.White;
        Opacity = 0;
        AutoSize = true;
        Padding = new Padding(12, 5, 12, 5);

        _titleLabel = new Label
        {
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular),
            MaximumSize = new Size(380, 0)
        };
        Controls.Add(_titleLabel);

        _hideTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _hideTimer.Tick += (_, _) =>
        {
            if ((DateTime.UtcNow - _hoverStart).TotalMilliseconds > AutoHideMs)
                HideHint();
        };
    }

    public void ShowHint(Point screenPos, string windowTitle)
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

        if (!Visible)
        {
            _titleLabel.Text = windowTitle;
            // v1.4: 先强制布局计算，确保 Height 已根据新文本更新，再定位
            PerformLayout();
            // 定位在鼠标上方偏右
            Location = new Point(screenPos.X + 12, screenPos.Y - Height - 8);
            Opacity = 0.92;
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
