using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class ApiSettingsView : UserControl
    {
        public ApiSettingsView()
        {
            InitializeComponent();
            //DataContext = new ApiSettingsViewModel();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
