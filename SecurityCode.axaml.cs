using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Markuse_asjade_juurutamise_tööriist;

public partial class SecurityCode : Window
{
    public bool DialogResult = false;
    public SecurityCode()
    {
        InitializeComponent();
    }

    private void ConfirmButton_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}