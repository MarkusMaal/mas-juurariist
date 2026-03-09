using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        ScCode.Focus();
    }

    private void ScCode_OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        DialogResult = true;
        Close();
    }
}