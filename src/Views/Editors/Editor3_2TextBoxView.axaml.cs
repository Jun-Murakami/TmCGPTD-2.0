using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class Editor3_2TextBoxView : UserControl
    {
        public Editor3_2TextBoxView()
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
