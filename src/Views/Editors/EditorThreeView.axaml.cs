using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using System;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class EditorThreeView : UserControl
    {
        private Frame? _editor3_2Pane;
        public EditorThreeView()
        {
            InitializeComponent();
            DataContext = VMLocator.EditorViewModel;

            this.AttachedToVisualTree += async (sender, e) =>
            {
                var br = Environment.NewLine;
                if (VMLocator.EditorViewModel == null || await VMLocator.EditorViewModel.GetRecentText() == null) return;
                VMLocator.EditorViewModel.Editor3Text = (VMLocator.EditorViewModel.Editor3Text + br + VMLocator.EditorViewModel.Editor4Text + br + VMLocator.EditorViewModel.Editor5Text).Trim();
                VMLocator.EditorViewModel.Editor4Text = string.Empty;
                VMLocator.EditorViewModel.Editor5Text = string.Empty;
            };


            VMLocator.EditorViewModel.PropertyChanged += ViewModel_PropertyChanged;

            _editor3_2Pane = this.FindControl<Frame>("Editor3_2Frame")!;

            _editor3_2Pane.IsNavigationStackEnabled = false;

            if (VMLocator.EditorViewModel.EditorModeIsChecked)
            {
                _editor3_2Pane.Navigate(typeof(Editor3_2AvalonEditView));
            }
            else
            {
                _editor3_2Pane.Navigate(typeof(Editor3_2TextBoxView));
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorViewModel.EditorModeIsChecked))
            {
                if (VMLocator.EditorViewModel.EditorModeIsChecked)
                {
                    _editor3_2Pane!.Navigate(typeof(Editor3_2AvalonEditView),null,new SlideNavigationTransitionInfo());
                }
                else
                {
                    _editor3_2Pane!.Navigate(typeof(Editor3_2TextBoxView), null, new SlideNavigationTransitionInfo());
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
