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
using System.Reflection;
using Tmds.DBus.Protocol;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using static TmCGPTD.Models.HtmlProcess;
using System.Reactive.Joins;

namespace TmCGPTD.ViewModels
{
    public class ChatViewModel : ViewModelBase
    {
        private AvaloniaCefBrowser _browser;
        private Button _button;
        private Button _button2;
        DatabaseProcess _databaseProcess = new DatabaseProcess();
        HtmlProcess _htmlProcess = new HtmlProcess();

        public ChatViewModel()
        {
            ChatViewFontSize = 16;
            ChatIsRunning = false;
            ChatViewIsVisible = true;

            TitleUpdateCommand = new AsyncRelayCommand(async () => await TitleUpdateAsync());
            CategoryUpdateCommand = new AsyncRelayCommand(async () => await CategoryUpdateAsync());
            InitializeChatCommand = new AsyncRelayCommand(async () => await InitializeChatAsync());
            OpenApiSettingsCommand = new RelayCommand(OpenApiSettings);

            SearchPrev = new AsyncRelayCommand(async () => await TextSearch(VMLocator.MainViewModel.SearchKeyword, false));
            SearchNext = new AsyncRelayCommand(async () => await TextSearch(VMLocator.MainViewModel.SearchKeyword, true));

            _ = InitializeChatAsync();
        }

        public IAsyncRelayCommand TitleUpdateCommand { get; }
        public IAsyncRelayCommand CategoryUpdateCommand { get; }
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
                if(ReEditIsOn)
                {
                    string Code = @"var userDivs = document.querySelectorAll('.user'); // userクラスのdiv要素を取得
                                    for (var i = 0; i < userDivs.length; i++) {
                                      var editDiv = userDivs[i].querySelector('.editDiv'); // editDivクラスのdiv要素を取得
                                      if (editDiv) { // editDivが存在する場合
                                        var assistantDiv = userDivs[i].nextElementSibling; // userクラスの次の兄弟要素（assistantクラスのdiv要素）を取得
                                        userDivs[i].parentElement.removeChild(userDivs[i]); // editDivの親要素を含めて削除
                                        assistantDiv.parentElement.removeChild(assistantDiv); // assistantクラスのdiv要素を削除
                                      }
                                    }";
                    _browser.ExecuteJavaScript(Code);
                }

                await Task.Delay(100);

                string postText = VMLocator.EditorViewModel.GetRecentText().Trim().Trim('\r', '\n');

                string jsCode = $@"var element = document.querySelector('.svg-container');
                                if (element) {{
                                    element.remove();
                                }}";
                _browser.ExecuteJavaScript(jsCode);
                await Task.Delay(100);

                string escapedString = JsonSerializer.Serialize(postText);

                // システムメッセージの処理
                string systemMessage = "";
                if (postText.StartsWith("#system", StringComparison.OrdinalIgnoreCase) || postText.StartsWith("# system", StringComparison.OrdinalIgnoreCase))
                {
                    escapedString = Regex.Replace(postText, @"^#(\s*?)system", "", RegexOptions.IgnoreCase).Trim();

                    // 最初の"---"の位置を検索
                    int separatorIndex = escapedString.IndexOf("---");

                    systemMessage = escapedString.Substring(0, separatorIndex).Trim();//システムメッセージを取得

                    escapedString = escapedString.Substring(separatorIndex+3).Trim();//本文だけ残す
                    escapedString = JsonSerializer.Serialize(escapedString);
                }

                string htmlToAdd = $"<div class=\"user\"><span class=\"userHeader\">[{postDate}] by You</span></div>";

                string systemHtml = $"<div class=\"user\"><span class=\"userHeader\">[{postDate}] by You</span>" +
                                    "<div class=\"codeHeader2\"><span class=\"lang\">System Message</span</div> +" +
                                    $"<pre style=\"margin:0px 0px 2.5em 0px\"><code id=\"headerOn\" class=\"plaintext\">{systemMessage}</code></pre></div>";

                if (systemMessage !="")
                {
                    htmlToAdd = systemHtml;
                }

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

                await Task.Delay(100);

                jsCode = $@"window.scrollTo({{top: document.body.scrollHeight, behavior: 'smooth' }});";
                _browser.ExecuteJavaScript(jsCode);

                await Task.Delay(100);

                htmlToAdd = $"<div class=\"assistant\"><span class=\"thinkingHeader\">Now thinking...</span></div>";

                jsCode = $@"var wrapper = document.getElementById('scrollableWrapper');
                        var newAssistantElement = document.createElement('div');
                        newAssistantElement.innerHTML = `{htmlToAdd}`;
                        wrapper.appendChild(newAssistantElement);";
                _browser.ExecuteJavaScript(jsCode);

                jsCode = $@"window.scrollTo({{top: document.body.scrollHeight, behavior: 'smooth' }});";
                _browser.ExecuteJavaScript(jsCode);

                await Task.Delay(100);

                var resText = await _htmlProcess.PostChatAsync(postText);
                var resDate = DateTime.Now;

                await Task.Delay(100);

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
                await Task.Delay(100);
                // 'thinkingHeader'を削除し、受信中フラグをオフにする
                string removeThinkingHeaderScript = @"
                    var thinkingHeader = document.querySelector('.thinkingHeader');
                    thinkingHeader.parentNode.removeChild(thinkingHeader);
                    window.scrollTo({top: document.body.scrollHeight, behavior: 'smooth' });
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
                    // メッセージを受信し、文字挿入前にスクロール位置が一番下にあった場合のみスクロール実行
                    string insertMessageScript = $@"
                        (() => {{
                            var isBottom = isAtBottom();
                            var receivingDiv = document.getElementById('receivingDiv');
                            {{receivingDiv.innerHTML = {escapedString};}}
                            if (isBottom) {{
                                window.scrollTo({{top: document.body.scrollHeight, behavior: 'smooth' }});
                            }}
                        }})();
                    ";
                    _browser.ExecuteJavaScript(insertMessageScript);

                }

            }
        }


        // 新しいチャットを初期化--------------------------------------------------------------
        public async Task InitializeChatAsync()
        {
            ReEditIsOn = false;
            ChatTitle = "";
            ConversationHistory = new List<Dictionary<string, object>>();
            LastConversationHistory = new List<Dictionary<string, object>>();
            LastId = -1;
            LastPrompt = "";
            HtmlContent = await _htmlProcess.InitializeChatLogToHtml();
            VMLocator.DataGridViewModel.SelectedItem = default;
        }

        // タイトル更新--------------------------------------------------------------
        public async Task TitleUpdateAsync()
        {
            if(string.IsNullOrWhiteSpace(ChatTitle) || LastId == -1)
            {
                return;
            }

            var chatId = LastId;
            var selectedId = VMLocator.DataGridViewModel.SelectedItemIndex;
            try
            {
                _button.Classes.Add("AnimeStart");
                await _databaseProcess.UpdateTitleDatabaseAsync(chatId, ChatTitle);
                if(selectedId >= 0)
                {
                    VMLocator.DataGridViewModel.DataGridIsFocused = true;
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

        // カテゴリ更新--------------------------------------------------------------
        public async Task CategoryUpdateAsync()
        {
            if (ChatCategory == null || LastId == -1)
            {
                return;
            }

            var chatId = LastId;
            var selectedId = VMLocator.DataGridViewModel.SelectedItemIndex;
            try
            {
                _button2.Classes.Add("AnimeStart");
                await _databaseProcess.UpdateCategoryDatabaseAsync(chatId, ChatCategory);
                if (selectedId >= 0)
                {
                    VMLocator.DataGridViewModel.DataGridIsFocused = true;
                    VMLocator.DataGridViewModel.SelectedItemIndex = selectedId;
                }
                await Task.Delay(TimeSpan.FromSeconds(5));
                _button2.Classes.Remove("AnimeStart");
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog() { Title = $"Error: {ex.Message}", PrimaryButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
            }
        }

        // テキスト検索--------------------------------------------------------------
        public async Task TextSearch(string searchKeyword, bool searchDirection, bool searchReset = false)
        {
            if (_browser == null || string.IsNullOrEmpty(VMLocator.MainViewModel.SearchKeyword))
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

        public void PromptEditOn()
        {
            ReEditIsOn = true;
            // JavaScriptから呼び出されるメソッドの実装
            string text = LastPrompt;
            string[] texts = text.Split(new[] { "<---TMCGPT--->" }, StringSplitOptions.None);
            for (int i = 0, loopTo = Math.Min(texts.Length - 1, 4); i <= loopTo; i++) // 5要素目までを取得
            {
                string propertyName = $"Editor{i + 1}Text";
                PropertyInfo property = VMLocator.EditorViewModel.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    property.SetValue(VMLocator.EditorViewModel, string.Empty);
                    if (!string.IsNullOrWhiteSpace(texts[i]))
                    {
                        property.SetValue(VMLocator.EditorViewModel, texts[i].Trim()); // 空白を削除して反映
                    }
                }
            }

        }

        public void PromptEditOff()
        {
            ReEditIsOn = false;
            // JavaScriptから呼び出されるメソッドの実装
            VMLocator.EditorViewModel.TextClear();
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

        public void SetButtonWrite2(Button button)
        {
            _button2 = button;
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
            VMLocator.WebChatBardViewModel.WebChatBardViewIsVisible = false;
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

        private bool _reEditIsOn;
        public bool ReEditIsOn //Postボタンの表示切り替え用
        {
            get => _reEditIsOn;
            set
            {
                if (SetProperty(ref _reEditIsOn, value))
                {
                    if (value)
                    {
                        VMLocator.MainViewModel.PostButtonText = "Edit";
                    }
                    else
                    {
                        VMLocator.MainViewModel.PostButtonText = "Post";
                    }
                }
            }
        }

        private string _chatTitle;
        public string ChatTitle
        {
            get => _chatTitle;
            set => SetProperty(ref _chatTitle, value);
        }

        private string _chatCategory;
        public string ChatCategory
        {
            get => _chatCategory;
            set => SetProperty(ref _chatCategory, value);
        }

        private string _lastPrompt;
        public string LastPrompt
        {
            get => _lastPrompt;
            set => SetProperty(ref _lastPrompt, value);
        }

        private List<Dictionary<string, object>> _conversationHistory;
        public List<Dictionary<string, object>> ConversationHistory
        {
            get => _conversationHistory;
            set => SetProperty(ref _conversationHistory, value);
        }

        private List<Dictionary<string, object>> _lastConversationHistory;
        public List<Dictionary<string, object>> LastConversationHistory
        {
            get => _lastConversationHistory;
            set => SetProperty(ref _lastConversationHistory, value);
        }

        private double _chatViewFontSize;
        public double ChatViewFontSize
        {
            get => _chatViewFontSize;
            set => SetProperty(ref _chatViewFontSize, value);
        }
    }
}