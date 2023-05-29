using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class DatabaseSettingsView : UserControl
    {
        public DatabaseSettingsViewModel DatabaseSettingsViewModel { get; } = new DatabaseSettingsViewModel();

        public DatabaseSettingsView()
        {
            InitializeComponent();

            DataContext = DatabaseSettingsViewModel;
            VMLocator.DatabaseSettingsViewModel = DatabaseSettingsViewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
