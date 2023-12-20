using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class Editor2TextBoxView : UserControl
    {
        public Editor2TextBoxView()
        {
            InitializeComponent();
            DataContext = VMLocator.EditorViewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
