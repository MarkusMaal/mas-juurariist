using Avalonia.Controls;
using HardwareInformation;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Threading;

namespace Markuse_asjade_juurutamise_tööriist;

public partial class MainWindow : Window
{
    string CPU_ID = "";
    string Mobo_ID = "";
    string mas_root = "";
    App? app = ((App?)App.Current);
    private bool unsupported = false;
    Edition edition;
    public MainWindow()
    {
        InitializeComponent();
    }

    private void UpdateStatusLabelText()
    {
        if (app == null) return;
        string[] possible_paths = [
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas",
            Environment.GetEnvironmentVariable("HOMEDRIVE") + "/mas"
        ];
        foreach (string path in possible_paths)
        {
            if (File.Exists(path + "/edition.txt"))
            {
                mas_root = path;
                break;
            }
        }

        edition = new Edition(mas_root + "/edition.txt")
        {
            MasRoot = mas_root
        };
        string cpuid = app.GetCpuId();
        string bios = hwinfo.SmBios.BIOSVersion;
        string verificate = q();
        string verificate2 = CalculateSHA256(mas_root + "/verifile2.dat");
        bool vf1 = Verifile();
        string vf2 = Verifile2();
        Dispatcher.UIThread.Post(() =>
        {
            StatusLabelCpuId.Content = $"Protsessori ID: " + cpuid;
            StatusLabelBiosId.Text = $"SMBIOS ID: {bios}";
            StatusLabelMoboId.Content = $"Emaplaadi ID: {Mobo_ID}";
            Verifile1Hash.Text = "Verifile 1.0 räsi: " + verificate + "\nVerifile 1.0 olek: " +
                                 (vf1 ? "OK" : "Vajab juurutamist");
            Verifile2State.Text = "Verifile 2.0 räsi: " + verificate2 + "\nVerifile 2.0 olek: " + vf2;
            StatusProgress.IsIndeterminate = false;
            StatusLabel.Content = "Valmis";
            MainTabs.IsEnabled = true;
            if (vf1 && (vf2 == "VERIFIED"))
            {
                AppendLog("Markuse asjad on selles seadmes õigesti juurutatud");
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
        });

    }

    public void AppendLog(string log)
    {
        LogOutput.Text += log + "\n";
    }
    static string CalculateSHA256(string filename)
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
    private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var ts = new ThreadStart(UpdateStatusLabelText);
        var t = new Thread(ts);
        t.Start();
        AppendLog("------------------------------------------");
        AppendLog("Markuse asjade juurutamise tööriist");
        AppendLog("------------------------------------------");
    }

}