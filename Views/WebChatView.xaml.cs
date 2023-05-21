using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Xilium.CefGlue.Avalonia;
using TmCGPTD.ViewModels;
using Avalonia.Interactivity;

namespace TmCGPTD.Views
{
    public partial class WebChatView : UserControl
    {
        private AvaloniaCefBrowser browser;
        private TextBox _searchBox;
        public WebChatViewModel WebChatViewModel { get; } = new WebChatViewModel();

        public WebChatView()
        {
            InitializeComponent();

            DataContext = WebChatViewModel;
            VMLocator.WebChatViewModel = WebChatViewModel;

            var browserWrapper = this.FindControl<Decorator>("WebChatBrowserWrapper");

            browser = new AvaloniaCefBrowser();
            browser.Address = "https://chat.openai.com/";
            browser.ContextMenuHandler = new CustomContextMenuHandler();
            browserWrapper.Child = browser;
            WebChatViewModel.SetBrowser(browser);
            browser.Focusable = false;

            _searchBox = this.FindControl<TextBox>("SearchBox");
        }

        private void FocusSearchBox(object sender, RoutedEventArgs e)
        {
            if (VMLocator.MainViewModel.SelectedLeftPane == "WebChat")
            {
                _searchBox.Focus();
            }
        }

        public void OpenDevTools()
        {
            browser.ShowDeveloperTools();
        }

        public void Dispose()
        {
            browser.Dispose();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}