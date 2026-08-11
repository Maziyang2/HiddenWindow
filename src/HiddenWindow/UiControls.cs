using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HiddenWindow;

internal static class UiTheme
{
    public static readonly Color Canvas = Color.FromArgb(12, 12, 13);
    public static readonly Color Surface = Color.FromArgb(20, 20, 22);
    public static readonly Color SurfaceRaised = Color.FromArgb(27, 27, 30);
    public static readonly Color Line = Color.FromArgb(58, 58, 63);
    public static readonly Color LineStrong = Color.FromArgb(92, 92, 98);
    public static readonly Color Text = Color.FromArgb(244, 244, 246);
    public static readonly Color TextMuted = Color.FromArgb(158, 158, 166);
    public static readonly Color TextDim = Color.FromArgb(104, 104, 112);
    public static readonly Color Accent = Color.FromArgb(238, 238, 242);
    public static readonly Color AccentText = Color.FromArgb(13, 13, 15);

    public static Font Font(float size, FontStyle style = FontStyle.Regular) =>
        new("Segoe UI Variable Text", size, style, GraphicsUnit.Point);

    public static Font DisplayFont(float size, FontStyle style = FontStyle.Bold) =>
        new("Segoe UI Variable Display", size, style, GraphicsUnit.Point);

    public static void ApplyDarkTitleBar(Form form)
    {
        if (!OperatingSystem.IsWindows()) return;
        var enabled = 1;
        WinApi.DwmSetWindowAttribute(form.Handle, 20, ref enabled, sizeof(int));
        var cornerPreference = 2;
        WinApi.DwmSetWindowAttribute(form.Handle, 33, ref cornerPreference, sizeof(int));
    }
}

internal sealed class BrandMarkControl : Control
{
    public BrandMarkControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Size = new Size(36, 36);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var scale = Math.Min(ClientSize.Width, ClientSize.Height) / 36f;
        e.Graphics.ScaleTransform(scale, scale);
        using var framePen = new Pen(UiTheme.Text, 2.2f) { LineJoin = LineJoin.Miter };
        using var detailPen = new Pen(UiTheme.TextMuted, 1.4f);
        using var fillBrush = new SolidBrush(UiTheme.Text);

        e.Graphics.DrawRectangle(framePen, 4, 4, 24, 24);
        e.Graphics.DrawLine(detailPen, 10, 10, 23, 10);
        e.Graphics.DrawLine(detailPen, 10, 15, 19, 15);
        e.Graphics.FillRectangle(fillBrush, 22, 19, 10, 10);
        e.Graphics.DrawLine(framePen, 28, 7, 28, 17);
    }
}

internal sealed class ModernCard : Panel
{
    public ModernCard()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        Padding = new Padding(22);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(UiTheme.Line);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}

internal sealed class ModernSlider : Control
{
    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private bool _dragging;

    public event EventHandler? ValueChanged;

    public int Minimum
    {
        get => _minimum;
        set { _minimum = value; Value = _value; }
    }

    public int Maximum
    {
        get => _maximum;
        set { _maximum = Math.Max(value, _minimum + 1); Value = _value; }
    }

    public int Value
    {
        get => _value;
        set
        {
            var next = Math.Clamp(value, Minimum, Maximum);
            if (_value == next) return;
            _value = next;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ModernSlider()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.Selectable, true);
        Height = 30;
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.Slider;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var y = Height / 2;
        var left = 7;
        var right = Math.Max(left + 1, Width - 7);
        var ratio = (Value - Minimum) / (double)(Maximum - Minimum);
        var x = left + (int)Math.Round((right - left) * ratio);

        using var trackPen = new Pen(UiTheme.LineStrong, 2f);
        using var activePen = new Pen(UiTheme.Text, 2f);
        using var knobBrush = new SolidBrush(Focused ? Color.White : UiTheme.Accent);
        e.Graphics.DrawLine(trackPen, left, y, right, y);
        e.Graphics.DrawLine(activePen, left, y, x, y);
        e.Graphics.FillRectangle(knobBrush, x - 5, y - 5, 10, 10);
        if (Focused)
        {
            using var focusPen = new Pen(UiTheme.TextMuted);
            e.Graphics.DrawRectangle(focusPen, x - 8, y - 8, 16, 16);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        Focus();
        _dragging = true;
        Capture = true;
        SetFromX(e.X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging) SetFromX(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
        Capture = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Left or Keys.Down)
        {
            Value--;
            e.Handled = true;
        }
        else if (e.KeyCode is Keys.Right or Keys.Up)
        {
            Value++;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Home)
        {
            Value = Minimum;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.End)
        {
            Value = Maximum;
            e.Handled = true;
        }
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    private void SetFromX(int x)
    {
        var ratio = Math.Clamp((x - 7d) / Math.Max(1, Width - 14d), 0d, 1d);
        Value = Minimum + (int)Math.Round((Maximum - Minimum) * ratio);
    }
}

internal sealed class ModernToggle : CheckBox
{
    public ModernToggle()
    {
        Appearance = Appearance.Button;
        AutoSize = false;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Size = new Size(46, 24);
        Cursor = Cursors.Hand;
        Text = string.Empty;
        AccessibleRole = AccessibleRole.CheckButton;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var track = new Rectangle(1, 3, Width - 2, Height - 6);
        using var trackBrush = new SolidBrush(Checked ? UiTheme.Accent : UiTheme.SurfaceRaised);
        using var trackPen = new Pen(Checked ? UiTheme.Text : UiTheme.LineStrong);
        e.Graphics.FillRectangle(trackBrush, track);
        e.Graphics.DrawRectangle(trackPen, track);
        var knobX = Checked ? Width - 18 : 6;
        using var knobBrush = new SolidBrush(Checked ? UiTheme.AccentText : UiTheme.TextMuted);
        e.Graphics.FillRectangle(knobBrush, knobX, 7, 10, 10);
    }
}

internal sealed class ModernButton : Button
{
    public ModernButton(bool primary)
    {
        AutoSize = false;
        Height = 40;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 1;
        FlatAppearance.BorderColor = primary ? UiTheme.Text : UiTheme.LineStrong;
        BackColor = primary ? UiTheme.Accent : UiTheme.Surface;
        ForeColor = primary ? UiTheme.AccentText : UiTheme.Text;
        Font = UiTheme.Font(9.5f, FontStyle.Bold);
        Cursor = Cursors.Hand;
        UseVisualStyleBackColor = false;
    }
}

internal sealed class HiddenWindowColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => UiTheme.Surface;
    public override Color ImageMarginGradientBegin => UiTheme.Surface;
    public override Color ImageMarginGradientMiddle => UiTheme.Surface;
    public override Color ImageMarginGradientEnd => UiTheme.Surface;
    public override Color MenuItemSelected => UiTheme.SurfaceRaised;
    public override Color MenuItemBorder => UiTheme.LineStrong;
    public override Color MenuBorder => UiTheme.Line;
    public override Color SeparatorDark => UiTheme.Line;
    public override Color SeparatorLight => UiTheme.Line;
}
