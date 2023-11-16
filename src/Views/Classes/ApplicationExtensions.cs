using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia;
using Avalonia.VisualTree;

namespace TmCGPTD
{
    public static class ApplicationExtensions
    {
        public static TopLevel? GetTopLevel(this Application app)
        {
            if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            if (app.ApplicationLifetime is ISingleViewApplicationLifetime viewApp)
            {
                var visualRoot = viewApp.MainView?.GetVisualRoot();
                return visualRoot as TopLevel;
            }
            return null;
        }
    }

}
