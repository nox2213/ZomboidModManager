using Avalonia.Media.Imaging;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZomboidModManager.Models
{
    public class SteamModItem
    {
        public string Id { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string WorkshopLink { get; set; } = "";
        public string BackgroundColor { get; set; } = "Transparent";


        private string? _localImagePath;

        public Bitmap? ImageBitmap
        {
            get
            {
                if (_localImagePath != null && File.Exists(_localImagePath))
                {
                    return new Bitmap(_localImagePath);
                }
                return null;
            }
        }

        public async Task DownloadImageAsync()
        {
            if (!string.IsNullOrEmpty(ImageUrl))
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(ImageUrl);
                response.EnsureSuccessStatusCode();

                _localImagePath = Path.Combine("src", "WorkshopObjects", "tempimage", $"{Id}.png");
                var directoryPath = Path.GetDirectoryName(_localImagePath);

                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                await using var fileStream = new FileStream(_localImagePath, FileMode.Create, FileAccess.Write);
                await response.Content.CopyToAsync(fileStream);
            }
        }

        public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}
