using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using TmCGPTD.ViewModels;
using FluentAvalonia.UI.Controls;

namespace TmCGPTD.Views
{
    public partial class MacEditorView : UserControl
    {
        private Frame _editorPane;
        public EditorViewModel EditorViewModel { get; } = new EditorViewModel();

        public MacEditorView()
        {
            InitializeComponent();
            DataContext = EditorViewModel;
            VMLocator.EditorViewModel = EditorViewModel;

            EditorViewModel.PropertyChanged += ViewModel_PropertyChanged;

            _editorPane = this.FindControl<Frame>("EditorFrame")!;

            _editorPane.IsNavigationStackEnabled = false;

            _editorPane.Navigate(typeof(MacEditorFiveView));
        }

        private void UserControl_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
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

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorViewModel.EditorSeparateMode))
            {
                switch (EditorViewModel.EditorSeparateMode)
                {
                    case 5:
                        _editorPane.Navigate(typeof(MacEditorFiveView));
                        break;
                    case 3:
                        _editorPane.Navigate(typeof(MacEditorThreeView));
                        break;
                    case 1:
                        _editorPane.Navigate(typeof(EditorOneView));
                        break;
                }
            }

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    }
}
