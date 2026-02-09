using Avalonia.Controls;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Platform;
using System;
using MonitorInputApp.Models; 

namespace MonitorInputApp.Views;

public partial class MainWindow : Window
{
    private TrayIcon? _trayIcon;
    private KeyRegistration _keyRegistration; // Agrega este campo

    public MainWindow()
    {
        InitializeComponent();

        _keyRegistration = new KeyRegistration();
        _keyRegistration.Start(); // Inicia el registro de teclas

        // Crear el icono de la bandeja
        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon("Assets/icon.ico"), // Usa tu propio icono aquí
            ToolTipText = "MonitorInputApp"
        };

        // Mostrar ventana al hacer doble clic en el icono de la bandeja
        _trayIcon.Clicked += (s, e) =>
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        };

        // Ocultar ventana al minimizar
        this.PropertyChanged += (s, e) =>
        {
            if (e.Property == Window.WindowStateProperty && WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
        };
    }

    // Opcional: Cerrar la app desde la bandeja
    protected override void OnClosed(EventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnClosed(e);
    }
}