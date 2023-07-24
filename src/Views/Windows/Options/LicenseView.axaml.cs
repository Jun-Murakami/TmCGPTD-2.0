using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class LicenseView : UserControl
    {
        public LicenseView()
        {
            InitializeComponent();
            DataContext = new LicenseViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
