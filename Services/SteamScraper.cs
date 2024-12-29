using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using ZomboidModManager.Models;

namespace ZomboidModManager.Services
{
    public class SteamScraper
    {
        public async Task<List<SteamModItem>> ScrapeCollectionAsync(List<string> workshopIds)
        {
            var modItems = new List<SteamModItem>();

            foreach (var workshopId in workshopIds)
            {
                var modItem = await ScrapeWorkshopItemAsync(workshopId);
                if (modItem != null)
                {
                    modItems.Add(modItem);
                }
            }

            return modItems;
        }

        public async Task<SteamModItem?> ScrapeWorkshopItemAsync(string workshopId)
        {
            try
            {
                var url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopId}";
                using var httpClient = new HttpClient();
                var html = await httpClient.GetStringAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var title = doc.DocumentNode.SelectSingleNode("//div[@class='workshopItemTitle']")?.InnerText.Trim() ?? "Unknown Title";
                var author = doc.DocumentNode.SelectSingleNode("//div[@class='friendBlockContent']")?.InnerText.Trim() ?? "Unknown Author";
                var description = doc.DocumentNode.SelectSingleNode("//div[@class='workshopItemDescription']")?.InnerText.Trim() ?? "No Description";
                var imageUrl = doc.DocumentNode.SelectSingleNode("//img[@id='previewImageMain']")?.GetAttributeValue("src", "")
                            ?? doc.DocumentNode.SelectSingleNode("//img[@id='previewImage']")?.GetAttributeValue("src", "");

                return new SteamModItem
                {
                    Id = workshopId,
                    ImageUrl = imageUrl ?? "",
                    Title = title,
                    Author = author,
                    ShortDescription = description,
                    WorkshopLink = url
                };
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, $"Failed to scrape Workshop item with ID {workshopId}");
                return null;
            }
        }
    }
}
