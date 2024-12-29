using Avalonia.Media.Imaging;
using System.IO;
using System.Text.Json;

namespace ZomboidModManager.Models
{
    public class SteamCollectionItem
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
                return new Bitmap(Path.Combine("Models", "Assets", "placeholder.png"));
            }
        }

        public void SetLocalImagePath(string path)
        {
            _localImagePath = path;
        }

        public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}
