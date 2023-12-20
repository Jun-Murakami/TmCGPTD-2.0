using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TmCGPTD.Views
{
    public partial class EditorOneView : UserControl
    {
        public EditorOneView()
        {
            InitializeComponent();
            DataContext = VMLocator.EditorViewModel;

            var editorOne = this.FindControl<TextBox>("OneEditor");

            this.AttachedToVisualTree += (sender, e) =>
            {
                if (editorOne == null || VMLocator.EditorViewModel == null || VMLocator.EditorViewModel.GetRecentText() == null) return;

                VMLocator.EditorViewModel.Editor1_1Text = VMLocator.EditorViewModel.GetRecentText();

                VMLocator.EditorViewModel.Editor3_1Text = VMLocator.EditorViewModel.Editor1_1Text;

                VMLocator.EditorViewModel.Editor5_1Text = VMLocator.EditorViewModel.Editor1_1Text;

                VMLocator.EditorViewModel.Editor3_2Text = string.Empty;
                VMLocator.EditorViewModel.Editor3_3Text = string.Empty;

                VMLocator.EditorViewModel.Editor5_2Text = string.Empty;
                VMLocator.EditorViewModel.Editor5_3Text = string.Empty;
                VMLocator.EditorViewModel.Editor5_4Text = string.Empty;
                VMLocator.EditorViewModel.Editor5_5Text = string.Empty;

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
