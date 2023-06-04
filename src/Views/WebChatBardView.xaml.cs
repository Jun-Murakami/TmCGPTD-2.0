using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Xilium.CefGlue.Avalonia;
using TmCGPTD.ViewModels;
using Avalonia.Interactivity;
using Xilium.CefGlue.Common.Events;

namespace TmCGPTD.Views
{
    public partial class WebChatBardView : UserControl
    {
        private AvaloniaCefBrowser browser;
        private TextBox _searchBox;
        public WebChatBardViewModel WebChatBardViewModel { get; } = new WebChatBardViewModel();

        public WebChatBardView()
        {
            InitializeComponent();

            DataContext = WebChatBardViewModel;
            VMLocator.WebChatBardViewModel = WebChatBardViewModel;

            var browserWrapper = this.FindControl<Decorator>("WebChatBardBrowserWrapper");

            browser = new AvaloniaCefBrowser
            {
                LifeSpanHandler = new CustomLifeSpanHandler(),
            };
            browser.Address = "https://bard.google.com/";
            browser.ContextMenuHandler = new CustomContextMenuHandler();
            browserWrapper.Child = browser;
            WebChatBardViewModel.SetBrowser(browser);
            //browser.Focusable = false;
            browser.LoadEnd += Browser_LoadEnd;

            _searchBox = this.FindControl<TextBox>("SearchBox");
        }

        private void FocusSearchBox(object sender, RoutedEventArgs e)
        {
            if (VMLocator.MainViewModel.SelectedLeftPane == "Bard")
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

        private async void Browser_LoadEnd(object sender, LoadEndEventArgs e)
        {
            string addElementsCode = @"
                                    const button = document.createElement('button');
                                    button.id = 'floatingCopyButton';
                                    button.textContent = 'Copy to clipboard';
                                    document.body.appendChild(button);

                                    const style = document.createElement('style');
                                    style.textContent = `
                                    #floatingCopyButton {
                                        position: absolute;
                                        display: none;
                                        background: #343541;
                                        color: #dcdcdc;
                                        border-width: 1px;
                                        border: #545563 solid;
                                        cursor: pointer;
                                        padding: 10px;
                                        line-height: 1.0em;
                                        font-size: 0.8em;
                                        border-radius: 6px;
                                        z-index: 999999;
                                    }

                                    #floatingCopyButton:hover {
                                        background: #444654;
                                    }
                                    `;
                                    document.head.appendChild(style);
                                ";
            browser.ExecuteJavaScript(addElementsCode);

            string scriptCode = @"let savedSelection = null;

                                document.body.addEventListener('mousedown', (event) => {
                                    const floatingButton = document.getElementById('floatingCopyButton');
                                    // Check if right-click or Ctrl + click (Mac)
                                    if (event.button === 2 || (event.ctrlKey && event.button === 0)) {
                                        // Save the current selection
                                        savedSelection = window.getSelection().getRangeAt(0);

                                        floatingButton.style.display = 'block';
                                        floatingButton.style.left = event.clientX + 'px';
                                        floatingButton.style.top = event.pageY + 'px';
                                    } else if (event.button === 0 && event.target === floatingButton) {
                                        // Left click on floatingButton
                                        copySelectedText();
                                        floatingButton.style.display = 'none';
                                    } else {
                                        floatingButton.style.display = 'none';
                                    }
                                });

                                document.body.addEventListener('mouseup', (event) => {
                                    if (savedSelection) {
                                        // Restore the saved selection
                                        const selection = window.getSelection();
                                        selection.removeAllRanges();
                                        selection.addRange(savedSelection);

                                        // Clear the saved selection
                                        savedSelection = null;
                                    }
                                });

                                document.addEventListener('keydown', (event) => {
                                    // Check if Cmd + C or Cmd + V (Mac)
                                    if (event.metaKey && event.key === 'c') {
                                        copySelectedText();
                                    } else if (event.metaKey && event.key === 'v') {
                                        const mainTag = document.querySelector('main')
                                        const formTag = mainTag.querySelector('form')
                                        const textarea = formTag.querySelector('textarea')
                                        if (document.activeElement === textarea) {
                                            // クリップボードからペーストする
                                            navigator.clipboard.readText().then(text => {
                                                textarea.value = text
                                            })
                                            setTimeout(function() {
                                                var event = new Event('input', { bubbles: true });  // イベントを作成
                                                textarea.dispatchEvent(event);  // イベントをディスパッチ
                                            }, 300);  // 300ミリ秒の遅延を設ける
                                        }
                                    }
                                });

                                function copySelectedText() {
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
                                ";
            browser.ExecuteJavaScript(scriptCode);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}