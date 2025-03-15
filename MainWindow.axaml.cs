using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Markuse_asjade_juurutamise_tööriist;

public partial class MainWindow : Window
{
    private string _moboId = "";
    private string _masRoot = "";
    private readonly App? _app = ((App?)Application.Current);
    private Edition? _edition;
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets Verifile edition info
    /// </summary>
    private void UpdateStatusLabelText()
    {
        if (_app == null) return;
        string[] possiblePaths = [
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas",
            Environment.GetEnvironmentVariable("HOMEDRIVE") + "/mas"
        ];
        foreach (var path in possiblePaths)
        {
            if (!File.Exists(path + "/edition.txt")) continue;
            _masRoot = path;
            break;
        }

        _edition = new Edition(_masRoot + "/edition.txt")
        {
            MasRoot = _masRoot
        };
        var cpuid = _app.GetCpuId();
        var bios = _edition.GetBios();
        var verificate = _edition.q();
        var verificate2 = CalculateSha256(_masRoot + "/verifile2.dat");
        var vf1 = _edition.Verifile();
        var vf2 = _edition.Verifile2();
        _moboId = _moboId != "" ? _moboId : "N/A";
        Dispatcher.UIThread.Post(() =>
        {
            StatusLabelCpuId.Content = $"Protsessori ID: " + cpuid;
            StatusLabelBiosId.Text = $"SMBIOS ID: {bios}";
            StatusLabelMoboId.Content = $"Emaplaadi ID: {_moboId}";
            Verifile1Hash.Text = "Verifile 1.0 räsi: " + verificate + "\nVerifile 1.0 olek: " +
                                 (vf1 ? "OK" : !_edition.Unsupported ? "Vajab juurutamist" : "N/A");
            Verifile2State.Text = "Verifile 2.0 räsi: " + verificate2 + "\nVerifile 2.0 olek: " + vf2;
            Unlock();
            ClearLog();
            if (vf1 && (vf2 == "VERIFIED"))
            {
                AppendLog("Markuse asjad on selles seadmes õigesti juurutatud");
            }
            else if ((vf2 == "VERIFIED") && (_edition.Unsupported))
            {
                AppendLog("Markuse asjad on selles seadmes õigesti juurutatud. See seade ei toeta protsessori arhitektuuri tõttu vanemaid Markuse asjade programme.");   
            }
            else switch (vf2)
            {
                case "FAILED":
                    AppendLog("Püsivuskontrolli käivitamine nurjus. Olge kindlad, et teie arvutisse oleks paigaldatud Java 21 või hilisem versioon.");
                    break;
                case "TAMPERED":
                    AppendLog("Räsi ei vasta riistvara ja väljaande konfiguratsioonile. Protsessori ja/või emaplaadi väljavahetamisel tuleb Markuse asjad uuesti juurutada. Juhul kui muutsite käsitsi edition.txt sisu, võtke kõik enda muudatused tagasi.");
                    break;
                default:
                {
                    switch (vf1)
                    {
                        case false when (vf2 == "FOREIGN"):
                            AppendLog("Selles arvutis ei ole Markuse asjad süsteemi tarkvara");
                            break;
                        case true when (vf2 == "LEGACY"):
                            AppendLog("Selles arvutis on pärandversioon Markuse asjadest. Soovitav on seade juurutada Verifile 2.0 räsiga.");
                            break;
                        case false:
                            AppendLog("Soovitatav on lisada sobiv Verifile 1.0 räsi, et vanemad Markuse asjade programmid toimiksid õigesti.");
                            break;
                    }

                    break;
                }
            }

            EditionDetails.Text = _edition.ToString();
        });

    }

    /// <summary>
    /// Display a log message (only run from UI thread)
    /// </summary>
    /// <param name="log">Line to log</param>
    public void AppendLog(string log)
    {
        LogOutput.Text += log + "\n";
        LogScroll.ScrollToEnd();
    }
    
    /// <summary>
    /// Calculates SHA256 hash from a binary file
    /// </summary>
    /// <param name="filename">Full path to the file</param>
    /// <returns>Hexadecimal representation of the SHA256 hash (lower case invariant)</returns>
    private static string CalculateSha256(string filename)
    {
        using (var sha256 = SHA256.Create())
        {
            using (var stream = File.OpenRead(filename))
            {
                var hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    /// <summary>
    /// Clears log output
    /// </summary>
    private void ClearLog()
    {
        LogOutput.Text = "";
        AppendLog("------------------------------------------");
        AppendLog("Markuse asjade juurutamise tööriist");
        AppendLog("------------------------------------------");
    }
    
    private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var ts = new ThreadStart(UpdateStatusLabelText);
        var t = new Thread(ts);
        t.Start();
        ClearLog();
    }

    private void ADB_Buttons_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        
        Dictionary<string, string> tips = new()
        {
            {"Taaskäivita", "Taaskäivitab seadme"},
            {"Kuva ühendatud ADB seadmed", "Kuvab ühendatud USB silumisega ühendatud seadmed ning nende oleku"},
            {"Uuenda taustapildid", "Kopeerib uusimad taustapildid teie seadmesse ja rakendab need (vajalik superkasutaja)"},
            {"Taaskäivita alglaadurisse", "Taaskäivitab seadme alglaadimisrežiimi, kus on võimalik välgutada süsteemitõmmiseid, modifitseerida partitsioone jms"},
            {"Taaskäivita taastekeskkonda", "Taaskäivitab seadme taastekeskkonda, kus on võimalik läbida erinevaid taastetoiminguid"},
            {"Taaskäivita süsteemi UI", "Sulgeb ja avab uuesti Android süsteemi kasutajaliidese"},
        };
        var content = ((Button?)sender)?.Content;
        if (content == null) return;
        var helpTip = tips[content?.ToString()!];
        ExplainADB.Text = helpTip;
    }

    private void ADB_Buttons_OnPointerExited(object? sender, PointerEventArgs e)
    {
        ExplainADB.Text = "Liikuge hiirekursoriga soovitud nupu peale, et lisainfot saada.";
    }

    private void AboutButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ClearLog();
        AppendLog("Versioon 1.0");
        AppendLog("Võimaldab Verifile 1.0 ja 2.0 räsisid genereerida");
        AppendLog("Autor: Markus Maal");
        AppendLog("Kuupäev: 15.03.2025");
        AppendLog("\n(c) 2025 Kõik õigused on kaitstud");
    }

    private async void CopyLogButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await Clipboard!.SetTextAsync(LogOutput.Text);
    }
    
    private void Relock(string msg)
    {
        StatusProgress.IsIndeterminate = true;
        StatusLabel.Content = msg;
        MainTabs.IsEnabled = false;
    }

    private void Unlock()
    {
        StatusProgress.IsIndeterminate = false;
        StatusLabel.Content = "Valmis";
        MainTabs.IsEnabled = true;
    }

    private void RunAdbDevices()
    {
        var p = new Process()
        {
            StartInfo =
            {
                FileName = "adb",
                Arguments = "devices",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            }
        };
        p.Start();
        while (!p.StandardOutput.EndOfStream)
        {
            var line = p.StandardOutput.ReadLine();
            Dispatcher.UIThread.Post(() => AppendLog(line!));
        }
        Dispatcher.UIThread.Post(Unlock);
    }

    private string RunAdbShellCmd(string cmd)
    {
        Dispatcher.UIThread.Post(() => AppendLog("-> adb shell \"" + cmd.Replace("\"", "\\\"") + "\""));
        var p = new Process()
        {
            StartInfo =
            {
                FileName = "adb",
                Arguments = "shell \"" + cmd.Replace("\"", "\\\"") + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            }
        };
        p.Start();
        string _out = "";
        while (!p.StandardOutput.EndOfStream)
        {
            var line = p.StandardOutput.ReadLine();
            _out += line + "\n";
            Dispatcher.UIThread.Post(() => AppendLog(line!));
        }

        return _out;
    }

    private void AdbWaitForDevice()
    {
        Dispatcher.UIThread.Post(() => AppendLog("-> adb wait-for-device"));
        var p = new Process()
        {
            StartInfo =
            {
                FileName = "adb",
                Arguments = "wait-for-device",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            }
        };
        p.Start();
        while (!p.StandardOutput.EndOfStream)
        {
            var line = p.StandardOutput.ReadLine();
            Dispatcher.UIThread.Post(() => AppendLog(line!));
        }
    }

    private void AdbPush(string source, string destination)
    {
        Dispatcher.UIThread.Post(() => AppendLog("-> adb push \"" + source + "\" " + "\"" + destination + "\""));
        var p = new Process()
        {
            StartInfo =
            {
                FileName = "adb",
                Arguments = "push \"" + source + "\" " + "\"" + destination + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        p.Start();
        while (!p.StandardOutput.EndOfStream)
        {
            var line = p.StandardOutput.ReadLine();
            Dispatcher.UIThread.Post(() => AppendLog(line!));
        }
    }

    private void RunWallpaperUpdate()
    {
        AdbWaitForDevice();
        string devType = "unknown";
        string characteristics = RunAdbShellCmd("getprop ro.build.characteristics").TrimEnd();
        if (characteristics.Contains("tablet"))
        {
            devType = "tablet";
        } else if (characteristics.Contains("phone"))
        {
            devType = "mobile";
        }
        else
        {
            string[] size = RunAdbShellCmd("wm size").TrimEnd().Split(": ")[1].Split("x");
            var density = int.Parse(RunAdbShellCmd("wm density").TrimEnd().Split(": ")[1]);
            double diag_pixels = Math.Sqrt(Math.Pow((double)int.Parse(size[0]), 2) * Math.Pow((double)int.Parse(size[1]), 2));
            double diag_inches = diag_pixels / density;
            if (diag_inches / 1000 > 7.0)
            {
                devType = "tablet";
            }
            else
            {
                devType = "mobile";
            }
        }

        if (File.Exists(_masRoot + $"/bg_{devType}.png") && File.Exists(_masRoot + $"/bg_{devType}_lock.png"))
        {
            AdbPush(_masRoot + $"/bg_{devType}.png", $"/sdcard/Pictures/bg_{devType}.png");
            AdbPush(_masRoot + $"/bg_{devType}_lock.png", $"/sdcard/Pictures/bg_{devType}_lock.png");
            _ = RunAdbShellCmd($"su -c cp /sdcard/Pictures/bg_{devType}.png /data/system/users/0/wallpaper");
            _ = RunAdbShellCmd($"su -c cp /sdcard/Pictures/bg_{devType}.png /data/system/users/0/wallpaper_orig");
            _ = RunAdbShellCmd($"su -c cp /sdcard/Pictures/bg_{devType}_lock.png /data/system/users/0/wallpaper_lock");
            _ = RunAdbShellCmd(
                $"su -c cp /sdcard/Pictures/bg_{devType}_lock.png /data/system/users/0/wallpaper_lock_orig");
            _ = RunAdbShellCmd("su -c killall com.android.systemui");
        }

        Dispatcher.UIThread.Post(Unlock);
    }

    private void RunReboot(string type)
    {
        AdbWaitForDevice();
        _ = RunAdbShellCmd("reboot " + type);
        Dispatcher.UIThread.Post(Unlock);
    }
    private void AdbDevices_OnClick(object? sender, RoutedEventArgs e)
    {
        ClearLog();
        AppendLog("-> adb devices\n");
        Relock("Seadmete tuvastamine...");
        var ts = new ThreadStart(RunAdbDevices);
        var t = new Thread(ts);
        t.Start();
    }

    private void AdbWallpaperUpdate_OnClick(object? sender, RoutedEventArgs e)
    {
        ClearLog();
        Relock("Taustapildi uuendamine...");
        var ts = new ThreadStart(RunWallpaperUpdate);
        var t = new Thread(ts);
        t.Start();
    }

    private void AdbRebootFastboot_OnClick(object? sender, RoutedEventArgs e)
    {
        ClearLog();
        Relock("Alglaadurisse käivitamine...");
        var t = new Thread(() => RunReboot("bootloader"));
        t.Start();
    }

    private void AdbRebootSystem_OnClick(object? sender, RoutedEventArgs e)
    {
        ClearLog();
        Relock("Süsteemi taaskäivitamine...");
        var t = new Thread(() => RunReboot("system"));
        t.Start();
    }

    private void AdbRebootRecovery_OnClick(object? sender, RoutedEventArgs e)
    {
        ClearLog();
        Relock("Taastekeskkonna käivitamine...");
        var t = new Thread(() => RunReboot("recovery"));
        t.Start();
    }

    private void RebootSystemUI()
    {
        AdbWaitForDevice();
        RunAdbShellCmd("su -c killall com.android.systemui");
        Dispatcher.UIThread.Post(Unlock);
    }

    private void AdbSystemUiReboot_OnClick(object? sender, RoutedEventArgs e)
    {
        ClearLog();
        Relock("Süsteemi UI taaskäivitamine...");
        var t = new Thread(RebootSystemUI);
        t.Start();
    }

    private void DeployTestsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ClearLog();
        Relock("Juurutuseelsete testide läbimine...");
        new Thread(() =>
        {
            string testLog = _edition!.PerformTests();
            Dispatcher.UIThread.Post(() => {
                AppendLog(testLog);
                Unlock();
                if (LogOutput.Text!.Contains("FAIL")) return;
                FixVf1Button.IsEnabled = true;
                FixVf2Button.IsEnabled = true;
                DeployNewButton.IsEnabled = true;
            });
            
        }).Start();
    }

    private void FixVf1Button_OnClick(object? sender, RoutedEventArgs e)
    {
        ClearLog();
        Relock("Juurutamine...");
        new Thread(() =>
        {
            if (_edition == null) return;
            _edition.Reverificate();
            Dispatcher.UIThread.Post(() => AppendLog("Genereeritud räsi: " + _edition.Hash));
            var vf2 = _edition.Verifile2();
            Dispatcher.UIThread.Post(() => AppendLog("Verifile 2 olek: " + vf2));
            if (vf2 != "VERIFIED")
            {
                Dispatcher.UIThread.Post(() =>
                {
                    AppendLog("Verifile 2.0 kontroll nurjus. Peate arvuti nullist juurutama.");
                    Unlock();
                });
                return;
            }
            _edition.UnlockVF2();
            Dispatcher.UIThread.Post(() => AppendLog("Verifile 2.0 avatud"));
            // sleep to avoid race conditions
            Thread.Sleep(5000);
            _edition.SaveEditionInfo();
            Dispatcher.UIThread.Post(() => AppendLog("Väljaande info salvestatud"));
            _edition.RelockVF2();
            Dispatcher.UIThread.Post(() => AppendLog("Juurutamine õnnestus!"));
            Dispatcher.UIThread.Post(Unlock);
        }).Start();
    }
}