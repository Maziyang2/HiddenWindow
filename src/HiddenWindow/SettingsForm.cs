using System;
using System.Drawing;
using System.Windows.Forms;

namespace HiddenWindow;

internal sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly Action<AppSettings> _onSave;

    private readonly TrackBar _sensitivitySlider;
    private readonly Label _sensitivityValue;
    private readonly TrackBar _visibleEdgeSlider;
    private readonly Label _visibleEdgeValue;
    private readonly TrackBar _animationSlider;
    private readonly Label _animationValue;
    private readonly TrackBar _hideDelaySlider;
    private readonly Label _hideDelayValue;
    private readonly CheckBox _hotkeyCheck;
    private readonly CheckBox _autoStartCheck;

    public SettingsForm(AppSettings settings, Action<AppSettings> onSave)
    {
        _settings = settings;
        _onSave = onSave;

        Text = "HiddenWindow 设置";
        Size = new Size(420, 420);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 12),
            ColumnCount = 3,
            RowCount = 7
        };
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));

        // --- 边缘灵敏度 ---
        mainPanel.Controls.Add(new Label { Text = "边缘灵敏度:", TextAlign = ContentAlignment.MiddleLeft, AutoSize = true }, 0, 0);
        _sensitivitySlider = CreateSlider(10, 100, settings.EdgeSensitivityPx, 5);
        _sensitivityValue = new Label { Text = $"{settings.EdgeSensitivityPx} px", TextAlign = ContentAlignment.MiddleCenter, AutoSize = true };
        mainPanel.Controls.Add(_sensitivitySlider, 1, 0);
        mainPanel.Controls.Add(_sensitivityValue, 2, 0);

        // --- 可见边宽度 ---
        mainPanel.Controls.Add(new Label { Text = "可见边宽度:", TextAlign = ContentAlignment.MiddleLeft, AutoSize = true }, 0, 1);
        _visibleEdgeSlider = CreateSlider(2, 15, settings.VisibleEdgePx, 1);
        _visibleEdgeValue = new Label { Text = $"{settings.VisibleEdgePx} px", TextAlign = ContentAlignment.MiddleCenter, AutoSize = true };
        mainPanel.Controls.Add(_visibleEdgeSlider, 1, 1);
        mainPanel.Controls.Add(_visibleEdgeValue, 2, 1);

        // --- 动画时长 ---
        mainPanel.Controls.Add(new Label { Text = "动画时长:", TextAlign = ContentAlignment.MiddleLeft, AutoSize = true }, 0, 2);
        _animationSlider = CreateSlider(80, 600, settings.EffectiveAnimationDurationMs, 20);
        _animationValue = new Label { Text = $"{settings.EffectiveAnimationDurationMs} ms", TextAlign = ContentAlignment.MiddleCenter, AutoSize = true };
        mainPanel.Controls.Add(_animationSlider, 1, 2);
        mainPanel.Controls.Add(_animationValue, 2, 2);

        // --- 隐藏延迟 ---
        mainPanel.Controls.Add(new Label { Text = "隐藏延迟:", TextAlign = ContentAlignment.MiddleLeft, AutoSize = true }, 0, 3);
        _hideDelaySlider = CreateSlider(50, 2000, settings.HideDelayMs, 50);
        _hideDelayValue = new Label { Text = $"{settings.HideDelayMs} ms", TextAlign = ContentAlignment.MiddleCenter, AutoSize = true };
        mainPanel.Controls.Add(_hideDelaySlider, 1, 3);
        mainPanel.Controls.Add(_hideDelayValue, 2, 3);

        // --- 热键 ---
        _hotkeyCheck = new CheckBox
        {
            Text = "启用全局热键 (Ctrl+Alt+H 暂停/恢复吸附)",
            Checked = settings.HotkeyEnabled,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0)
        };
        mainPanel.Controls.Add(_hotkeyCheck, 0, 4);
        mainPanel.SetColumnSpan(_hotkeyCheck, 3);

        // --- 开机自启 ---
        _autoStartCheck = new CheckBox
        {
            Text = "开机自启",
            Checked = settings.AutoStart,
            AutoSize = true
        };
        mainPanel.Controls.Add(_autoStartCheck, 0, 5);
        mainPanel.SetColumnSpan(_autoStartCheck, 3);

        // --- 按钮 ---
        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 0)
        };
        var cancelBtn = new Button { Text = "取消", Size = new Size(80, 30) };
        cancelBtn.Click += (_, _) => Close();
        var saveBtn = new Button { Text = "保存", Size = new Size(80, 30) };
        saveBtn.Click += OnSave;
        buttonPanel.Controls.Add(saveBtn);
        buttonPanel.Controls.Add(cancelBtn);
        mainPanel.Controls.Add(buttonPanel, 0, 6);
        mainPanel.SetColumnSpan(buttonPanel, 3);

        Controls.Add(mainPanel);
    }

    private static TrackBar CreateSlider(int min, int max, int value, int tickFreq)
    {
        return new TrackBar
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Clamp(value, min, max),
            TickFrequency = tickFreq,
            Dock = DockStyle.Fill
        };
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _settings.EdgeSensitivityPx = _sensitivitySlider.Value;
        _settings.VisibleEdgePx = _visibleEdgeSlider.Value;
        _settings.AnimationDurationMs = _animationSlider.Value;
        _settings.HideDelayMs = _hideDelaySlider.Value;
        _settings.HotkeyEnabled = _hotkeyCheck.Checked;
        _settings.AutoStart = _autoStartCheck.Checked;

        _settings.Save();
        _onSave(_settings);
        Close();
    }
}
