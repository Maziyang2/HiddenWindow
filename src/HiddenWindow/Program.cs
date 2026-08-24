using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace HiddenWindow;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // 诊断模式：在真实 Windows 环境中截图设置窗口并导出控件树，用于定位 UI 缺陷
        if (args.Length > 0 && args[0] == "--diag")
            return RunDiagnostics();

        Application.Run(new MainForm());
        return 0;
    }

    private static int RunDiagnostics()
    {
        var outDir = Path.Combine(Directory.GetCurrentDirectory(), "hw-diag");
        Directory.CreateDirectory(outDir);
        var report = new StringBuilder();
        report.AppendLine($"== HiddenWindow runtime diagnostics {DateTime.Now:O} ==");
        report.AppendLine($"OS: {Environment.OSVersion.VersionString}");

        try
        {
            // 图标提取链路验证（PNG 条目 ICO 兼容性）
            try
            {
                using var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                report.AppendLine($"ExtractAssociatedIcon: {(icon == null ? "NULL" : $"{icon.Width}x{icon.Height}")}");
            }
            catch (Exception ex)
            {
                report.AppendLine($"ExtractAssociatedIcon THREW: {ex.GetType().Name}: {ex.Message}");
            }

            var settings = AppSettings.Load();
            Localization.Configure(settings.Language);

            // 托盘菜单结构导出
            var main = new MainForm();
            var buildMenu = typeof(MainForm).GetMethod("BuildMenu", BindingFlags.NonPublic | BindingFlags.Instance);
            if (buildMenu?.Invoke(main, null) is ContextMenuStrip menu)
            {
                report.AppendLine("-- tray menu items --");
                foreach (var item in menu.Items)
                {
                    report.AppendLine(item switch
                    {
                        ToolStripSeparator => "  ---",
                        ToolStripMenuItem mi => $"  [{mi.Text}] visible={mi.Available}",
                        _ => $"  {item}"
                    });
                }
            }
            main.Dispose();

            // 设置窗口截图 + 控件树
            Shoot(() => (Form)new SettingsForm(settings, _ => { }),
                Path.Combine(outDir, "settings.png"), report, "SettingsForm");
            // 关于窗口截图
            Shoot(() => (Form)new AboutForm(),
                Path.Combine(outDir, "about.png"), report, "AboutForm");

            System.IO.File.WriteAllText(Path.Combine(outDir, "report.txt"), report.ToString());
            Console.WriteLine(report.ToString());
            return 0;
        }
        catch (Exception ex)
        {
            report.AppendLine($"FATAL: {ex}");
            try { System.IO.File.WriteAllText(Path.Combine(outDir, "report.txt"), report.ToString()); } catch { }
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void Shoot(Func<Form> create, string pngPath, StringBuilder report, string label)
    {
        var done = new ManualResetEvent(false);
        var thread = new Thread(() =>
        {
            try
            {
                var form = create();
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(60, 60);
                form.Shown += (_, _) =>
                {
                    Thread.Sleep(600); // 等待布局与首帧绘制稳定
                    using var bmp = new Bitmap(form.Width, form.Height);
                    form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                    bmp.Save(pngPath, ImageFormat.Png);
                    report.AppendLine($"-- {label} screenshot {form.Width}x{form.Height} --");
                    DumpTree(form, 0, report);
                };
                // 截图完成后自动关闭消息循环
                var closeTimer = new System.Windows.Forms.Timer { Interval = 2500 };
                closeTimer.Tick += (_, _) => { closeTimer.Stop(); form.Close(); };
                closeTimer.Start();
                Application.Run(form);
            }
            catch (Exception ex)
            {
                report.AppendLine($"{label} THREAD EX: {ex}");
            }
            finally
            {
                done.Set();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!done.WaitOne(20000))
            report.AppendLine($"{label}: TIMEOUT waiting for screenshot thread");
    }

    private static void DumpTree(Control root, int depth, StringBuilder report)
    {
        var pad = new string(' ', depth * 2);
        var clipped = "";
        if (root.Parent is Panel p && root.Bottom > p.ClientSize.Height - p.Padding.Bottom)
            clipped = " <<CLIPPED-BOTTOM>>";
        report.AppendLine($"{pad}{root.GetType().Name} '{root.Text}' bounds={root.Bounds}{clipped}");
        foreach (Control c in root.Controls)
            DumpTree(c, depth + 1, report);
    }
}
