using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Serilog;
using System;

namespace ZomboidModManager
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Set StartupWindow as the first window
                desktop.MainWindow = new StartupWindow();

                // Add global exception handling
                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    if (args.ExceptionObject is Exception ex)
                    {
                        Log.Error(ex, "An unhandled exception occurred.");
                    }
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
