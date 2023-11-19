using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Xilium.CefGlue.Avalonia;
using TmCGPTD.ViewModels;

namespace TmCGPTD.Views
{
    public partial class WebChatView : UserControl
    {
        private readonly AvaloniaCefBrowser browser;
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
                Address = "https://chat.openai.com/",
                ContextMenuHandler = new CustomContextMenuHandler()
            };
            browserWrapper!.Child = browser;
            WebChatViewModel.SetBrowser(browser);
            }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}