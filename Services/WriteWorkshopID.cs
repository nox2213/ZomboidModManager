using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZomboidModManager.Models;
using Serilog;

namespace ZomboidModManager.Services
{
    public static class WriteWorkshopID
    {
        private static readonly string OutputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Saves_and_Output", "WorkshopID.txt");
        private static readonly string WorkshopObjectsPath = Path.Combine("src", "WorkshopObjects");
        private static readonly string TempImagePath = Path.Combine(WorkshopObjectsPath, "tempimage");

        public static async Task WriteAndSyncWorkshopIdsAsync(IEnumerable<string> workshopIds, SteamScraper scraper)
        {
            // Ensure directories exist
            EnsureDirectoryExists(OutputFilePath);
            EnsureDirectoryExists(WorkshopObjectsPath);
            EnsureDirectoryExists(TempImagePath);

            // Load existing IDs and calculate new ones
            var existingIds = LoadExistingIds();
            var newIds = workshopIds.Except(existingIds).ToList();

            // Combine all IDs
            var allIds = existingIds.Union(newIds).ToList();
            File.WriteAllText(OutputFilePath, "WorkshopItems=" + string.Join(";", allIds));

            Log.Information("Added {Count} new IDs to WorkshopID.txt", newIds.Count);

            // Synchronize files
            await SynchronizeWorkshopObjectsAsync(allIds, scraper);

            // Close the SteamCollectionWindow
            CloseSteamCollectionWindow();
        }

        private static void EnsureDirectoryExists(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Log.Information("Created missing directory: {Directory}", directory);
            }
        }

        private static HashSet<string> LoadExistingIds()
        {
            if (!File.Exists(OutputFilePath))
            {
                Log.Warning("WorkshopID.txt not found. Returning empty ID set.");
                return new HashSet<string>();
            }

            var content = File.ReadAllText(OutputFilePath);
            var ids = content.Replace("WorkshopItems=", "").Split(';', StringSplitOptions.RemoveEmptyEntries);
            return new HashSet<string>(ids);
        }

        private static async Task SynchronizeWorkshopObjectsAsync(List<string> validIds, SteamScraper scraper)
        {
            // Synchronize JSON files
            var jsonFiles = Directory.GetFiles(WorkshopObjectsPath, "*.json");
            foreach (var jsonFile in jsonFiles)
            {
                var id = Path.GetFileNameWithoutExtension(jsonFile);
                if (!validIds.Contains(id))
                {
                    File.Delete(jsonFile);
                    Log.Information("Deleted orphaned JSON file: {File}", jsonFile);
                }
            }

            // Synchronize PNG files
            var pngFiles = Directory.GetFiles(TempImagePath, "*.png");
            foreach (var pngFile in pngFiles)
            {
                var id = Path.GetFileNameWithoutExtension(pngFile);
                if (!validIds.Contains(id))
                {
                    File.Delete(pngFile);
                    Log.Information("Deleted orphaned PNG file: {File}", pngFile);
                }
            }

            // Check for mismatched files
            var missingJsonIds = validIds.Where(id => !File.Exists(Path.Combine(WorkshopObjectsPath, id + ".json"))).ToList();
            var missingPngIds = validIds.Where(id => !File.Exists(Path.Combine(TempImagePath, id + ".png"))).ToList();

            if (missingJsonIds.Any() || missingPngIds.Any())
            {
                Log.Warning("Mismatch detected: Missing JSONs: {JsonIds}, Missing PNGs: {PngIds}", string.Join(", ", missingJsonIds), string.Join(", ", missingPngIds));
            }

            // Fetch missing data using the scraper
            var idsToFetch = missingJsonIds.Union(missingPngIds).Distinct().ToList();
            if (idsToFetch.Any())
            {
                Log.Information("Fetching data for {Count} missing IDs.", idsToFetch.Count);
                var scrapedMods = await scraper.ScrapeCollectionAsync(idsToFetch);

                foreach (var mod in scrapedMods)
                {
                    await SaveModDataAsync(mod);
                }
            }
        }

        private static async Task SaveModDataAsync(SteamModItem mod)
        {
            // Save JSON
            var jsonPath = Path.Combine(WorkshopObjectsPath, mod.Id + ".json");
            File.WriteAllText(jsonPath, mod.ToJson());
            Log.Information("Saved JSON file: {Path}", jsonPath);

            // Download and save image
            await mod.DownloadImageAsync();
            Log.Information("Downloaded image for mod ID: {Id}", mod.Id);
        }

        private static void CloseSteamCollectionWindow()
        {
            Log.Information("Closing SteamCollectionWindow.");
            // Logic to close the window; requires integration with UI context.
            // This method needs to be implemented in the UI code.
        }
    }
}
