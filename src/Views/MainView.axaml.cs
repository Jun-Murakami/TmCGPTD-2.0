using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using System;
using System.Threading.Tasks;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class MainView : UserControl
    {
        public MainViewModel MainViewModel { get; } = new MainViewModel();
        private StackPanel _stackPanel;
        public MainView()
        {
            InitializeComponent();

            DataContext = MainViewModel;
            VMLocator.MainViewModel = MainViewModel;
            MainViewModel.PropertyChanged += ViewModel_PropertyChanged!;


            _stackPanel = this.FindControl<StackPanel>("ProgramTitleBar")!;

            if (OperatingSystem.IsMacOS())
            {
                _stackPanel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            }

            _syncLogBlock = this.FindControl<TextBlock>("SyncLogText")!;


            _leftPane = this.FindControl<Frame>("LeftFrame")!;
            _rightPane = this.FindControl<Frame>("RightFrame")!;

            //Frame‚Ìƒiƒr—š—ð–³Œø
            _leftPane.IsNavigationStackEnabled = false;
            _rightPane.IsNavigationStackEnabled = false;

            var lPane = this.FindControl<ListBox>("LeftPaneList");
            var rPane = this.FindControl<ListBox>("RightPaneList");

            lPane.SelectionChanged += OnLeftListBoxSelectionChanged!;
            rPane.SelectionChanged += OnRightListBoxSelectionChanged!;
            lPane.SelectedIndex = 0;
            rPane.SelectedIndex = 0;

            _rightPane.Navigate(typeof(EditorView));
            _leftPane.Navigate(typeof(ChatView));
        }

        private TextBlock _syncLogBlock;
        private Frame _leftPane;
        private Frame _rightPane;

        private void OnRightListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch ((sender as ListBox).SelectedIndex)
            {
                case 0:
                    _rightPane.Navigate(typeof(EditorView));
                    break;

                case 1:
                    _rightPane.Navigate(typeof(PreviewView));
                    break;
            }
        }
        private void OnLeftListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch ((sender as ListBox).SelectedIndex)
            {
                case 0:
                    _leftPane.Navigate(typeof(ChatView));
                    break;

                case 1:
                    _leftPane.Navigate(typeof(WebChatView), null, new SuppressNavigationTransitionInfo());
                    break;

                case 2:
                    _leftPane.Navigate(typeof(WebChatBardView), null, new SuppressNavigationTransitionInfo());
                    break;
            }
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.OnLogin))
            {
                if (MainViewModel.OnLogin)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _leftPane.Navigate(typeof(WebLogInView), null, new SuppressNavigationTransitionInfo());
                    });
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _leftPane.Navigate(typeof(ChatView), null, new SuppressNavigationTransitionInfo());
                        var cdialog = new ContentDialog() { Title = $"Login success.", PrimaryButtonText = "OK" };
                        MainViewModel.ContentDialogShowAsync(cdialog);
                    });

                }
            }
            else if (e.PropertyName == nameof(MainViewModel.SyncLogText))
            {
                _syncLogBlock.Classes.Add("FadeOut");

                await Task.Delay(10000);

                VMLocator.MainViewModel.SyncLogText = "";
                _syncLogBlock.Classes.Remove("FadeOut");
            }

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
