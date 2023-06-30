using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class CloudLoginView : UserControl
    {
        public CloudLoginViewModel CloudLoginViewModel { get; } = new CloudLoginViewModel();
        public CloudLoginView()
        {
            InitializeComponent();

            DataContext = CloudLoginViewModel;
            VMLocator.CloudLoginViewModel = CloudLoginViewModel;
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
