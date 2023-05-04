using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using TmCGPTD.ViewModels;
using System.IO;
using Avalonia.Threading;
using System.ComponentModel;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Events;
using System.Threading.Tasks;
using System.Text;
using Avalonia.Interactivity;

namespace TmCGPTD.Views
{
    public partial class ChatView: UserControl
    {
        private AvaloniaCefBrowser _browser;
        private Button _button;
        private TextBox _searchBox;

        public ChatViewModel ChatViewModel { get; } = new ChatViewModel();

        public ChatView()
        {
            InitializeComponent();
            DataContext = ChatViewModel;
            VMLocator.ChatViewModel = ChatViewModel;

            var browserWrapper = this.FindControl<Decorator>("ChatBrowserWrapper");
            _browser = new AvaloniaCefBrowser();
            browserWrapper.Child = _browser;

            _browser.LoadEnd += Browser_LoadEnd;
            ChatViewModel.PropertyChanged += ViewModel_PropertyChanged;
            _browser.Focusable = false;
            ChatViewModel.SetBrowser(_browser);

            _button = this.FindControl<Button>("ButtonWrite");
            ChatViewModel.SetButtonWrite(_button);


            _searchBox = this.FindControl<TextBox>("SearchBox");
        }

        private async void Browser_LoadEnd(object sender, LoadEndEventArgs e)
        {
            if (VMLocator.DataGridViewModel.SelectedItem != null)
            { 
                if (string.IsNullOrWhiteSpace(VMLocator.MainViewModel.SearchLogKeyword))
                {
                    var script = "window.scrollTo(0, document.body.scrollHeight);";
                    _browser.ExecuteJavaScript(script);
                }
                else
                {
                    ChatViewModel.SearchKeyword = VMLocator.MainViewModel.SearchLogKeyword;
                    await ChatViewModel.TextSearch(ChatViewModel.SearchKeyword, true, true);
                }
            }
            await Dispatcher.UIThread.InvokeAsync(() =>{_browser.Opacity = 1;});
        }

        private async void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChatViewModel.HtmlContent))
            {
                await LoadHtmlFromViewModel();
            }
        }

        private async Task LoadHtmlFromViewModel()
        {
            if (_browser != null && DataContext is ChatViewModel viewModel)
            {
                _browser.Opacity = 0;
                string htmlContent = ChatViewModel.HtmlContent;

                int byteCount = Encoding.UTF8.GetByteCount(htmlContent);

                // バイト数に応じて読み込み方法を選択
                if (byteCount > 1000000) //
                {
                    // 一時ファイルにHTMLコンテンツを書き込む
                    string tempFilePath = System.IO.Path.Combine(AppSettings.Instance.AppDataPath, "tempHtmlFile.html");
                    await File.WriteAllTextAsync(tempFilePath, htmlContent);

                    // 一時ファイルのパスを_browser.Addressに設定する
                    _browser.Address = tempFilePath;
                }
                else
                {
                    var encodedHtml = Uri.EscapeDataString(htmlContent);
                    var dataUrl = $"data:text/html;charset=utf-8,{encodedHtml}";
                    _browser.Address = dataUrl;
                }
            }
        }

        private void FocusSearchBox(object sender, RoutedEventArgs e)
        {
            if (VMLocator.MainViewModel.SelectedLeftPane == "Chat")
            {
                _searchBox.Focus();
            }
        }

        public void OpenDevTools()
        {
            _browser.ShowDeveloperTools();
        }

        public void Dispose()
        {
            _browser.Dispose();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

}
