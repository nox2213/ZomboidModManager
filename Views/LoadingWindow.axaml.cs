using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ZomboidModManager
{
    public partial class LoadingWindow : Window
    {
        public Window? ParentWindow { get; set; }

        public LoadingWindow()
        {
            InitializeComponent();
            Topmost = true; // Ensure the window stays on top
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Optionale Logik für die Interaktion mit ParentWindow
        public void LinkToParent(Window parent)
        {
            ParentWindow = parent;
            Owner = parent; // Explizit die Owner-Eigenschaft setzen
        }
    }
}
