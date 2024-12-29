using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;

namespace ZomboidModManager.Services
{
    public static class PythonInstaller
    {
        public static async Task InstallPythonAndPipAsync()
        {
            try
            {
                // Projektbasis ermitteln
                string basePath = AppContext.BaseDirectory;
                string srcFolder = Path.Combine(basePath, "src");

                // Pfade für Python-Installation
                string pythonDir = Path.Combine(srcFolder, "python-3.11.9-embed-amd64");
                string pythonZip = Path.Combine(srcFolder, "python-embed.zip");
                string getPipPath = Path.Combine(srcFolder, "scripts", "get-pip.py");
                string pythonExe = Path.Combine(pythonDir, "python.exe");

                // 1) Python-Ordner prüfen und ggf. herunterladen und entpacken
                if (!Directory.Exists(pythonDir))
                {
                    Log.Information("Python-Verzeichnis nicht vorhanden. Lade python-embed.zip herunter und entpacke es...");

                    await DownloadFileAsync(
                        "https://www.python.org/ftp/python/3.11.9/python-3.11.9-embed-amd64.zip",
                        pythonZip
                    );

                    // Entpacke die Python-Embed-Zip-Datei
                    ZipFile.ExtractToDirectory(pythonZip, pythonDir);
                    Log.Information("Python-Embed erfolgreich entpackt.");

                    // Lösche die Zip-Datei nach erfolgreicher Extraktion
                    if (File.Exists(pythonZip))
                    {
                        File.Delete(pythonZip);
                        Log.Information("Python-Zip-Datei gelöscht: {PythonZip}", pythonZip);
                    }

                    // PTH-Datei anpassen
                    string pthFile = Path.Combine(pythonDir, "python311._pth");
                    if (File.Exists(pthFile))
                    {
                        string pthContent = File.ReadAllText(pthFile);
                        File.WriteAllText(pthFile, pthContent.Replace("#import site", "import site"));
                        Log.Information("Die Datei python311._pth wurde angepasst (import site aktiviert).");
                    }
                    else
                    {
                        throw new FileNotFoundException("Die Datei python311._pth wurde nicht gefunden.");
                    }
                }
                else
                {
                    Log.Debug("Python-Embed-Ordner ist bereits vorhanden, überspringe Download.");
                }

                // 2) Prüfen und get-pip.py herunterladen
                if (!File.Exists(getPipPath))
                {
                    Log.Information("get-pip.py wird heruntergeladen...");
                    await DownloadFileAsync("https://bootstrap.pypa.io/get-pip.py", getPipPath);
                    Log.Information("get-pip.py erfolgreich heruntergeladen.");
                }
                else
                {
                    Log.Debug("get-pip.py ist bereits vorhanden, überspringe Download.");
                }

                // 3) Pip installieren
                Log.Information("Installiere pip mit get-pip.py...");
                RunCommand(pythonExe, $"\"{getPipPath}\"");
                Log.Information("Pip erfolgreich installiert.");

                // 4) Entferne get-pip.py nach der Installation
                if (File.Exists(getPipPath))
                {
                    File.Delete(getPipPath);
                    Log.Information("get-pip.py wurde entfernt.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fehler beim Installieren von Python und Pip.");
                throw;
            }
        }

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

        public static void RunCommand(string exePath, string arguments)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null)
                {
                    throw new InvalidOperationException($"Der Prozess '{exePath}' konnte nicht gestartet werden.");
                }

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        Log.Debug($"OUTPUT: {args.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        Log.Error($"ERROR: {args.Data}");
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                Log.Debug($"Prozess '{exePath}' beendet (Exit Code: {process.ExitCode}).");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Fehler beim Ausführen des Befehls '{exePath}' mit Argumenten '{arguments}'");
                throw;
            }
        }
    }
}
