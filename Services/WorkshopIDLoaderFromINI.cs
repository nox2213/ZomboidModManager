using System;
using System.Collections.Generic;
using System.IO;
using Serilog;

namespace ZomboidModManager.Services
{
    public static class WorkshopIDLoaderFromINI
    {
        private static readonly string OutputFilePath = Path.Combine("Saves_and_Output", "WorkshopID.txt");

        public static void LoadAndSaveWorkshopIDs(string iniPath)
        {
            var workshopIds = ExtractWorkshopIDs(iniPath);

            if (workshopIds.Count == 0)
                return;

            var directory = Path.GetDirectoryName(OutputFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var writer = new StreamWriter(OutputFilePath, false);
            writer.WriteLine("WorkshopItems=" + string.Join(";", workshopIds));
            Log.Information($"Workshop-IDs erfolgreich nach {OutputFilePath} geschrieben.");
        }

        public static List<string> ExtractWorkshopIDs(string iniPath)
        {
            var workshopIds = new List<string>();
            var lines = File.ReadAllLines(iniPath);

            foreach (var line in lines)
            {
                if (line.StartsWith("WorkshopItems=", System.StringComparison.OrdinalIgnoreCase))
                {
                    workshopIds.AddRange(
                        line.Substring("WorkshopItems=".Length)
                            .Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));
                }
            }

            return workshopIds;
        }
    }
}
