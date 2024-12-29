using Avalonia.Controls;
using Avalonia.Interactivity;
using Serilog;

namespace ZomboidModManager
{
    public partial class ModLibraryWindow : Window
    {
        public ModLibraryWindow()
        {
            InitializeComponent();
            Log.Information("ModLibraryWindow geöffnet.");
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            Log.Information("ModLibraryWindow geschlossen.");
            Close();
        }
    }
}
