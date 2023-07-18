using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TmCGPTD.Views
{
    public partial class EditorOneView : UserControl
    {
        public EditorOneView()
        {
            InitializeComponent();


            var editorOne = this.FindControl<TextBox>("OneEditor");

            this.AttachedToVisualTree += async (sender, e) =>
            {
                if (editorOne == null || VMLocator.EditorViewModel == null || await VMLocator.EditorViewModel.GetRecentTextAsync() == null) return;
                VMLocator.EditorViewModel.Editor1Text = await VMLocator.EditorViewModel.GetRecentTextAsync();
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
