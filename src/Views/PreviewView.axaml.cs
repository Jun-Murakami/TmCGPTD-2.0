using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class PreviewView: UserControl
    {
        public PreviewViewModel PreviewViewModel { get; } = new PreviewViewModel();
        public PreviewView()
        {
            InitializeComponent();
            var editorViewModel = VMLocator.EditorViewModel;
            PreviewViewModel.EditorViewModel = editorViewModel;
            DataContext = PreviewViewModel;
            VMLocator.PreviewViewModel = PreviewViewModel;

            var previewTextBox = this.FindControl<TextBox>("PreviewTextBox");

            this.AttachedToVisualTree += (sender, e) =>
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
