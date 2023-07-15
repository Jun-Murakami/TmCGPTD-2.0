using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class PreviewView: UserControl
    {
        public PreviewView()
        {
            InitializeComponent();
            var editorViewModel = VMLocator.EditorViewModel;

            var previewTextBox = this.FindControl<TextBox>("PreviewTextBox");

            this.AttachedToVisualTree += async (sender, e) =>
            {
                if (previewTextBox == null || VMLocator.EditorViewModel == null|| VMLocator.EditorViewModel.GetRecentText() == null) return;
                previewTextBox.Text = VMLocator.EditorViewModel.GetRecentText();
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

}
