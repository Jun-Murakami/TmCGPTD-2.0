﻿using Avalonia.Controls;
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

        private async void Browser_LoadEnd(object sender, LoadEndEventArgs e)
        {
            string addElementsCode = @"
                                    const button = document.createElement('button');
                                    button.id = 'floatingCopyButton';
                                    button.innerHTML = 'Copy to clipboard';
                                    document.body.appendChild(button);

                                    const style = document.createElement('style');
                                    style.type = 'text/css';
                                    style.innerHTML = `
                                    #floatingCopyButton {
                                        position: absolute;
                                        display: none;
                                        background: #343541;
                                        border-width: 1px;
                                        border: #545563 solid;
                                        cursor: pointer;
                                        padding: 10px;
                                        line-height: 1.0em;
                                        font-size: 0.8em;
                                        border-radius: 6px;
                                    }

                                    #floatingCopyButton:hover {
                                        background: #444654;
                                    }
                                    `;
                                    document.head.appendChild(style);
                                ";
            browser.ExecuteJavaScript(addElementsCode);

            string scriptCode = @"
                                const floatingButton = document.getElementById('floatingCopyButton');
                                let savedSelection = null;

                                document.body.addEventListener('mousedown', (event) => {
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
                                    // Check if Cmd + C (Mac)
                                    if (event.metaKey && event.key === 'c') {
                                        copySelectedText();
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