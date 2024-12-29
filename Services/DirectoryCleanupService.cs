using System;
using System.IO;
using Serilog;

namespace ZomboidModManager.Services
{
    public static class DirectoryCleanupService
    {
        /// <summary>
        /// Clears all JSON files from the specified directory.
        /// </summary>
        /// <param name="relativeDirectoryPath">The relative path to the directory.</param>
        public static void ClearJsonFiles(string relativeDirectoryPath)
        {
            var directoryPath = Path.Combine(AppContext.BaseDirectory, relativeDirectoryPath);

            if (Directory.Exists(directoryPath))
            {
                var jsonFiles = Directory.GetFiles(directoryPath, "*.json");
                foreach (var file in jsonFiles)
                {
                    try
                    {
                        File.Delete(file);
                        Log.Information("Deleted JSON file: {File}", file);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to delete JSON file: {File}", file);
                    }
                }
            }
            else
            {
                Log.Warning("Directory does not exist: {DirectoryPath}", directoryPath);
            }
        }

        /// <summary>
        /// Clears all PNG files from the specified directory.
        /// </summary>
        /// <param name="relativeDirectoryPath">The relative path to the directory.</param>
        public static void ClearPngFiles(string relativeDirectoryPath)
        {
            var directoryPath = Path.Combine(AppContext.BaseDirectory, relativeDirectoryPath);

            if (Directory.Exists(directoryPath))
            {
                var pngFiles = Directory.GetFiles(directoryPath, "*.png");
                foreach (var file in pngFiles)
                {
                    try
                    {
                        File.Delete(file);
                        Log.Information("Deleted PNG file: {File}", file);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to delete PNG file: {File}", file);
                    }
                }
            }
            else
            {
                Log.Warning("Directory does not exist: {DirectoryPath}", directoryPath);
            }
        }
    }
}
