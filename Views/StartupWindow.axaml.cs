using System; // Für AppContext
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Controls;
using ZomboidModManager.Services;
using Serilog; 

namespace ZomboidModManager
{
    public partial class StartupWindow : Window
    {
        public StartupWindow()
        {
            InitializeComponent();
            RunSetupAsync();
        }

        private async void RunSetupAsync()
        {
            try
            {
                // Schritt 0: Verzeichnisstruktur erstellen
                UpdateStatus("Erstelle Verzeichnisstruktur...", 0);
                Log.Information("Starte Erstellung der Verzeichnisstruktur...");
                CreateFolderStructure.Initialize();

                // Schritt 1: Python und Pip installieren
                UpdateStatus("Installiere Python und Pip...", 1);
                Log.Information("Starte Installation von Python + Pip...");
                await PythonInstaller.InstallPythonAndPipAsync();

                // Schritt 2: Anforderungen installieren (SteamCMD, etc.)
                UpdateStatus("Installiere Anforderungen (SteamCMD)...", 2);
                Log.Information("Starte Installation von Requirements (SteamCMD)...");
                await RequirementsInstaller.InstallRequirementsAsync();

                // Setup abgeschlossen
                UpdateStatus("Setup abgeschlossen. Starte Anwendung...", 2);
                Log.Information("Setup erfolgreich abgeschlossen – Starte Hauptfenster.");

                // Hauptfenster öffnen
                await Task.Delay(1000);
                new MainWindow().Show();
                Close();
            }
            catch (Exception ex)
            {
                // Fehler anzeigen und loggen
                UpdateStatus($"Fehler: {ex.Message}", 0);
                Log.Error(ex, "Fehler beim Setup-Prozess");
            }
        }

        private void UpdateStatus(string message, int progress)
        {
            // GUI-Update via Dispatcher
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var statusText = this.FindControl<TextBlock>("StatusText");
                if (statusText != null)
                {
                    statusText.Text = message;
                }

                var progressBar = this.FindControl<ProgressBar>("ProgressBar");
                if (progressBar != null)
                {
                    progressBar.Value = progress;
                }
            });
        }
    }
}