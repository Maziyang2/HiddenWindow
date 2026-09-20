using System;
using System.Reflection;

namespace HiddenWindow;

/// <summary>应用级常量与版本信息；版本从程序集读取，避免多处硬编码。</summary>
internal static class AppInfo
{
    public const string Name = "HiddenWindow";
    public const string RepositorySlug = "Maziyang2/HiddenWindow";
    public const string WebsiteHost = "github.maziyang.top";
    public const string WebsiteUrl = "https://" + WebsiteHost;

    public static string Version { get; } = ResolveVersion();

    private static string ResolveVersion()
    {
        var assembly = typeof(AppInfo).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            // 去掉 SourceLink 等追加的 +<commit> 后缀
            var plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
