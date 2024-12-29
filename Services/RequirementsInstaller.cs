using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;

namespace ZomboidModManager.Services
{
    public static class RequirementsInstaller
    {
        /// <summary>
        /// Installiert SteamCMD und weitere Requirements (requirements.txt).
        /// Schreibt Log-Einträge über Serilog.
        /// </summary>
        public static async Task InstallRequirementsAsync()
        {
            try
            {
                // 1) Projektbasis ermitteln
                string basePath = AppContext.BaseDirectory;
                string srcFolder = Path.Combine(basePath, "src");

                // 2) Pfade für SteamCMD
                string steamCmdDir = Path.Combine(srcFolder, "steamcmd");
                string steamCmdPath = Path.Combine(steamCmdDir, "steamcmd.exe");
                string steamCmdZip = Path.Combine(srcFolder, "steamcmd.zip");

                // 3) SteamCMD herunterladen und entpacken, falls noch nicht vorhanden
                if (!File.Exists(steamCmdPath))
                {
                    Log.Information("SteamCMD wird heruntergeladen...");

                    await DownloadFileAsync(
                        "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip",
                        steamCmdZip
                    );

                    try
                    {
                        ZipFile.ExtractToDirectory(steamCmdZip, steamCmdDir);
                        Log.Information("SteamCMD erfolgreich entpackt.");
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Fehler beim Entpacken von SteamCMD: {ex.Message}"
                        );
                    }
                    finally
                    {
                        if (File.Exists(steamCmdZip))
                        {
                            File.Delete(steamCmdZip);
                            Log.Information("SteamCMD-Zip-Datei gelöscht: {SteamCmdZip}", steamCmdZip);
                        }
                    }

                    if (!File.Exists(steamCmdPath))
                    {
                        throw new FileNotFoundException(
                            "SteamCMD konnte nicht heruntergeladen oder entpackt werden."
                        );
                    }
                }
                else
                {
                    Log.Debug("SteamCMD bereits vorhanden, überspringe Download.");
                }

                // 4) Pfade für Python-Ordner und requirements.txt
                string pythonDir = Path.Combine(srcFolder, "python-3.11.9-embed-amd64");
                string pythonExe = Path.Combine(pythonDir, "python.exe");
                string scriptsPath = Path.Combine(srcFolder, "scripts");
                string requirementsTxt = Path.Combine(scriptsPath, "requirements.txt");

                // 5) Existenz der requirements.txt prüfen
                if (!File.Exists(requirementsTxt))
                {
                    throw new FileNotFoundException($"Die Datei '{requirementsTxt}' wurde nicht gefunden.");
                }

                // 6) Installiere Anforderungen via pip
                Log.Information("Installiere Anforderungen aus {FileName} per pip...", requirementsTxt);
                PythonInstaller.RunCommand(pythonExe, $"-m pip install -r \"{requirementsTxt}\"");
                Log.Information("Anforderungen erfolgreich installiert.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fehler beim Installieren der Requirements.");
                throw;
            }
        }

        /// <summary>
        /// Lädt eine Datei von der angegebenen URL herunter.
        /// </summary>
        private static async Task DownloadFileAsync(string url, string destinationPath)
        {
            try
            {
                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    await using var fs = new FileStream(destinationPath, FileMode.Create);
                    await response.Content.CopyToAsync(fs);
                    Log.Information("Datei erfolgreich heruntergeladen: {Url}", url);
                }
                else
                {
                    throw new Exception($"Fehler beim Herunterladen der Datei '{url}': {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fehler beim Herunterladen der Datei von {Url}", url);
                throw;
            }
        }
    }
}
