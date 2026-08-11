using System;
using System.Drawing;
using System.Windows.Forms;

namespace HiddenWindow;

internal sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly Action<AppSettings> _onSave;
    private readonly ModernSlider _sensitivitySlider;
    private readonly ModernSlider _visibleEdgeSlider;
    private readonly ModernSlider _animationSlider;
    private readonly ModernSlider _hideDelaySlider;
    private readonly Label _sensitivityValue;
    private readonly Label _visibleEdgeValue;
    private readonly Label _animationValue;
    private readonly Label _hideDelayValue;
    private readonly ModernToggle _hotkeyToggle;
    private readonly ModernToggle _autoStartToggle;
    private readonly ComboBox _languagePicker;

    public SettingsForm(AppSettings settings, Action<AppSettings> onSave)
    {
        _settings = settings;
        _onSave = onSave;

        Text = Localization.Get("settingsTitle");
        ClientSize = new Size(760, 720);
        MinimumSize = new Size(680, 620);
        BackColor = UiTheme.Canvas;
        ForeColor = UiTheme.Text;
        Font = UiTheme.Font(9f);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Canvas,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));

        root.Controls.Add(BuildHeader(), 0, 0);

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = UiTheme.Canvas,
            Padding = new Padding(28, 12, 28, 28),
            Margin = Padding.Empty
        };

        _sensitivitySlider = CreateSlider(10, 100, settings.EdgeSensitivityPx, "edgeSensitivity");
        _visibleEdgeSlider = CreateSlider(2, 15, settings.VisibleEdgePx, "visibleEdge");
        _animationSlider = CreateSlider(80, 600, settings.EffectiveAnimationDurationMs, "animationDuration");
        _hideDelaySlider = CreateSlider(50, 2000, settings.HideDelayMs, "hideDelay");

        _sensitivityValue = CreateValueLabel($"{settings.EdgeSensitivityPx} px");
        _visibleEdgeValue = CreateValueLabel($"{settings.VisibleEdgePx} px");
        _animationValue = CreateValueLabel($"{settings.EffectiveAnimationDurationMs} ms");
        _hideDelayValue = CreateValueLabel($"{settings.HideDelayMs} ms");

        _sensitivitySlider.ValueChanged += (_, _) => _sensitivityValue.Text = $"{_sensitivitySlider.Value} px";
        _visibleEdgeSlider.ValueChanged += (_, _) => _visibleEdgeValue.Text = $"{_visibleEdgeSlider.Value} px";
        _animationSlider.ValueChanged += (_, _) => _animationValue.Text = $"{_animationSlider.Value} ms";
        _hideDelaySlider.ValueChanged += (_, _) => _hideDelayValue.Text = $"{_hideDelaySlider.Value} ms";

        var behaviorCard = new ModernCard
        {
            Width = 682,
            Height = 406,
            Margin = new Padding(0, 0, 0, 16),
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };
        behaviorCard.Controls.Add(BuildSliderStack());
        behaviorCard.Controls.Add(BuildCardHeading("behavior", "behaviorHint"));
        body.Controls.Add(behaviorCard);

        _hotkeyToggle = new ModernToggle { Checked = settings.HotkeyEnabled };
        _hotkeyToggle.AccessibleName = Localization.Get("hotkey");
        _autoStartToggle = new ModernToggle { Checked = settings.AutoStart };
        _autoStartToggle.AccessibleName = Localization.Get("autoStart");

        _languagePicker = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = UiTheme.SurfaceRaised,
            ForeColor = UiTheme.Text,
            FlatStyle = FlatStyle.Flat,
            Font = UiTheme.Font(9.5f),
            Width = 166,
            IntegralHeight = false,
            DropDownHeight = 120,
            AccessibleName = Localization.Get("language")
        };
        _languagePicker.Items.Add(Localization.Get("languageSystem"));
        _languagePicker.Items.Add(Localization.Get("languageChinese"));
        _languagePicker.Items.Add(Localization.Get("languageEnglish"));
        _languagePicker.SelectedIndex = (int)settings.Language;

        var preferencesCard = new ModernCard
        {
            Width = 682,
            Height = 282,
            Margin = new Padding(0),
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };
        preferencesCard.Controls.Add(BuildPreferencesStack());
        preferencesCard.Controls.Add(BuildCardHeading("preferences", "languageHint"));
        body.Controls.Add(preferencesCard);

        root.Controls.Add(body, 0, 1);
        root.Controls.Add(BuildFooter(), 0, 2);
        Controls.Add(root);

        Shown += (_, _) => UiTheme.ApplyDarkTitleBar(this);
        Resize += (_, _) =>
        {
            var width = Math.Max(580, body.ClientSize.Width - body.Padding.Horizontal - 6);
            behaviorCard.Width = width;
            preferencesCard.Width = width;
        };
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Canvas,
            Padding = new Padding(28, 24, 28, 20)
        };
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(UiTheme.Line);
            e.Graphics.DrawLine(pen, 28, header.Height - 1, header.Width - 28, header.Height - 1);
        };

        var mark = new BrandMarkControl { Location = new Point(28, 28), Size = new Size(42, 42) };
        var title = new Label
        {
            AutoSize = true,
            Text = "HIDDENWINDOW",
            ForeColor = UiTheme.Text,
            Font = UiTheme.DisplayFont(17f, FontStyle.Bold),
            Location = new Point(88, 25)
        };
        var subtitle = new Label
        {
            AutoSize = true,
            Text = Localization.Get("brandCaption"),
            ForeColor = UiTheme.TextDim,
            Font = UiTheme.Font(7.5f, FontStyle.Bold),
            Location = new Point(90, 57)
        };
        var status = new Label
        {
            AutoSize = false,
            Text = $"●  {Localization.Get("statusActive")}",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = UiTheme.TextMuted,
            BackColor = UiTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle,
            Font = UiTheme.Font(8f, FontStyle.Bold),
            Size = new Size(112, 32),
            Location = new Point(12, 7)
        };
        var statusHost = new Panel
        {
            Dock = DockStyle.Right,
            Width = 136,
            BackColor = UiTheme.Canvas
        };
        statusHost.Controls.Add(status);

        header.Controls.Add(mark);
        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        header.Controls.Add(statusHost);
        return header;
    }

    private Control BuildCardHeading(string titleKey, string hintKey)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            Padding = new Padding(0, 0, 0, 14),
            BackColor = UiTheme.Surface
        };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(UiTheme.Line);
            e.Graphics.DrawLine(pen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
        };
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = Localization.Get(titleKey),
            ForeColor = UiTheme.Text,
            Font = UiTheme.DisplayFont(13f, FontStyle.Bold),
            Location = new Point(0, 2)
        });
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = Localization.Get(hintKey),
            ForeColor = UiTheme.TextDim,
            Font = UiTheme.Font(8.5f),
            Location = new Point(1, 33)
        });
        return panel;
    }

    private Control BuildSliderStack()
    {
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 70, 0, 0),
            BackColor = UiTheme.Surface,
            ColumnCount = 1,
            RowCount = 4
        };
        for (var i = 0; i < 4; i++)
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        stack.Controls.Add(CreateSliderRow("edgeSensitivity", "edgeSensitivityHint", _sensitivitySlider, _sensitivityValue), 0, 0);
        stack.Controls.Add(CreateSliderRow("visibleEdge", "visibleEdgeHint", _visibleEdgeSlider, _visibleEdgeValue), 0, 1);
        stack.Controls.Add(CreateSliderRow("animationDuration", "animationDurationHint", _animationSlider, _animationValue), 0, 2);
        stack.Controls.Add(CreateSliderRow("hideDelay", "hideDelayHint", _hideDelaySlider, _hideDelayValue), 0, 3);
        return stack;
    }

    private static Control CreateSliderRow(string titleKey, string hintKey, ModernSlider slider, Label valueLabel)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Surface,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 53));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        row.Paint += (_, e) =>
        {
            using var pen = new Pen(UiTheme.Line);
            e.Graphics.DrawLine(pen, 0, row.Height - 1, row.Width, row.Height - 1);
        };
        var copyPanel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
        copyPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = Localization.Get(titleKey),
            ForeColor = UiTheme.Text,
            Font = UiTheme.Font(9.5f, FontStyle.Bold),
            Location = new Point(0, 14)
        });
        copyPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = Localization.Get(hintKey),
            ForeColor = UiTheme.TextDim,
            Font = UiTheme.Font(8f),
            Location = new Point(0, 39)
        });
        slider.Dock = DockStyle.Fill;
        slider.Margin = new Padding(12, 29, 12, 18);
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.Margin = Padding.Empty;
        row.Controls.Add(copyPanel, 0, 0);
        row.Controls.Add(slider, 1, 0);
        row.Controls.Add(valueLabel, 2, 0);
        return row;
    }

    private Control BuildPreferencesStack()
    {
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 70, 0, 0),
            BackColor = UiTheme.Surface,
            ColumnCount = 1,
            RowCount = 3
        };
        for (var i = 0; i < 3; i++) stack.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333f));
        stack.Controls.Add(CreatePreferenceRow("hotkey", "hotkeyHint", _hotkeyToggle), 0, 0);
        stack.Controls.Add(CreatePreferenceRow("autoStart", "autoStartHint", _autoStartToggle), 0, 1);
        stack.Controls.Add(CreatePreferenceRow("language", "languageHint", _languagePicker), 0, 2);
        return stack;
    }

    private static Control CreatePreferenceRow(string titleKey, string hintKey, Control action)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Surface,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        row.Paint += (_, e) =>
        {
            using var pen = new Pen(UiTheme.Line);
            e.Graphics.DrawLine(pen, 0, row.Height - 1, row.Width, row.Height - 1);
        };
        var copyPanel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
        copyPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = Localization.Get(titleKey),
            ForeColor = UiTheme.Text,
            Font = UiTheme.Font(9.5f, FontStyle.Bold),
            Location = new Point(0, 11)
        });
        copyPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = Localization.Get(hintKey),
            ForeColor = UiTheme.TextDim,
            Font = UiTheme.Font(8f),
            Location = new Point(0, 34)
        });
        var actionHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Surface,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, action is ComboBox ? 14 : 17, 0, 0)
        };
        action.Margin = Padding.Empty;
        actionHost.Controls.Add(action);
        row.Controls.Add(copyPanel, 0, 0);
        row.Controls.Add(actionHost, 1, 0);
        return row;
    }

    private Control BuildFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Canvas,
            Padding = new Padding(28, 16, 28, 16)
        };
        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(UiTheme.Line);
            e.Graphics.DrawLine(pen, 28, 0, footer.Width - 28, 0);
        };
        footer.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "v2.0.0  /  github.maziyang.top",
            ForeColor = UiTheme.TextDim,
            Font = UiTheme.Font(8f, FontStyle.Bold),
            Location = new Point(28, 28)
        });

        var save = new ModernButton(primary: true)
        {
            Text = Localization.Get("save"),
            Width = 130
        };
        save.Click += OnSave;
        var cancel = new ModernButton(primary: false)
        {
            Text = Localization.Get("cancel"),
            Width = 94,
            DialogResult = DialogResult.Cancel
        };
        var buttonHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 244,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = UiTheme.Canvas,
            Padding = new Padding(0)
        };
        save.Margin = new Padding(10, 0, 0, 0);
        cancel.Margin = Padding.Empty;
        buttonHost.Controls.Add(save);
        buttonHost.Controls.Add(cancel);
        footer.Controls.Add(buttonHost);
        AcceptButton = save;
        CancelButton = cancel;
        return footer;
    }

    private static ModernSlider CreateSlider(int min, int max, int value, string accessibleKey) => new()
    {
        Minimum = min,
        Maximum = max,
        Value = Math.Clamp(value, min, max),
        AccessibleName = Localization.Get(accessibleKey)
    };

    private static Label CreateValueLabel(string value) => new()
    {
        AutoSize = false,
        Size = new Size(70, 24),
        Text = value,
        TextAlign = ContentAlignment.MiddleRight,
        ForeColor = UiTheme.TextMuted,
        Font = UiTheme.Font(8.5f, FontStyle.Bold)
    };

    private void OnSave(object? sender, EventArgs e)
    {
        _settings.EdgeSensitivityPx = _sensitivitySlider.Value;
        _settings.VisibleEdgePx = _visibleEdgeSlider.Value;
        _settings.AnimationDurationMs = _animationSlider.Value;
        _settings.HideDelayMs = _hideDelaySlider.Value;
        _settings.HotkeyEnabled = _hotkeyToggle.Checked;
        _settings.AutoStart = _autoStartToggle.Checked;
        _settings.Language = (LanguageMode)Math.Max(0, _languagePicker.SelectedIndex);
        _settings.Save();
        Localization.Configure(_settings.Language);
        _onSave(_settings);
        DialogResult = DialogResult.OK;
        Close();
    }
}
