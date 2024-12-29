using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;
using System.Linq;
using ZomboidModManager.Models;
using ZomboidModManager.Services;

namespace ZomboidModManager
{
    public partial class SteamCollectionWindow : Window
    {
        public ObservableCollection<SteamCollectionItem> LeftMods { get; } = new();
        public ObservableCollection<SteamCollectionItem> RightMods { get; } = new();

        private readonly CollectionScraper _scraper = new();
        private HashSet<string> _existingWorkshopIds = new();

        public SteamCollectionWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Find controls dynamically
            LeftListBox = this.FindControl<ListBox>("LeftListBox");
            RightListBox = this.FindControl<ListBox>("RightListBox");

            if (LeftListBox == null)
            {
                Log.Error("LeftListBox not found in XAML.");
            }
            else
            {
                LeftListBox.SelectionChanged += OnListBoxSelectionChanged;
            }

            if (RightListBox == null)
            {
                Log.Error("RightListBox not found in XAML.");
            }
            else
            {
                RightListBox.SelectionChanged += OnListBoxSelectionChanged;
            }

            LoadExistingWorkshopIds();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        private void OnListBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender == LeftListBox && RightListBox.SelectedItem != null)
        {
            RightListBox.SelectedItem = null;
        }
        else if (sender == RightListBox && LeftListBox.SelectedItem != null)
        {
            LeftListBox.SelectedItem = null;
        }
    }
        private void LoadExistingWorkshopIds()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Saves_and_Output", "WorkshopID.txt");
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                var ids = content.Replace("WorkshopItems=", "").Split(';');
                _existingWorkshopIds = new HashSet<string>(ids.Where(id => !string.IsNullOrWhiteSpace(id)));
            }
        }

        public async void OnOkClick(object sender, RoutedEventArgs e)
        {
            Log.Information("OnOkClick triggered.");

            var urlTextBox = this.FindControl<TextBox>("UrlTextBox");
            if (urlTextBox == null)
            {
                Log.Error("TextBox 'UrlTextBox' was not found in the XAML.");
                ShowError("URL input box not found.");
                return;
            }

            string collectionUrl = urlTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(collectionUrl))
            {
                Log.Warning("No URL entered for scraping.");
                ShowError("Please enter a URL.");
                return;
            }

            // Display the "Scraping in Process" dialog
            var loadingDialog = CreateLoadingDialog("Scraping in Process");
            Log.Information("Displaying loading dialog.");
            loadingDialog.Show();

            try
            {
                Log.Information("Starting scrape for collection URL: {CollectionUrl}", collectionUrl);

                var mods = await _scraper.ScrapeCollectionAsync(collectionUrl);

                Log.Information("Successfully scraped {Count} mods from collection.", mods.Count);

                // Close the loading dialog
                loadingDialog.Close();
                Log.Information("Loading dialog closed.");

                LeftMods.Clear();

                var rightModIds = new HashSet<string>(RightMods.Select(mod => mod.Id));
                bool hasYellowMods = false;

                foreach (var mod in mods)
                {
                    Log.Debug("Processing mod with ID: {ModId}", mod.Id);

                    // Check if the mod exists in WorkshopID.txt
                    bool isInWorkshopFile = _existingWorkshopIds.Contains(mod.Id);

                    if (rightModIds.Contains(mod.Id) && !isInWorkshopFile)
                    {
                        Log.Information("Mod {ModId} exists in the right list but is not in WorkshopID.txt. Updating background to yellow.", mod.Id);

                        // Remove the old mod from the right list
                        var existingMod = RightMods.FirstOrDefault(m => m.Id == mod.Id);
                        if (existingMod != null)
                        {
                            Log.Debug("Removing existing mod with ID: {ModId} from right list.", existingMod.Id);
                            RightMods.Remove(existingMod);
                        }

                        // Re-add the mod to the right list with a yellow background
                        mod.BackgroundColor = "Yellow";
                        RightMods.Add(mod);
                        hasYellowMods = true;

                        // Skip adding to the left list
                        continue;
                    }

                    if (isInWorkshopFile)
                    {
                        Log.Information("Mod {ModId} is in WorkshopID.txt. Marking background as green.", mod.Id);
                        mod.BackgroundColor = "LightGreen";
                        if (!rightModIds.Contains(mod.Id))
                        {
                            Log.Debug("Adding mod {ModId} to the right list.", mod.Id);
                            RightMods.Add(mod);
                        }
                        continue;
                    }

                    // Add new mod to the left list
                    Log.Information("Adding new mod {ModId} to the left list.", mod.Id);
                    mod.BackgroundColor = "Transparent";
                    LeftMods.Add(mod);
                }

                if (hasYellowMods)
                {
                    Log.Information("One or more mods were reloaded into the right list and marked with a yellow background.");
                    ShowInfo("One or more Mods were reloaded into the right list and marked with a yellow background.");
                }

                Log.Information("Mod processing completed successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while scraping the collection.");
                ShowError("An error occurred while scraping the collection.");
            }
            finally
            {
                // Ensure the loading dialog is closed in case of an error
                if (loadingDialog.IsVisible)
                {
                    Log.Information("Ensuring loading dialog is closed.");
                    loadingDialog.Close();
                }
            }
        }

        private Window CreateLoadingDialog(string message)
        {
            return new Window
            {
                Title = "Loading",
                Width = 300,
                Height = 100,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                        }
                    }
                }
            };
        }
        public void OnWorkshopLinkClick(object sender, PointerPressedEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.Tag is string workshopLink)
            {
                try
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = workshopLink,
                        UseShellExecute = true
                    };
                    Process.Start(processStartInfo);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Fehler beim Öffnen des Links: {workshopLink}");
                }
            }
        }

        public void OnMoveSelectedClick(object sender, RoutedEventArgs e)
{
    Log.Information("OnMoveSelectedClick triggered.");

    // Verwende das aktuell ausgewählte Element der linken Liste
    var selectedItem = LeftListBox?.SelectedItem as SteamCollectionItem;

    if (selectedItem != null)
    {
        Log.Debug("Attempting to move selected mod with ID: {ModId} to the right list.", selectedItem.Id);

        LeftMods.Remove(selectedItem);
        RightMods.Add(selectedItem);

        Log.Information("Mod with ID: {ModId} successfully moved to the right list.", selectedItem.Id);
    }
    else
    {
        Log.Warning("No mod selected in the left list to move.");
        ShowError("Please select a mod from the left list to move.");
    }
}

        public void OnMoveAllClick(object sender, RoutedEventArgs e)
        {
            Log.Information("OnMoveAllClick triggered.");

            foreach (var mod in LeftMods.ToList())
            {
                Log.Debug("Moving mod with ID: {ModId} to the right list.", mod.Id);
                LeftMods.Remove(mod);
                RightMods.Add(mod);
            }

            Log.Information("All mods successfully moved from the left list to the right list.");
        }

        public void OnMoveBackSelectedClick(object sender, RoutedEventArgs e)
{
    Log.Information("OnMoveBackSelectedClick triggered.");

    // Verwende das aktuell ausgewählte Element der rechten Liste
    var selectedItem = RightListBox?.SelectedItem as SteamCollectionItem;

    if (selectedItem != null)
    {
        Log.Debug("Attempting to move selected mod with ID: {ModId} back to the left list.", selectedItem.Id);

        if (_existingWorkshopIds.Contains(selectedItem.Id))
        {
            Log.Warning("Mod with ID: {ModId} is already in WorkshopID.txt and cannot be moved back.", selectedItem.Id);
            ShowError("This Mod is already saved in the local Mod Library and cannot be moved to Hold.");
        }
        else if (selectedItem.BackgroundColor == "Yellow")
        {
            Log.Debug("Mod with ID: {ModId} has a yellow background. Resetting and moving to the left list.", selectedItem.Id);
            RightMods.Remove(selectedItem);
            selectedItem.BackgroundColor = "Transparent";
            LeftMods.Add(selectedItem);
            Log.Information("Mod with ID: {ModId} successfully moved to the left list with reset background.", selectedItem.Id);
        }
        else
        {
            Log.Debug("Mod with ID: {ModId} has no restrictions. Moving to the left list.", selectedItem.Id);
            RightMods.Remove(selectedItem);
            LeftMods.Add(selectedItem);
            Log.Information("Mod with ID: {ModId} successfully moved to the left list.", selectedItem.Id);
        }
    }
    else
    {
        Log.Warning("No mod selected in the right list to move back.");
        ShowError("Please select a mod from the right list to move back.");
    }
}
        public void OnMoveBackAllClick(object sender, RoutedEventArgs e)
{
    Log.Information("OnMoveBackAllClick triggered.");

    bool modsSkipped = false;

    foreach (var mod in RightMods.ToList())
    {
        Log.Debug("Processing mod with ID: {ModId} for moving back to the left list.", mod.Id);

        if (_existingWorkshopIds.Contains(mod.Id))
        {
            Log.Warning("Mod with ID: {ModId} is already in WorkshopID.txt and will not be moved back.", mod.Id);
            modsSkipped = true;
            continue;
        }
        else if (mod.BackgroundColor == "Yellow")
        {
            Log.Debug("Mod with ID: {ModId} has a yellow background. Resetting and moving to the left list.", mod.Id);
            RightMods.Remove(mod);
            mod.BackgroundColor = "Transparent";
            LeftMods.Add(mod);
            Log.Information("Mod with ID: {ModId} successfully moved to the left list with reset background.", mod.Id);
        }
        else
        {
            Log.Debug("Mod with ID: {ModId} has no restrictions. Moving to the left list.", mod.Id);
            RightMods.Remove(mod);
            LeftMods.Add(mod);
            Log.Information("Mod with ID: {ModId} successfully moved to the left list.", mod.Id);
        }
    }

    if (modsSkipped)
    {
        Log.Information("Some mods were not moved back to the left list as they are already in WorkshopID.txt.");
        ShowInfo("Only Mods not saved in the local Mod Library were moved to Hold.");
    }

    Log.Information("All applicable mods have been processed and moved back to the left list.");
}

        public async void OnWriteWorkshopIDsClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var workshopIds = RightMods.Select(mod => mod.Id).ToList();

                if (!workshopIds.Any())
                {
                    ShowError("No Workshop IDs available to save.");
                    return;
                }

                // Initialize the SteamScraper
                var scraper = new SteamScraper();

                // Use the new method
                await WriteWorkshopID.WriteAndSyncWorkshopIdsAsync(workshopIds, scraper);

                Log.Information($"Successfully saved and synchronized Workshop IDs. Total IDs processed: {workshopIds.Count}");

                // Show info and close the window after the dialog is closed
                ShowInfo("Workshop IDs saved and synchronized successfully.", this.Close);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save and synchronize Workshop IDs.");
                ShowError("An error occurred while saving Workshop IDs.");
            }
        }

private void ShowError(string message)
{
    Log.Error("Error dialog triggered with message: {Message}", message);
    ShowDialog("Error", message);
}

private void ShowInfo(string message, Action? onClose = null)
{
    Log.Information("Info dialog triggered with message: {Message}", message);

    var dialog = new Window
    {
        Title = "Info",
        Width = 400,
        Height = 150,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(10),
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Margin = new Avalonia.Thickness(0, 10, 0, 0)
                }
            }
        }
    };

    var okButton = (Button)((StackPanel)dialog.Content).Children[1];
    okButton.Click += (_, _) =>
    {
        Log.Debug("Closing info dialog.");
        dialog.Close();
        onClose?.Invoke(); // Execute the callback if provided
    };

    dialog.ShowDialog(this);
}
private void ShowDialog(string title, string message)
{
    Log.Debug("Creating dialog with title: {Title} and message: {Message}", title, message);

    var dialog = new Window
    {
        Title = title,
        Width = 400,
        Height = 150,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(10),
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Margin = new Avalonia.Thickness(0, 10, 0, 0)
                }
            }
        }
    };

    var okButton = (Button)((StackPanel)dialog.Content).Children[1];
    okButton.Click += (_, _) => {
        Log.Debug("Closing dialog with title: {Title}", title);
        dialog.Close();
    };

    Log.Debug("Displaying dialog with title: {Title}", title);
    dialog.ShowDialog(this);
}

    }
}
