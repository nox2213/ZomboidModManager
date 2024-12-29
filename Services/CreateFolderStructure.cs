using System;
using System.IO;
using Serilog;

namespace ZomboidModManager.Services
{
    public static class CreateFolderStructure
    {
        /// <summary>
        /// Initializes the folder and file structure required for the application.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                string basePath = AppContext.BaseDirectory;
                
                // Define the folders to create
                string[] folders =
                {
                    Path.Combine(basePath, "src"),
                    Path.Combine(basePath, "src", "WorkshopObjects"),
                    Path.Combine(basePath, "src", "WorkshopObjects", "tempimage"),
                    Path.Combine(basePath, "src", "scripts"),
                    Path.Combine(basePath, "Logs")
                };

                // Define essential files to create
                var files = new (string path, string content)[]
                {
                    (Path.Combine(basePath, "Saves_and_Output", "WorkshopID.txt"), "WorkshopItems="),
                    (Path.Combine(basePath, "src", "scripts", "requirements.txt"), "# Add Python package requirements here")
                };

                // Create folders
                foreach (var folder in folders)
                {
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                        Log.Information("Created folder: {Folder}", folder);
                    }
                    else
                    {
                        Log.Debug("Folder already exists: {Folder}", folder);
                    }
                }

                // Create files with initial content if they don't exist
                foreach (var (path, content) in files)
                {
                    string directory = Path.GetDirectoryName(path) ?? string.Empty;
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                        Log.Information("Created directory for file: {Directory}", directory);
                    }

                    if (!File.Exists(path))
                    {
                        File.WriteAllText(path, content);
                        Log.Information("Created file: {File}", path);
                    }
                    else
                    {
                        Log.Debug("File already exists: {File}", path);
                    }
                }

                Log.Information("Folder structure initialization completed successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize folder structure.");
                throw;
            }
        }
    }
}
