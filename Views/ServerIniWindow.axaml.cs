using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;
using Serilog;
using System;
using System.Collections.Generic;
using ZomboidModManager.Services;

namespace ZomboidModManager
{
    public partial class ServerIniWindow : Window
    {
        public ServerIniWindow()
        {
            InitializeComponent();
        }

        private async void OnBrowseClick(object? sender, RoutedEventArgs e)
        {
            var iniTextBox = this.FindControl<TextBox>("IniPathTextBox");
            if (iniTextBox == null)
            {
                Log.Error("IniPathTextBox wurde nicht gefunden.");
                return;
            }

            if (StorageProvider != null)
            {
                var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Server INI-Datei auswählen",
                    AllowMultiple = false,
                    FileTypeFilter = new List<FilePickerFileType>
                    {
                        new FilePickerFileType("INI-Dateien") { Patterns = new[] { "*.ini" } },
                        new FilePickerFileType("Alle Dateien") { Patterns = new[] { "*" } }
                    }
                });

                if (result != null && result.Count > 0)
                {
                    iniTextBox.Text = result[0].Path.LocalPath;
                }
            }
            else
            {
                Log.Warning("StorageProvider ist nicht verfügbar.");
            }
        }

        private async void OnOkClick(object? sender, RoutedEventArgs e)
        {
            var iniTextBox = this.FindControl<TextBox>("IniPathTextBox");
            if (iniTextBox == null)
            {
                Log.Error("IniPathTextBox wurde nicht gefunden.");
                return;
            }

            string iniPath = iniTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(iniPath))
            {
                ShowErrorDialog("Keine Datei angegeben.", "Bitte wähle eine gültige INI-Datei aus.");
                return;
            }

            var workshopIds = WorkshopIDLoaderFromINI.ExtractWorkshopIDs(iniPath);
            if (workshopIds.Count == 0)
            {
                ShowErrorDialog("Keine Workshop-IDs gefunden.", "Die INI-Datei enthält keine gültigen Workshop-IDs.");
                return;
            }

            // Create and show IniInputWindow
            var iniInputWindow = new IniInputWindow(workshopIds, iniPath);

            try
            {
                // Show IniInputWindow modally without closing or hiding the current window
                await iniInputWindow.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fehler beim Öffnen des IniInputWindow.");
            }
            finally
            {
                Close(); // Close the current ServerIniWindow after IniInputWindow is done
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowErrorDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 300,
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
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Avalonia.Thickness(0, 0, 0, 10)
                        },
                        new Button
                        {
                            Content = "OK",
                            Width = 80,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                        }
                    }
                }
            };

            var okButton = (Button)((StackPanel)dialog.Content).Children[1];
            okButton.Click += (s, e) => dialog.Close();

            dialog.ShowDialog(this);
        }
    }
}
