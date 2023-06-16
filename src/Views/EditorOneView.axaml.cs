using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TmCGPTD.Views
{
    public partial class EditorOneView : UserControl
    {
        public EditorOneView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
