using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TmCGPTD.Views
{
    public partial class MacEditorOneView : UserControl
    {
        public MacEditorOneView()
        {
            InitializeComponent();


            var editorOne = this.FindControl<TextBox>("OneEditor");

            this.AttachedToVisualTree += (sender, e) =>
            {
                if (editorOne == null || VMLocator.EditorViewModel == null || VMLocator.EditorViewModel.GetRecentText() == null) return;
                VMLocator.EditorViewModel.Editor1Text = VMLocator.EditorViewModel.GetRecentText();
                VMLocator.EditorViewModel.Editor2Text = string.Empty;
                VMLocator.EditorViewModel.Editor3Text = string.Empty;
                VMLocator.EditorViewModel.Editor4Text = string.Empty;
                VMLocator.EditorViewModel.Editor5Text = string.Empty;
            };
        }


        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
