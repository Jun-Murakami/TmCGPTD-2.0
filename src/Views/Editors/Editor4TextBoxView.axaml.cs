using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class Editor4TextBoxView : UserControl
    {
        public Editor4TextBoxView()
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
