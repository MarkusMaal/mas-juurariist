using Avalonia.Controls;
using HardwareInformation;
using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Linq;

namespace Markuse_asjade_juurutamise_tööriist;

public partial class MainWindow : Window
{
    string CPU_ID = "";
    string Mobo_ID = "";
    string mas_root = "";
    MachineInformation hwinfo = MachineInformationGatherer.GatherInformation(true);
    App? app = ((App?)App.Current);
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
        StatusLabelCpuId.Content = $"Protsessori ID: " + app.GetCpuId();
        StatusLabelBiosId.Content = $"SMBIOS ID: {hwinfo.SmBios.BIOSVersion}";
        StatusLabelMoboId.Content = $"Emaplaadi ID: {Mobo_ID}";
        Verifile1Hash.Content = "Verifile 1.0 räsi: " + q() + "\nVerifile 1.0 olek: " + (Verifile() ? "OK" : "vajab juurutamist");
        Verifile2State.Content = "Verifile 2.0 räsi: " + CalculateSHA256(mas_root + "/verifile2.dat") + "\nVerifile 2.0 olek: BYPASS";

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
        UpdateStatusLabelText();
        for (int i = 0; i < 10; i++)
        {
            LogOutput.Text += "\nTest";
        }
    }
    public bool Verifile()
    {
        string verificatable = q();
        string[] savedstr = File.ReadAllText(mas_root + "/edition.txt", Encoding.GetEncoding(1252)).ToString().Split('\n');
        string sttr = savedstr[savedstr.Length - 1];
        if (verificatable == sttr)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public string q()
    {
        string CPIProcessorID = "CPI0" + ProcessorID().Substring(1);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string SHA1HashOfTrimmedEditionInfo = SHA1Hash(RemoveLinesFromBottomUntilPINAddNewLine(File.ReadAllText(mas_root + "/edition.txt", Encoding.GetEncoding(1252))));

        return (MD5Hash(SHA1HashOfTrimmedEditionInfo + SHA1Hash(CPIProcessorID.Substring(1, CPIProcessorID.Length - 2)
            + CPIProcessorID.Substring(0, 1)
            + CPIProcessorID.Substring(CPIProcessorID.Length - 1, 1))).ToLower() + SHA1Hash(BIOS()).ToLower() + SHA1Hash(BaseboardProduct)).ToLower();
    }

    static string RemoveLinesFromBottomUntilPINAddNewLine(string s)
    {
        string[] sar = s.Split('\n');
        string ns = "";
        for (int i = 0; i < sar.Length - 3; i++)
        {
            ns += sar[i].ToString() + "\n";
        }
        return ns;
    }
    private string BIOS()
    {
        return hwinfo.SmBios.BIOSVersion.ToString() + "\r";
    }

    public string ProcessorID()
    {
        if (app == null) return "";
        return app.GetCpuId();
    }
    public static string SHA1Hash(string z)
    {
        SHA1 cx = SHA1.Create();
        byte[] xx = Encoding.ASCII.GetBytes(z);
        byte[] hash = cx.ComputeHash(xx);

        StringBuilder t = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            t.Append(hash[i].ToString("X2"));
        }
        return t.ToString();
    }
    public static string MD5Hash(string z)
    {
        MD5 cx = MD5.Create();
        byte[] xx = Encoding.ASCII.GetBytes(z);
        byte[] hash = cx.ComputeHash(xx);

        StringBuilder t = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            t.Append(hash[i].ToString("X2"));
        }
        return t.ToString();
    }

    public string BaseboardProduct {
        get
        {
            return "";
        }
    }
}