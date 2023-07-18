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
using HarfBuzzSharp;
using System.Reflection;
using System.Diagnostics;
using Avalonia.Platform;

namespace TmCGPTD.Views
{
    public partial class ChatView : UserControl
    {
        private AvaloniaCefBrowser _browser;
        private Button _button;
        private Button _button2;
        private TextBox _searchBox;

        public ChatViewModel ChatViewModel { get; } = new ChatViewModel();

        public ChatView()
        {
            InitializeComponent();
            DataContext = ChatViewModel;
            VMLocator.ChatViewModel = ChatViewModel;

            var browserWrapper = this.FindControl<Decorator>("ChatBrowserWrapper")!;

            _browser = new AvaloniaCefBrowser
            {
                LifeSpanHandler = new CustomLifeSpanHandler(),
            };
            _browser.ContextMenuHandler = new CustomContextMenuHandler();
            browserWrapper.Child = _browser;

            _browser.LoadEnd += Browser_LoadEnd;
            _browser.LoadStart += Browser_LoadStart;
            ChatViewModel.PropertyChanged += ViewModel_PropertyChanged;
            _browser.Focusable = false;
            ChatViewModel.SetBrowser(_browser);

            _button = this!.FindControl<Button>("ButtonWrite")!;
            ChatViewModel.SetButtonWrite(_button);

            _button2 = this!.FindControl<Button>("ButtonWrite2")!;
            ChatViewModel.SetButtonWrite2(_button2);

            _searchBox = this!.FindControl<TextBox>("SearchBox")!;
        }

        private async void Browser_LoadStart(object sender, LoadStartEventArgs e)
        {
#if WINDOWS
            using var scriptStreamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/Assets/highlight.min.js")));
#else
            using var scriptStreamReader = new StreamReader(AvaloniaLocator.Current.GetService<IAssetLoader>()!.Open(new Uri("avares://TmCGPTD/Assets/highlight.min.js")));
#endif
            string scriptContent = await scriptStreamReader.ReadToEndAsync();
            _browser.ExecuteJavaScript(scriptContent);
            _browser.ExecuteJavaScript("hljs.highlightAll();");
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
                    VMLocator.MainViewModel.SearchKeyword = VMLocator.MainViewModel.SearchLogKeyword;
                    await ChatViewModel.TextSearch(VMLocator.MainViewModel.SearchKeyword, true, true);
                }
            }
            await Dispatcher.UIThread.InvokeAsync(() => _browser.Opacity = 1);
        }

        private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
                string? htmlContent = ChatViewModel.HtmlContent;

                int byteCount = Encoding.UTF8.GetByteCount(htmlContent!);

                // 1MBを超えたら一時ファイルに書き出してから読み込む
                if (byteCount > 1000000) //
                {
                    string tempFilePath = System.IO.Path.Combine(AppSettings.Instance.AppDataPath, "tempHtmlFile.html");
                    await File.WriteAllTextAsync(tempFilePath, htmlContent);

                    _browser.Address = tempFilePath;
                }
                else
                {
                    var encodedHtml = Uri.EscapeDataString(htmlContent!);
                    var dataUrl = $"data:text/html;charset=utf-8,{encodedHtml}";
                    _browser.Address = dataUrl;
                }
            }
        }

        private void FocusSearchBox(object sender, RoutedEventArgs e)
        {
            if (VMLocator.MainViewModel.SelectedLeftPane == "API Chat")
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
