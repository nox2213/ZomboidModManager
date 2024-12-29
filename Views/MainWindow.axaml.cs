using Avalonia.Controls;
using Avalonia.Interactivity;
using Serilog;

namespace ZomboidModManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Log.Information("Anwendung gestartet.");
        }

        private void OnSteamCollectionClick(object? sender, RoutedEventArgs e)
        {
            // Öffnet ein neues Fenster für die Steam Kollektion
            var steamWindow = new SteamCollectionWindow();
            steamWindow.ShowDialog(this); 
        }

        private void OnServerIniClick(object? sender, RoutedEventArgs e)
        {
            // Öffnet ein neues Fenster für Server Ini
            var serverWindow = new ServerIniWindow();
            serverWindow.ShowDialog(this);
        }

        private void OnModLibraryClick(object? sender, RoutedEventArgs e)
        {
            // Öffnet ein neues Fenster für Mod Library
            var modLibraryWindow = new ModLibraryWindow();
            modLibraryWindow.ShowDialog(this);
        }
    }
}
