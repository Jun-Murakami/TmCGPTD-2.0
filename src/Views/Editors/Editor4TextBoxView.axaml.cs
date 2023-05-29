using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class Editor4TextBoxView : UserControl
    {
        private TextBox _editor4;
        public Editor4TextBoxViewModel Editor4TextBoxViewModel { get; } = new Editor4TextBoxViewModel();
        public Editor4TextBoxView()
        {
            InitializeComponent();
            DataContext = Editor4TextBoxViewModel;
            VMLocator.Editor4TextBoxViewModel = Editor4TextBoxViewModel;

            _editor4 = this.FindControl<TextBox>("Editor4TextBox");

            _editor4.Text = string.Empty;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
