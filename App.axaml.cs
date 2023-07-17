using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using System;
using TmCGPTD.Views;

namespace TmCGPTD
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            if (OperatingSystem.IsMacOS())
            {
                this.Styles.Add(new StyleInclude(new Uri("avares://TmCGPTD/Assets/StylesMac.axaml")));
            }
            else
            {
                this.Styles.Add(new StyleInclude(new Uri("avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml")));
                this.Styles.Add(new StyleInclude(new Uri("avares://TmCGPTD/Assets/Styles.axaml")));
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var VMLocator = new VMLocator();
            Resources.Add("VMLocator", VMLocator);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

    }
}