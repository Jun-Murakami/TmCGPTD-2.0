using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;

namespace TmCGPTD.Views
{
    public partial class OptionSettingsView : UserControl
    {
        private ListBox _dialogList;
        private Frame _dialogFrame;

        public OptionSettingsView()
        {
            InitializeComponent();

            _dialogFrame = this.FindControl<Frame>("DialogFrame")!;
            _dialogFrame.IsNavigationStackEnabled = false;
            _dialogFrame.Navigate(typeof(ApiSettingsView));

            _dialogList = this.FindControl<ListBox>("DialogListBox")!;
            _dialogList.SelectedIndex = 0;
            _dialogList.SelectionChanged += OnDialogListBoxSelectionChanged;

            this.AttachedToVisualTree += OptionSettingsView_AttachedToVisualTree;
        }

        private void OptionSettingsView_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (_dialogList.SelectedIndex == 0) return;
            _dialogList.SelectedIndex = 0;
        }

        private void OnDialogListBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox) return;

            switch (listBox.SelectedIndex)
            {
                case 0:
                    _dialogFrame.Navigate(typeof(ApiSettingsView), null, new SlideNavigationTransitionInfo());
                    break;
                case 1:
                    _dialogFrame.Navigate(typeof(AppSettingsView), null, new SlideNavigationTransitionInfo());
                    break;
                case 2:
                    _dialogFrame.Navigate(typeof(HotKeyDisplayView), null, new SlideNavigationTransitionInfo());
                    break;
                case 3:
                    _dialogFrame.Navigate(typeof(WebAppView), null, new SlideNavigationTransitionInfo());
                    break;
                case 4:
                    _dialogFrame.Navigate(typeof(LicenseView), null, new SlideNavigationTransitionInfo());
                    break;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
