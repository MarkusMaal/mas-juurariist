using System;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Markuse_asjade_juurutamise_tööriist;

public partial class EditionEditInfoForm : Window
{
    App app = (App)Application.Current;
    string edition = "Pro";
    string version = "0.0";
    string build = "A00000a";
    string name = "Alpha";
    static Random rnd = new Random();
    string pin = rnd.Next(0, 9) + rnd.Next(0, 9).ToString() + rnd.Next(0, 9) + rnd.Next(0, 9);
    string[] features = { "IT", "TS", "MM" };
    public string editionData = "";
    public required string mas_root;
    public bool ReadOnly = false;
    public EditionEditInfoForm()
    {
        InitializeComponent();
    }
    /* Loads mas theme */
    Color[] LoadTheme()
    {
        string[] bgfg = File.ReadAllText(mas_root + "/scheme.cfg").Split(';');
        string[] bgs = bgfg[0].Split(':');
        string[] fgs = bgfg[1].Split(':');
        Color[] cols = { Color.FromArgb(255, byte.Parse(bgs[0]), byte.Parse(bgs[1]), byte.Parse(bgs[2])), Color.FromArgb(255, byte.Parse(fgs[0]), byte.Parse(fgs[1]), byte.Parse(fgs[2])) };
        return cols;
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string s;
        using (var sr = new StreamReader(mas_root + "/edition.txt", Encoding.GetEncoding(1252)))
        {
            s = sr.ReadToEnd();
            sr.Close();
        }
        var attribs = s.Split('\n');
        edition = attribs[1];
        version = attribs[2];
        build = attribs[3];
        pin = attribs[9];
        features = attribs[8].Split('-');
        name = attribs[10];
        ReloadValues();
    }


    private void ReloadValues()
    {
        if (mas_root == "") return;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string s;
        using (var sr = new StreamReader(mas_root + "/edition.txt", Encoding.GetEncoding(1252)))
        {
            s = sr.ReadToEnd();
            sr.Close();
        }
        var attribs = s.Split('\n');
        edition = attribs[1];
        version = attribs[2];
        build = attribs[3];
        pin = attribs[9];
        features = attribs[8].Split('-');
        name = attribs[10];
        if (edition == "Pro")
        {
            EditionBox.SelectedIndex = 1;
        }
        else if (edition == "Ultimate")
        {
            EditionBox.SelectedIndex = 0;
        }
        else if (edition == "Premium")
        {
            EditionBox.SelectedIndex = 2;
        }
        else
        {
            EditionBox.SelectedIndex = 3;
        }
        VersionBox.Text = version;
        BuildBox.Text = build;
        NameBox.Text = name;
        label11.Content = "PIN kood: " + pin + " (automaatne väli)";
        label7.Content = "Juurutaja: " + Environment.UserName + " (automaatne väli)";
        label9.Content = "Tuuma versioon: " + Environment.OSVersion.Version.Major + "." + Environment.OSVersion.Version.Minor + " (automaatne väli)";
        DXBox.IsChecked = false;
        ITBox.IsChecked = false;
        WXBox.IsChecked = false;
        GPBox.IsChecked = false;
        CSBox.IsChecked = false;
        RDBox.IsChecked = false;
        LTBox.IsChecked = false;
        foreach (string element in features)
        {
            if ((element == "DX") || (element == "RM"))
            {
                DXBox.IsChecked = true;
            }
            else if (element == "IT")
            {
                ITBox.IsChecked = true;
            }
            else if (element == "WX")
            {
                WXBox.IsChecked = true;
            }
            else if (element == "GP")
            {
                GPBox.IsChecked = true;
            }
            else if (element == "CS")
            {
                CSBox.IsChecked = true;
            }
            else if (element == "RD")
            {
                RDBox.IsChecked = true;
            }
            else if (element == "LT")
            {
                LTBox.IsChecked = true;
            }
        }

    }

    private void RootClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var feats = "IP-";
            NameBox.Text = NameBox.Text!.Replace("ä", "2").Replace("õ", "?").Replace("ü", "_y_").Replace("ö", "9");
            if (ITBox.IsChecked == true) { feats += "IT-"; }
            if (WXBox.IsChecked == true) { feats += "WX-"; }
            if (GPBox.IsChecked == true) { feats += "GP-"; }
            if (DXBox.IsChecked == true) { feats += "RM-"; }
            if (CSBox.IsChecked == true) { feats += "CS-"; }
            if (RDBox.IsChecked == true) { feats += "RD-"; }
            if (LTBox.IsChecked == true) { feats += "LT-"; }
            if (TSMMBox.IsChecked == true) { feats += "TS-MM"; }
            var rooter = Environment.UserName;
            var win = Environment.OSVersion.Version.Major + "." + Environment.OSVersion.Version.Minor;
            //if (File.Exists("\\mas\\edition.bak")) { File.Delete("\\mas\\edition.bak"); }
            //File.Move("\\mas\\edition.txt", "\\mas\\edition.bak");
            
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (!ReadOnly)
            {
                using (var sw = new StreamWriter(mas_root + "/edition.txt", false, Encoding.GetEncoding(1252)))
                {
                    sw.Write(
                        $"[Edition_info]\n{edition}\n{version}\n{build}\nYes\n{rooter}\net-EE\n{win}\n{feats}\n{pin}\n{name}\n");
                    sw.Close();
                }

                using (var sw = new StreamWriter(mas_root + "/edition_1.txt", false, Encoding.GetEncoding(1252)))
                {
                    sw.Write(
                        $"[Edition_info];{edition};{version};{build};Yes;{rooter};et-EE;{win};{feats};{pin};{name}\n;[this file is for backwards compatibility with legacy programs]");
                    sw.Close();
                }
            }
            else
            {
                editionData =
                    $"[Edition_info]\n{edition}\n{version}\n{build}\nYes\n{rooter}\net-EE\n{win}\n{feats}\n{pin}\n{name}\n";
            }

            Program.RootOk = true;
            Program.RootCancel = false;
            Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Juurutusteabe salvestamine nurjus\n\n{ex.Message}\n\n{ex.StackTrace}");
        }
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        ReloadValues();
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!Program.RootOk)
        {
            Program.RootCancel = true;
        }
    }

    private void BuildBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        build = BuildBox.Text!;
    }
}