using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Xilium.CefGlue.Avalonia;
using Avalonia.Controls;
using TmCGPTD.Models;
using System.Text.Json;
using System.Diagnostics;
using Avalonia.Threading;

namespace TmCGPTD.ViewModels
{
    public class ChatViewModel : ViewModelBase
    {
        private AvaloniaCefBrowser _browser;
        private Button _button;
        DatabaseProcess _databaseProcess = new DatabaseProcess();
        HtmlProcess _htmlProcess = new HtmlProcess();

        public ChatViewModel()
        {
            ChatViewFontSize = 16;
            ChatIsRunning = false;
            ChatViewIsVisible = true;

            TitleUpdateCommand = new AsyncRelayCommand(async () => await TitleUpdateAsync());
            InitializeChatCommand = new AsyncRelayCommand(async () => await InitializeChatAsync());
            OpenApiSettingsCommand = new RelayCommand(OpenApiSettings);

            SearchPrev = new AsyncRelayCommand(async () => await TextSearch(SearchKeyword, false));
            SearchNext = new AsyncRelayCommand(async () => await TextSearch(SearchKeyword, true));

            LastId = default;
        }

        public IAsyncRelayCommand TitleUpdateCommand { get; }
        public IAsyncRelayCommand InitializeChatCommand { get; }
        public ICommand OpenApiSettingsCommand { get; }
        public IAsyncRelayCommand SearchPrev { get; }
        public IAsyncRelayCommand SearchNext { get; }

        public async Task GoChatAsync()
        {
            if (ChatIsRunning)//チャット実行中の場合はキャンセル
            {
                return;
            }
            ChatIsRunning = true;

            var postDate = DateTime.Now;
            if (LastId !>= 0 && string.IsNullOrWhiteSpace(ChatTitle)) //チャット表示無ければ新規と判断
            {
                await InitializeChatAsync();
                await Task.Delay(500);
            }

            try
            {
                string postText = VMLocator.EditorViewModel.RecentText.Trim().Trim('\r', '\n');
                string escapedString = JsonSerializer.Serialize(postText);

                string jsCode = $@"var element = document.querySelector('.svg-container');
                                if (element) {{
                                    element.remove();
                                }}";
                _browser.ExecuteJavaScript(jsCode);

                await Task.Delay(100);

                string htmlToAdd = $"<div class=\"user\"><span class=\"userHeader\">[{postDate}] by You</span></div>";

                jsCode = $@"var wrapper = document.getElementById('scrollableWrapper');
                        var newUserElement = document.createElement('div');
                        newUserElement.innerHTML = `{htmlToAdd}`;
                        var newTextElement = document.createElement('div');
                        newTextElement.style.whiteSpace = 'pre-wrap';
                        newTextElement.id = 'document';
                        newTextElement.innerText = {escapedString};
                        newUserElement.querySelector('.userHeader').parentNode.appendChild(newTextElement);
                        wrapper.appendChild(newUserElement);";
                _browser.ExecuteJavaScript(jsCode);

                htmlToAdd = $"<div class=\"assistant\"><span class=\"thinkingHeader\">Now thinking...</span></div>";

                jsCode = $@"var wrapper = document.getElementById('scrollableWrapper');
                        var newAssistantElement = document.createElement('div');
                        newAssistantElement.innerHTML = `{htmlToAdd}`;
                        wrapper.appendChild(newAssistantElement);";
                _browser.ExecuteJavaScript(jsCode);

                var resText = await _htmlProcess.PostChatAsync(postText);
                var resDate = DateTime.Now;

                await _databaseProcess.InsertDatabaseChatAsync(postDate, postText, resDate, resText);

                await Task.Delay(100);

                jsCode = $@"window.scrollTo({{top: document.body.scrollHeight, behavior: 'smooth' }});";
                _browser.ExecuteJavaScript(jsCode);
            }
            catch (Exception ex)
            {
                var htmlToAdd = $"<span class=\"assistantHeader\">[Error]</span><div style=\"white-space: pre-wrap\" id=\"document\">{ex.Message}</div>";
                string escapedString = JsonSerializer.Serialize(htmlToAdd);

                var jsCode = $@"var wrapper = document.getElementById('scrollableWrapper');
                        var thinkingHeader = wrapper.querySelector('.thinkingHeader');
                        if (thinkingHeader) {{
                            var newElement = document.createElement('div');
                            newElement.innerHTML = {escapedString};
                            thinkingHeader.parentNode.insertBefore(newElement, thinkingHeader);
                            thinkingHeader.parentNode.removeChild(thinkingHeader);
                        }} else {{
                            var newElement = document.createElement('div');
                            newElement.innerHTML = {escapedString};
                            wrapper.appendChild(newElement);
                        }}
                        window.scrollTo({{top: document.body.scrollHeight, behavior: 'smooth' }});";
                _browser.ExecuteJavaScript(jsCode);
                ChatIsRunning = false;
                throw;
            }

            ChatIsRunning = false;
        }

        // チャット受信メソッド--------------------------------------------------------------
        private bool isReceiving = false;

        public async Task UpdateUIWithReceivedMessage(string message)
        {
            var resDate = DateTime.Now;
            var convertedHtml = await _htmlProcess.ConvertAddLogToHtml(message, resDate);
            var escapedString = JsonSerializer.Serialize(convertedHtml);

            if (message == "[DONE]")
            {
                // 'thinkingHeader'を削除し、受信中フラグをオフにする
                string removeThinkingHeaderScript = @"
                    var thinkingHeader = document.querySelector('.thinkingHeader');
                    thinkingHeader.parentNode.removeChild(thinkingHeader);
                    window.scrollTo({{top: document.body.scrollHeight, behavior: 'smooth' }});
                ";
                _browser.ExecuteJavaScript(removeThinkingHeaderScript);
                isReceiving = false;
            }
            else
            {
                if (!isReceiving)
                {
                    // 受信中フラグをオンにし、新しいdivを作成
                    isReceiving = true;
                    string createDivScript = $@"
                    var newDiv = document.createElement('div');
                    newDiv.id = 'receivingDiv';
                    var thinkingHeader = document.querySelector('.thinkingHeader');
                    thinkingHeader.parentNode.insertBefore(newDiv, thinkingHeader.nextSibling);
                ";
                    _browser.ExecuteJavaScript(createDivScript);
                }
                else
                {
                    // 受け取ったメッセージを挿入
                    string insertMessageScript = $@"
                    var receivingDiv = document.getElementById('receivingDiv');
                    receivingDiv.innerHTML = {escapedString};
                ";
                    _browser.ExecuteJavaScript(insertMessageScript);
                }

            }
        }


        // 新しいチャットを初期化--------------------------------------------------------------
        public async Task InitializeChatAsync()
        {
            ChatTitle = "";
            ConversationHistory = new List<Dictionary<string, object>>();
            LastId = default;
            HtmlContent = await _htmlProcess.InitializeChatLogToHtml();
            VMLocator.DataGridViewModel.SelectedItem = default;
        }

        // タイトル更新--------------------------------------------------------------
        public async Task TitleUpdateAsync()
        {
            if(string.IsNullOrWhiteSpace(ChatTitle) || LastId == default)
            {
                return;
            }

            var chatId = LastId;
            var selectedId = VMLocator.DataGridViewModel.SelectedItemIndex;
            try
            {
                _button.Classes.Add("AnimeStart");
                await _databaseProcess.UpdateTitleDatabaseAsync(chatId, ChatTitle);
                await _databaseProcess.GetChatLogDatabaseAsync(chatId);
                if(selectedId >= 0)
                {
                    VMLocator.DataGridViewModel.SelectedItemIndex = selectedId;
                }
                await Task.Delay(TimeSpan.FromSeconds(5));
                _button.Classes.Remove("AnimeStart");
            }
            catch(Exception ex)
            {
                var dialog = new ContentDialog() { Title = $"Error: {ex.Message}", PrimaryButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
            }
        }

        // テキスト検索--------------------------------------------------------------
        public async Task TextSearch(string searchKeyword, bool searchDirection, bool searchReset = false)
        {
            if (_browser == null || string.IsNullOrEmpty(SearchKeyword))
            {
                return;
            }
            string script = @"function executeSearchText()
                    {
                        if (typeof searchText === 'function')
                        {" +
                            $"searchText('{searchKeyword}', {searchDirection.ToString().ToLower()}, {searchReset.ToString().ToLower()});" +
                        @"}
                        else
                        {
                            setTimeout(executeSearchText, 100); // 100ミリ秒後に再試行
                        }
                    }
                    executeSearchText();";
            try
            {
                await _browser.EvaluateJavaScript<ValueTuple<int, int>>(script);
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog() { Title = $"Error: {ex.Message}", PrimaryButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
            }
            return;
        }

        // Browserインスタンスを受け取る
        public async void SetBrowser(AvaloniaCefBrowser browser)
        {
            _browser = browser;
            HtmlContent = await _htmlProcess.InitializeChatLogToHtml();
        }

        // ButtonWriteインスタンスを受け取る
        public void SetButtonWrite(Button button)
        {
            _button = button;
        }

        private string _searchKeyword;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private string _htmlContent;
        public string HtmlContent
        {
            get => _htmlContent;
            set
            {
                _htmlContent = value;
                OnPropertyChanged(nameof(HtmlContent));
            }
        }
        
        public void OpenApiSettings()
        {
            VMLocator.ChatViewModel.ChatViewIsVisible = false;
            VMLocator.WebChatViewModel.WebChatViewIsVisible = false;
            VMLocator.MainWindowViewModel.ApiSettingIsOpened = true;
        }

        private bool _chatIsRunning;
        public bool ChatIsRunning //チャット実行中フラグ
        {
            get => _chatIsRunning;
            set => SetProperty(ref _chatIsRunning, value);
        }

        private long _lastId;
        public long LastId //最終選択ID
        {
            get => _lastId;
            set => SetProperty(ref _lastId, value);
        }

        private bool _chatViewIsVisible;
        public bool ChatViewIsVisible //ダイアログ表示用
        {
            get => _chatViewIsVisible;
            set => SetProperty(ref _chatViewIsVisible, value);
        }

        private string _chatTitle;
        public string ChatTitle
        {
            get => _chatTitle;
            set => SetProperty(ref _chatTitle, value);
        }

        private List<Dictionary<string, object>> _conversationHistory;
        public List<Dictionary<string, object>> ConversationHistory
        {
            get => _conversationHistory;
            set => SetProperty(ref _conversationHistory, value);
        }

        private double _chatViewFontSize;
        public double ChatViewFontSize
        {
            get => _chatViewFontSize;
            set => SetProperty(ref _chatViewFontSize, value);
        }

        private bool _logPainIsOpened;
        public bool LogPainIsOpened
        {
            get => _logPainIsOpened;
            set
            {
                if (SetProperty(ref _logPainIsOpened, value))
                {
                    VMLocator.MainViewModel.LogPainIsOpened = _logPainIsOpened;
                }
            }
        }

        private bool _logPainButtonIsVisible;
        public bool LogPainButtonIsVisible
        {
            get => _logPainButtonIsVisible;
            set => SetProperty(ref _logPainButtonIsVisible, value);
        }
    }
}