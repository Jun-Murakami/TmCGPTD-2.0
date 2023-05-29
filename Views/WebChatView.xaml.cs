using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Xilium.CefGlue.Avalonia;
using TmCGPTD.ViewModels;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Xilium.CefGlue.Common.Events;

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

            browser = new AvaloniaCefBrowser
            {
                LifeSpanHandler = new CustomLifeSpanHandler(),
            };
            browser.Address = "https://chat.openai.com/";
            browser.ContextMenuHandler = new CustomContextMenuHandler();
            browserWrapper.Child = browser;
            WebChatViewModel.SetBrowser(browser);
            //browser.Focusable = false;
            browser.LoadEnd += Browser_LoadEnd;

            _searchBox = this.FindControl<TextBox>("SearchBox");
        }

        private async void Browser_LoadEnd(object sender, LoadEndEventArgs e)
        {
            var script = @"document.addEventListener(""keydown"", (event) => {
                            // Check if Cmd + C (Mac)
                            if (event.metaKey && event.key === ""c"") {
                            const selectedText = window.getSelection().toString();

                            if (selectedText) {
                                const textarea = document.createElement('textarea');
                                textarea.value = selectedText;
                                document.body.appendChild(textarea);
                                textarea.select();
                                document.execCommand('copy');
                                document.body.removeChild(textarea);
                            }
                                }
                            });";
            browser.ExecuteJavaScript(script);
        }

        private void FocusSearchBox(object sender, RoutedEventArgs e)
        {
            if (VMLocator.MainViewModel.SelectedLeftPane == "Web Chat")
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