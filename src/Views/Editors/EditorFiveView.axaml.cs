using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using System;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class EditorFiveView : UserControl
    {
        private Frame? _editor5_2Pane;
        private Frame? _editor5_4Pane;
        public EditorFiveView()
        {
            InitializeComponent();
            DataContext = VMLocator.EditorViewModel;

            VMLocator.EditorViewModel.PropertyChanged += ViewModel_PropertyChanged;

            _editor5_2Pane = this.FindControl<Frame>("Editor5_2Frame")!;
            _editor5_4Pane = this.FindControl<Frame>("Editor5_4Frame")!;

            _editor5_2Pane.IsNavigationStackEnabled = false;
            _editor5_4Pane.IsNavigationStackEnabled = false;

            if (VMLocator.EditorViewModel.EditorModeIsChecked)
            {
                _editor5_2Pane.Navigate(typeof(Editor2AvalonEditView));
                _editor5_4Pane.Navigate(typeof(Editor4AvalonEditView));
            }
            else
            {
                _editor5_2Pane.Navigate(typeof(Editor2TextBoxView));
                _editor5_4Pane.Navigate(typeof(Editor4TextBoxView));
            }

            this.AttachedToVisualTree += (sender, e) =>
            {
                var br = Environment.NewLine;
                if (VMLocator.EditorViewModel == null) return;

                VMLocator.EditorViewModel.Editor1_1Text = VMLocator.EditorViewModel.Editor1Text;
                VMLocator.EditorViewModel.Editor3_1Text = VMLocator.EditorViewModel.Editor1Text;
                VMLocator.EditorViewModel.Editor5_1Text = VMLocator.EditorViewModel.Editor1Text;

                VMLocator.EditorViewModel.Editor3_2Text = VMLocator.EditorViewModel.Editor2Text;
                VMLocator.EditorViewModel.Editor5_2Text = VMLocator.EditorViewModel.Editor2Text;

                VMLocator.EditorViewModel.Editor3_3Text = VMLocator.EditorViewModel.Editor3Text;
                VMLocator.EditorViewModel.Editor5_3Text = VMLocator.EditorViewModel.Editor3Text;

                VMLocator.EditorViewModel.Editor5_4Text = VMLocator.EditorViewModel.Editor4Text;
                VMLocator.EditorViewModel.Editor5_5Text = VMLocator.EditorViewModel.Editor5Text;
            };
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorViewModel.EditorModeIsChecked))
            {
                if (VMLocator.EditorViewModel.EditorModeIsChecked)
                {
                    _editor5_2Pane!.Navigate(typeof(Editor2AvalonEditView), null, new SlideNavigationTransitionInfo());
                    _editor5_4Pane!.Navigate(typeof(Editor4AvalonEditView), null, new SlideNavigationTransitionInfo());
                }
                else
                {
                    _editor5_2Pane!.Navigate(typeof(Editor2TextBoxView), null, new SlideNavigationTransitionInfo());
                    _editor5_4Pane!.Navigate(typeof(Editor4TextBoxView), null, new SlideNavigationTransitionInfo());
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
