using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Diagnostics;

namespace TmCGPTD.Views
{
    public partial class WebAppView : UserControl
    {
        public WebAppView()
        {
            InitializeComponent();
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string url = "https://tmcgptd.web.app/";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}