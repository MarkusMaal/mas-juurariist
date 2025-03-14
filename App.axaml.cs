using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Markuse_asjade_juurutamise_tööriist;

public partial class App : Application
{
    public override void Initialize()
    {
        if (OperatingSystem.IsWindows())
        {
            File.WriteAllBytes(Path.GetTempPath() + "cpuid.exe", Properties.Resources.cpuid_win32);
        }
        else if (OperatingSystem.IsLinux())
        {
            File.WriteAllBytes(Path.GetTempPath() + "cpuid", Properties.Resources.cpuid_linux);
        }
        else if (OperatingSystem.IsMacOS())
        {
            File.WriteAllBytes(Path.GetTempPath() + "cpuid", Properties.Resources.cpuid_darwin);
        }
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(Path.GetTempPath() + "cpuid", UnixFileMode.UserExecute | UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public string GetCpuId()
    {

        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = Path.GetTempPath() + "cpuid" + (OperatingSystem.IsWindows() ? ".exe" : ""),
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process? process = Process.Start(psi);
            if (process == null) return string.Empty;

            string result = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return result;
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}