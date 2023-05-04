using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class EditorView : UserControl
    {
        public EditorViewModel EditorViewModel { get; } = new EditorViewModel();

        public EditorView()
        {
            InitializeComponent();
            DataContext = EditorViewModel;
            VMLocator.EditorViewModel = EditorViewModel;
        }

        private void UserControl_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                var editorViewModel = DataContext as EditorViewModel;
                if (editorViewModel != null)
                {
                    if (e.Delta.Y > 0)
                        editorViewModel.EditorCommonFontSize += 1;
                    else if (e.Delta.Y < 0 && editorViewModel.EditorCommonFontSize > 1)
                        editorViewModel.EditorCommonFontSize -= 1;
                    e.Handled = true;
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    }
}
