using System;
using System.Threading;
using System.Windows.Forms;

namespace HiddenWindow;

internal static class Program
{
    private const string InstanceMutexName = "HiddenWindow.SingleInstance";

    [STAThread]
    private static void Main()
    {
        // 单实例：双开会让两个 DockManager 互相争抢窗口、反复播放动画
        using var mutex = new Mutex(initiallyOwned: true, InstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show(Localization.Get("alreadyRunning"), AppInfo.Name,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ReportError(e.ExceptionObject as Exception);

        Application.Run(new MainForm());
    }

    /// <summary>托盘程序不应静默崩溃：显示未处理异常，并尽量保持进程存活。</summary>
    private static void ReportError(Exception? exception)
    {
        var detail = exception?.Message ?? "Unknown error";
        MessageBox.Show($"{Localization.Get("unexpectedError")}\n\n{detail}", AppInfo.Name,
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
