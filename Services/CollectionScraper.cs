using HtmlAgilityPack;
using ZomboidModManager.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZomboidModManager.Services
{
    public class CollectionScraper
    {
        private readonly HttpClient _httpClient;
        private readonly string _tempImageDirectory = Path.Combine(Path.GetTempPath(), "SteamCollectionImages");
        private readonly string _placeholderImagePath = Path.Combine("Models", "Assets", "placeholder.png");

        public CollectionScraper()
        {
            _httpClient = new HttpClient();

            // Ensure the temp directory exists
            if (!Directory.Exists(_tempImageDirectory))
            {
                Directory.CreateDirectory(_tempImageDirectory);
            }
        }

        public async Task<List<SteamCollectionItem>> ScrapeCollectionAsync(string collectionUrl)
        {
            var items = new List<SteamCollectionItem>();

            try
            {
                var response = await _httpClient.GetAsync(collectionUrl);
                response.EnsureSuccessStatusCode();
                var htmlContent = await response.Content.ReadAsStringAsync();

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(htmlContent);

                var modNodes = htmlDocument.DocumentNode.SelectNodes("//div[@class='collectionItem']");
                if (modNodes == null)
                {
                    Log.Warning("Keine Mods in der Kollektion gefunden.");
                    return items;
                }

                foreach (var modNode in modNodes)
                {
                    try
                    {
                        var linkNode = modNode.SelectSingleNode(".//a[@href]");
                        var modLink = linkNode?.Attributes["href"]?.Value;
                        var modId = ExtractModId(modLink);

                        var imageNode = modNode.SelectSingleNode(".//img[@class='workshopItemPreviewImage']");
                        var imageUrl = imageNode?.Attributes["src"]?.Value;

                        var titleNode = modNode.SelectSingleNode(".//div[@class='workshopItemTitle']");
                        var title = titleNode?.InnerText.Trim();

                        var authorNode = modNode.SelectSingleNode(".//div[@class='workshopItemAuthor']");
                        var author = authorNode?.InnerText.Trim() ?? "Unbekannter Autor";

                        var descriptionNode = modNode.SelectSingleNode(".//div[@class='workshopItemShortDesc']");
                        var description = descriptionNode?.InnerText.Trim() ?? "Keine Beschreibung verfügbar.";

                        if (!string.IsNullOrEmpty(modId))
                        {
                            var localImagePath = await DownloadImageAsync(imageUrl, modId);

                            var collectionItem = new SteamCollectionItem
                            {
                                Id = modId,
                                ImageUrl = imageUrl ?? "",
                                Title = title ?? "Unbenannte Mod",
                                Author = author,
                                ShortDescription = description,
                                WorkshopLink = modLink ?? "",
                                BackgroundColor = "Transparent"
                            };

                            collectionItem.SetLocalImagePath(localImagePath);
                            items.Add(collectionItem);
                        }
                        else
                        {
                            Log.Warning("Mod ID konnte nicht extrahiert werden.");
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Log.Error(innerEx, "Fehler beim Verarbeiten eines Mods.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fehler beim Abrufen der Kollektion von {CollectionUrl}.", collectionUrl);
            }

            return items;
        }

        private async Task<string> DownloadImageAsync(string? imageUrl, string modId)
        {
            try
            {
                var localImagePath = Path.Combine(_tempImageDirectory, $"{modId}.png");

                if (string.IsNullOrEmpty(imageUrl))
                {
                    return _placeholderImagePath;
                }

                var response = await _httpClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();

                await using var fileStream = new FileStream(localImagePath, FileMode.Create, FileAccess.Write);
                await response.Content.CopyToAsync(fileStream);

                return localImagePath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fehler beim Herunterladen des Bildes von {ImageUrl}.", imageUrl);
                return _placeholderImagePath;
            }
        }

        private string? ExtractModId(string? url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            var queryStart = url.IndexOf("?id=");
            if (queryStart == -1) return null;

            return url[(queryStart + 4)..];
        }

        public void CleanupTemporaryImages()
        {
            try
            {
                if (Directory.Exists(_tempImageDirectory))
                {
                    Directory.Delete(_tempImageDirectory, true);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fehler beim Löschen des temporären Bildverzeichnisses.");
            }
        }
    }
}
