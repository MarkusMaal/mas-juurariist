using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Markuse_asjade_juurutamise_tööriist;

public partial class MainWindow : Window
{
    string CPU_ID = "";
    public MainWindow()
    {
        InitializeComponent();
    }

    private void UpdateStatusLabelText()
    {
        App? app = ((App?)App.Current);
        if (app == null) return;
        StatusLabelCpuId.Content = $"Protsessori ID: " + app.GetCpuId();
    }

    private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateStatusLabelText();
        for (int i = 0; i < 10; i++)
        {
            LogOutput.Text += "\nTest";
        }
    }
}