using Avalonia.Controls;
using System;
using System.Collections.Generic;
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
            StatusProgress.IsIndeterminate = false;
            StatusLabel.Content = "Valmis";
            MainTabs.IsEnabled = true;
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
}