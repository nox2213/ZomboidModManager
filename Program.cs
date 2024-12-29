using Avalonia;
using Serilog;           
using System;

namespace ZomboidModManager
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // 1) Serilog konfigurieren:
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug() // Ab welchem Log-Level wir loggen (Debug, Info, Warn, Error, Fatal)
                .WriteTo.Console()    // Ausgabe in die Konsole
                .WriteTo.File(
                    "Logs/log-.txt",  // Automatische Rotation: log-20241225.txt usw.
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,  // 7 Log-Dateien behalten
                    fileSizeLimitBytes: 10_000_000, // 10 MB pro Datei
                    rollOnFileSizeLimit: true
                )
                .CreateLogger();

            try
            {
                Log.Information("Starte Anwendung...");

                // 2) Avalonia-App starten
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Anwendung hat einen kritischen Fehler und wird beendet!");
                throw;
            }
            finally
            {
                // 3) Beendet alle Serilog-Ressourcen sauber (Dateien schließen etc.)
                Log.CloseAndFlush();
            }
        }

        // Avalonia-Konfiguration
        private static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace(); // Kann stehen bleiben, falls du Avalonia-interne Logs in der Konsole sehen willst
    }
}
