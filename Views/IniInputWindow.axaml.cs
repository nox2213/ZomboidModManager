using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZomboidModManager.Services;

namespace ZomboidModManager
{
    public partial class IniInputWindow : Window
    {
        private readonly List<string> _workshopIds;
        private readonly string _iniPath;

        // Parameterless constructor for Avalonia runtime
        public IniInputWindow()
        {
            InitializeComponent();
            _workshopIds = new List<string>();
            _iniPath = string.Empty;
        }

        public IniInputWindow(List<string> workshopIds, string iniPath, Window? owner = null)
        {
            InitializeComponent();
            Owner = owner;
            _workshopIds = workshopIds ?? new List<string>();
            _iniPath = iniPath ?? string.Empty;

            var infoTextBlock = this.FindControl<TextBlock>("InfoTextBlock");
            if (infoTextBlock != null)
            {
                infoTextBlock.Text =
                    $"{_workshopIds.Count} Workshop-IDs gefunden.\n" +
                    "Wenn du fortfährst, werden alle gespeicherten IDs überschrieben.";
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    private async void OnConfirmClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Clear JSON and PNG files from respective directories
        DirectoryCleanupService.ClearJsonFiles("src/WorkshopObjects");
        DirectoryCleanupService.ClearPngFiles("src/WorkshopObjects/tempimage");

        // Show loading screen
        var loadingWindow = new LoadingWindow();
        loadingWindow.Topmost = true; // Ensure it stays on top
        loadingWindow.LinkToParent(this);
        loadingWindow.Show();

        try
        {
            // Close current window
            Hide();

            // Scrape workshop items
            await ScrapeWorkshopItemsAsync();
        }
        catch (Exception ex)
        {
            // Log and handle errors
            Serilog.Log.Error(ex, "Error during scraping.");
        }
        finally
        {
            // Close loading screen
            loadingWindow.Close();

            // Ensure the main window stays active if applicable
            CloseAllWindowsExceptMainWindow();

            // Finally, close this window
            Close();
        }
    }

        private void CloseAllWindowsExceptMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                var windowsToClose = lifetime.Windows.Where(w => w is not MainWindow).ToList();
                foreach (var window in windowsToClose)
                {
                    window.Close();
                }
            }
        }

        private async Task ScrapeWorkshopItemsAsync()
        {
            var scraper = new SteamScraper();
            foreach (var workshopId in _workshopIds)
            {
                var details = await scraper.ScrapeWorkshopItemAsync(workshopId);
                if (details != null)
                {
                    // Save details to JSON
                    var jsonPath = Path.Combine("src", "WorkshopObjects", $"{workshopId}.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(jsonPath) ?? string.Empty);
                    File.WriteAllText(jsonPath, details.ToJson());

                    // Download image
                    await details.DownloadImageAsync();
                }
            }
        }

        private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
    }
}
