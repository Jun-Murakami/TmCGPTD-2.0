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
using static TmCGPTD.Models.ChatProcess;
using System.Threading;

namespace TmCGPTD.ViewModels
{
    public class ChatViewModel : ViewModelBase
    {
        private AvaloniaCefBrowser _browser;
        private Button _button;
        private Button _button2;
        DatabaseProcess _databaseProcess = new DatabaseProcess();
        HtmlProcess _htmlProcess = new HtmlProcess();
        ChatProcess _chatProcess = new ChatProcess();

        public ChatViewModel()
        {
            ChatViewFontSize = 16;
            ChatIsRunning = false;
            ChatViewIsVisible = true;

            TitleUpdateCommand = new AsyncRelayCommand(async () => await TitleUpdateAsync());
            CategoryUpdateCommand = new AsyncRelayCommand(async () => await CategoryUpdateAsync());
            InitializeChatCommand = new AsyncRelayCommand(async () => await InitializeChatAsync());
            OpenApiSettingsCommand = new RelayCommand(OpenApiSettings);

            ShowSystemMessageInfoCommand = new RelayCommand(ShowSystemMessageInfo);

            SearchPrev = new AsyncRelayCommand(async () => await TextSearch(VMLocator.MainViewModel.SearchKeyword, false));
            SearchNext = new AsyncRelayCommand(async () => await TextSearch(VMLocator.MainViewModel.SearchKeyword, true));

            _ = InitializeChatAsync();
        }

        public IAsyncRelayCommand TitleUpdateCommand { get; }
        public IAsyncRelayCommand CategoryUpdateCommand { get; }
        public IAsyncRelayCommand InitializeChatCommand { get; }
        public ICommand OpenApiSettingsCommand { get; }
        public ICommand ShowSystemMessageInfoCommand { get; }
        public IAsyncRelayCommand SearchPrev { get; }
        public IAsyncRelayCommand SearchNext { get; }

        public async Task<bool> GoChatAsync(CancellationToken token)
        {
            if (ChatIsRunning)//�`���b�g���s���̏ꍇ�̓L�����Z��
            {
                return true;
            }
            ChatIsRunning = true;

            var postDate = DateTime.Now;
            if (LastId < 0) //�`���b�g�\��������ΐV�K�Ɣ��f
            {
                await InitializeChatAsync();
                await Task.Delay(500);
            }

            try
            {
                // �ĕҏW���[�h���̏�����
                if (ReEditIsOn)
                {
                    string Code = @"var userDivs = document.querySelectorAll('.user'); // user�N���X��div�v�f���擾
                                    for (var i = 0; i < userDivs.length; i++) {
                                      var editDiv = userDivs[i].querySelector('.editDiv'); // editDiv�N���X��div�v�f���擾
                                      if (editDiv) { // editDiv�����݂���ꍇ
                                        var assistantDiv = userDivs[i].nextElementSibling; // user�N���X�̎��̌Z��v�f�iassistant�N���X��div�v�f�j���擾
                                        userDivs[i].parentElement.removeChild(userDivs[i]); // editDiv�̐e�v�f���܂߂č폜
                                        assistantDiv.parentElement.removeChild(assistantDiv); // assistant�N���X��div�v�f���폜
                                      }
                                    }";
                    _browser.ExecuteJavaScript(Code);
                }

                // ���[�U�[���͂��擾
                string postText = VMLocator.EditorViewModel.GetRecentText().Trim().Trim('\r', '\n');

                // ���S���폜
                string jsCode = $@"var element = document.querySelector('.svg-container');
                                if (element) {{
                                    element.remove();
                                }}";
                _browser.ExecuteJavaScript(jsCode);


                string escapedString = JsonSerializer.Serialize(postText);

                string htmlToAdd = $"<span class=\"userHeader\">[{postDate}] by You</span>";

                string additonalJsCode = "";
                bool isOnlySystemMessage = false;
                string postTextBody = "";

                // �V�X�e�����b�Z�[�W�̏���
                string systemMessage = "";
                if (Regex.IsMatch(postText, @"^#\s*system", RegexOptions.IgnoreCase))
                {
                    string tempString = Regex.Replace(postText, @"^#(\s*?)system", "", RegexOptions.IgnoreCase).Trim();

                    // �ŏ���"---"�̈ʒu������
                    int separatorIndex = tempString.IndexOf("---");
                    if (separatorIndex != -1)
                    {
                        systemMessage = tempString.Substring(0, separatorIndex).Trim();//�V�X�e�����b�Z�[�W���擾
                        tempString = tempString.Substring(separatorIndex + 3).Trim();//�{�������c��
                    }
                    else
                    {
                        systemMessage = tempString.Trim();//���݂��Ȃ���΃V�X�e�����b�Z�[�W�̂�
                        tempString = "";
                        isOnlySystemMessage = true;
                    }

                    if (string.IsNullOrWhiteSpace(systemMessage))
                    {
                        systemMessage = "System messages were turned off.";
                    }

                    postTextBody = tempString;
                    escapedString = JsonSerializer.Serialize(tempString);

                    htmlToAdd = $"<span class=\"userHeader\">[{postDate}] by You</span>" +
                                "<div class=\"codeHeader2\"><span class=\"lang\">System Message</span></div>" +
                                $"<pre style=\"margin:0px 0px 2.5em 0px\"><code id=\"headerOn\" class=\"plaintext\">{systemMessage}</code></pre>";
                    additonalJsCode = $@"hljs.highlightAll();";
                }


                // ���[�U�[�̗v�f�𐶐�
                jsCode = $@"var wrapper = document.getElementById('scrollableWrapper');
                        var newUserElement = document.createElement('div');
                        newUserElement.className = 'user';
                        newUserElement.innerHTML = `{htmlToAdd}`;
                        var newTextElement = document.createElement('div');
                        newTextElement.style.whiteSpace = 'pre-wrap';
                        newTextElement.id = 'document';
                        newTextElement.innerText = {escapedString};
                        newUserElement.querySelector('.userHeader').parentNode.appendChild(newTextElement);
                        wrapper.appendChild(newUserElement);
                        {additonalJsCode}";
                _browser.ExecuteJavaScript(jsCode);

                jsCode = $@"window.scrollTo({{top: document.body.scrollHeight, behavior: 'smooth' }});";
                _browser.ExecuteJavaScript(jsCode);

                // �A�V�X�^���g�̗v�f�𐶐��B�V�X�e�����b�Z�[�W�݂̂̏ꍇ�̓X�L�b�v
                if (!isOnlySystemMessage)
                {
                    htmlToAdd = $"<span class=\"thinkingHeader\">Now thinking...</span>";

                    jsCode = $@"var wrapper = document.getElementById('scrollableWrapper');
                        var newAssistantElement = document.createElement('div');
                        newAssistantElement.className = 'assistant';
                        newAssistantElement.innerHTML = `{htmlToAdd}`;
                        wrapper.appendChild(newAssistantElement);";
                    _browser.ExecuteJavaScript(jsCode);

                    jsCode = $@"window.scrollTo({{top: document.body.scrollHeight, behavior: 'smooth' }});";
                    _browser.ExecuteJavaScript(jsCode);
                }

                // �����̃V�X�e�����b�Z�[�W���f�B�[�v�R�s�[
                Dictionary<string, object>? oldSystemMessage = null;
                foreach (var item in ConversationHistory)
                {
                    if (item.ContainsKey("role") && item["role"].ToString() == "system" && item.ContainsKey("content"))
                    {
                        string json = JsonSerializer.Serialize(item);
                        oldSystemMessage = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                        break;
                    }
                }

                // �p�����[�^�𐶐�
                ChatParameters postParameters = new ChatParameters {
                    UserInput = postText,
                    UserInputBody = postTextBody,
                    AssistantResponse = "",
                    ChatTitle = VMLocator.ChatViewModel.ChatTitle,
                    OldSystemMessageDic = oldSystemMessage,
                    NewSystemMessageStr = systemMessage,
                    ConversationHistory = ConversationHistory,
                    PostedConversationHistory = null,
                };

                // �V�X�e�����b�Z�[�W�݂̂̏ꍇ�͓��e���Ȃ�
                string resText = "";
                if (isOnlySystemMessage)
                {
                    // �����̃V�X�e�����b�Z�[�W������΍폜
                    var itemToRemove = GetSystemMessageItem(ConversationHistory);
                    if (itemToRemove != null)
                    {
                        ConversationHistory!.Remove(itemToRemove);
                    }

                    // �V�����V�X�e�����b�Z�[�W������Ή�b�����̐擪�ɒǉ�
                    var systemInput = new Dictionary<string, object>() { { "role", "system" }, { "content", systemMessage } };
                    if (!string.IsNullOrWhiteSpace(systemMessage))
                    {
                        ConversationHistory!.Insert(0, systemInput);
                    }

                    if (string.IsNullOrEmpty(ChatCategory))
                    {
                        ChatCategory = "API Chat";
                    }
                }
                else
                {
                    // �L�����Z���{�^����\��
                    jsCode = @"var stopButton = document.getElementById('stopButton');
                               stopButton.style.display = 'block';";
                    _browser.ExecuteJavaScript(jsCode);

                    await Task.Delay(100);

                    // ���b�Z�[�W���e /////////////////////////////////////////////////
                    resText = await _chatProcess.PostChatAsync(postParameters, token);


                    if (string.IsNullOrWhiteSpace(resText)) //�ԓ����󂾂�����ԓ��O�ɃL�����Z�����ꂽ�Ɣ��f����
                    {
                        jsCode = $@"var wrapper = document.getElementById('scrollableWrapper');
                                var userElements = wrapper.getElementsByClassName('user');
                                var assistantElements = wrapper.getElementsByClassName('assistant');
                                if(userElements.length > 0 && assistantElements.length > 0) {{
                                    userElements[userElements.length - 1].remove(); // �Ō��'user'�v�f���폜
                                    assistantElements[assistantElements.length - 1].remove(); // �Ō��'assistant'�v�f���폜
                                }}
                            ";
                        _browser.ExecuteJavaScript(jsCode);

                        jsCode = $@"window.scrollTo({{top: document.body.scrollHeight, behavior: 'smooth' }});
                                    var stopButton = document.getElementById('stopButton');
                                    stopButton.style.display = 'none';";
                        _browser.ExecuteJavaScript(jsCode);
                        ChatIsRunning = false;
                        return false;
                    }

                    // �L�����Z���{�^�����\��
                    jsCode = @"var stopButton = document.getElementById('stopButton');
                               stopButton.style.display = 'none';";
                    _browser.ExecuteJavaScript(jsCode);

                    //��b�������������_�Ń^�C�g�����󗓂�������^�C�g����������������
                    if (string.IsNullOrEmpty(ChatTitle))
                    {
                        ChatTitle = await _chatProcess.GetTitleAsync(ConversationHistory);
                    }
                }

                // �J�e�S���[���󗓂�������uAPI Chat�v�������ݒ肷��
                if (string.IsNullOrEmpty(VMLocator.ChatViewModel.ChatCategory))
                {
                    VMLocator.ChatViewModel.ChatCategory = "API Chat";
                }

                var resDate = DateTime.Now;

                // �f�[�^�x�[�X���X�V
                await _databaseProcess.InsertDatabaseChatAsync(postDate, postText, resDate, resText);

                await Task.Delay(700);

                // �G�f�B�b�g�{�^��������
                jsCode = $@"var wrapper = document.getElementById('scrollableWrapper');
                        var editDivs = wrapper.querySelectorAll('.editDiv');
                        editDivs.forEach(function(editDiv) {{
                            editDiv.parentNode.removeChild(editDiv);
                        }});
                        var userDivs = wrapper.querySelectorAll('.user');
                        var lastUserDiv = userDivs[userDivs.length - 1];
                        var documentDivs = lastUserDiv.querySelectorAll('#document');
                        var lastDocumentDiv = documentDivs[documentDivs.length - 1];
                        if (lastDocumentDiv) {{
                            lastDocumentDiv.innerHTML += '<br/><br/><div class=""editDiv""><button class=""editButton"">Edit</button></div>';
                            const editButtons = document.querySelectorAll('.editButton');
                            editButtons.forEach(button => button.addEventListener('click', switchEdit));
                        }}
                    ";
                _browser.ExecuteJavaScript(jsCode);

                jsCode = $@"window.scrollTo({{top: document.body.scrollHeight, behavior: 'smooth' }});";
                _browser.ExecuteJavaScript(jsCode);

            }
            catch (Exception ex)
            {
                // �L�����Z���{�^�����\��
                string jsCode = @"var stopButton = document.getElementById('stopButton');
                                  stopButton.style.display = 'block';";
                _browser.ExecuteJavaScript(jsCode);

                // �G���[���b�Z�[�W��\��
                var htmlToAdd = $"<span class=\"assistantHeader\">[Error]</span><div style=\"white-space: pre-wrap\" id=\"document\">{ex.Message}</div>";
                string escapedString = JsonSerializer.Serialize(htmlToAdd);

                jsCode = $@"var wrapper = document.getElementById('scrollableWrapper');
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
                //throw;
            }

            ChatIsRunning = false;
            return true;
        }
        private Dictionary<string, object>? GetSystemMessageItem(List<Dictionary<string, object>>? conversationHistory)
        {
            foreach (var item in conversationHistory!)
            {
                if (item.ContainsKey("role") && item["role"].ToString() == "system" && item.ContainsKey("content"))
                {
                    return item;
                }
            }
            return null;
        }

        // �`���b�g��M���\�b�h--------------------------------------------------------------
        private bool isReceiving = false;
        private string? postedHtml = "";

        public async Task UpdateUIWithReceivedMessage(string? message,string chatText)
        {
            bool isUpdateTag = false;
            var resDate = DateTime.Now;
            string convertedHtml = await _htmlProcess.ConvertAddLogToHtml(chatText, resDate);
            string escapedHtml = JsonSerializer.Serialize(convertedHtml);
            string escapedString = JsonSerializer.Serialize(message);

            // �^�O���ǉ����ꂽ���ǂ����𔻒�
            if (convertedHtml.Length != (postedHtml + message).Length)
            {
                isUpdateTag = true;
            }

            if (message == "[ERROR]") // �G���[�����������ꍇ
            {
                string insertMessageScript = $@"
                        (() => {{
                            var wrapper = document.getElementById('scrollableWrapper');
                            var thinkingHeader = wrapper.querySelector('.thinkingHeader');
                            if (thinkingHeader) {{
                                thinkingHeader.parentNode.removeChild(thinkingHeader);
                            }} 
                            var isBottom = isAtBottom5();
                            const assistantElements = document.getElementsByClassName('assistant');
                            const lastAssistantElement = assistantElements[assistantElements.length - 1];
                            lastAssistantElement.innerHTML = {escapedHtml};
                            hljs.highlightAll();
                            window.scrollTo({{top: document.body.scrollHeight, behavior: 'smooth' }});
                        }})();
                    ";
                _browser.ExecuteJavaScript(insertMessageScript);
            }
            else if (message == "[CANCEL]") // �L�����Z�����ꂽ�ꍇ
            {
                string insertMessageScript = $@"
                        (() => {{
                            var wrapper = document.getElementById('scrollableWrapper');
                            var thinkingHeader = wrapper.querySelector('.thinkingHeader');
                            if (thinkingHeader) {{
                                thinkingHeader.parentNode.removeChild(thinkingHeader);
                            }} 
                            var isBottom = isAtBottom5();
                            const assistantElements = document.getElementsByClassName('assistant');
                            const lastAssistantElement = assistantElements[assistantElements.length - 1];
                            lastAssistantElement.innerHTML = {escapedHtml};
                            hljs.highlightAll();
                            window.scrollTo({{top: document.body.scrollHeight, behavior: 'smooth' }});
                        }})();
                    ";
                _browser.ExecuteJavaScript(insertMessageScript);
            }
            else
            {
                if (!isReceiving)
                {
                    postedHtml = message;
                    // ��M���t���O���I���ɂ���
                    isReceiving = true;
                }
                else
                {
                    string insertMessageScript;
                    if (isUpdateTag)
                    {
                        // �^�O�̍X�V���������ꍇ�̓��b�Z�[�W�S�̂����ւ��A�X�N���[���ʒu����ԉ��ɂ������ꍇ�̂݃X�N���[�����s
                        insertMessageScript = $@"
                            (() => {{
                                var isBottom = isAtBottom5();
                                const assistantElements = document.getElementsByClassName('assistant');
                                const lastAssistantElement = assistantElements[assistantElements.length - 1];
                                lastAssistantElement.innerHTML = {escapedHtml};
                                hljs.highlightAll();
                                if (isBottom) {{
                                    if (!isAtBottom()){{
                                        window.scrollTo({{top: document.body.scrollHeight, behavior: 'smooth' }});
                                    }}
                                }}
                                const copyButtons = document.querySelectorAll('#copyButton');
                                copyButtons.forEach(button => button.addEventListener('click', copyCode));
                            }})();
                        ";
                        _browser.ExecuteJavaScript(insertMessageScript);
                        postedHtml = convertedHtml;

                        // �I������ 'thinkingHeader'���폜���A��M���t���O���I�t�ɂ���
                        if (message == "[DONE]")
                        {
                            string removeThinkingHeaderScript = @"
                                window.scrollTo({top: document.body.scrollHeight, behavior: 'smooth' });
                            ";
                            _browser.ExecuteJavaScript(removeThinkingHeaderScript);
                            isReceiving = false;
                        }
                    }
                    else
                    {
                        // �^�O�̍X�V���Ȃ��ꍇ�́A���b�Z�[�W�݂̂�ǉ�
                        insertMessageScript = $@"
                            (() => {{
                                var isBottom = isAtBottom5();
                                const assistantElements = document.getElementsByClassName('assistant');
                                const lastAssistantElement = assistantElements[assistantElements.length - 1];
                                const lastDivInLastAssistantElement = lastAssistantElement.querySelector(':scope > div:last-child');
                                let newSpan = document.createElement('span');
                                newSpan.innerText = {escapedString};
                                lastDivInLastAssistantElement.appendChild(newSpan);
                                if (isBottom) {{
                                    if (!isAtBottom()){{
                                        window.scrollTo({{top: document.body.scrollHeight, behavior: 'smooth' }});
                                    }}
                                }}
                            }})();
                        ";
                        _browser.ExecuteJavaScript(insertMessageScript);
                        postedHtml += message;
                    }
                }
            }
        }

        // �V�����`���b�g��������--------------------------------------------------------------
        public async Task InitializeChatAsync()
        {
            ReEditIsOn = false;
            ChatTitle = "";
            ChatCategory = "";
            ConversationHistory = new List<Dictionary<string, object>>();
            LastConversationHistory = new List<Dictionary<string, object>>();
            LastId = -1;
            LastPrompt = "";
            HtmlContent = await _htmlProcess.InitializeChatLogToHtml();
        }

        // �^�C�g���X�V--------------------------------------------------------------
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

        // �J�e�S���X�V--------------------------------------------------------------
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

        // �e�L�X�g����--------------------------------------------------------------
        public async Task TextSearch(string searchKeyword, bool searchDirection, bool searchReset = false)
        {
            if (_browser == null || string.IsNullOrEmpty(searchKeyword))
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
                            setTimeout(executeSearchText, 100); // 100�~���b��ɍĎ��s
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

        // �v�����v�g�ĕҏW���I���ɂ���--------------------------------------------------------------
        public void PromptEditOn()
        {
            ReEditIsOn = true;
            // JavaScript����Ăяo����郁�\�b�h�̎���
            string text = LastPrompt;
            string[] texts = text.Split(new[] { "<---TMCGPT--->" }, StringSplitOptions.None);
            for (int i = 0, loopTo = Math.Min(texts.Length - 1, 4); i <= loopTo; i++) // 5�v�f�ڂ܂ł��擾
            {
                string propertyName = $"Editor{i + 1}Text";
                PropertyInfo property = VMLocator.EditorViewModel.GetType().GetProperty(propertyName)!;
                if (property != null)
                {
                    property.SetValue(VMLocator.EditorViewModel, string.Empty);
                    if (!string.IsNullOrWhiteSpace(texts[i]))
                    {
                        property.SetValue(VMLocator.EditorViewModel, texts[i].Trim()); // �󔒂��폜���Ĕ��f
                    }
                }
            }
        }

        // �v�����v�g�ĕҏW���I�t�ɂ���--------------------------------------------------------------
        public void PromptEditOff()
        {
            ReEditIsOn = false;
            VMLocator.EditorViewModel.TextClear();
        }

        // �V�X�e�����b�Z�[�W�̕\��--------------------------------------------------------------
        private void ShowSystemMessageInfo()
        {
            string? SystemMessage = "";

            foreach (var item in ConversationHistory)
            {
                if (item.ContainsKey("role") && item["role"].ToString() == "system" && item.ContainsKey("content"))
                {
                    SystemMessage = item["content"].ToString();
                    break;
                }
            }

            if (string.IsNullOrEmpty(SystemMessage))
            {
                Avalonia.Application.Current!.TryFindResource("My.Strings.SystemMessageInfo", out object? resource1);
                SystemMessage = resource1!.ToString();
            }

            string escapedHtml = JsonSerializer.Serialize(SystemMessage);


            var jsCode = $@"
                        (() => {{
                            var floatingSystemMessageInfo = document.getElementById('floatingSystemMessageInfo');
                            if (floatingSystemMessageInfo.style.display === 'block') {{
                                floatingSystemMessageInfo.style.opacity = 0.95;
                                floatingSystemMessageInfo.style.transition = 'opacity 0.5s';
                                floatingSystemMessageInfo.style.opacity = 0;
                                setTimeout(function() {{
                                    
                                    floatingSystemMessageInfo.style.display = 'none';
                                }}, 1000);
                                return;
                            }}

                            var systemMessageElement = document.querySelector('#floatingSystemMessageInfo div.codeBody');
                            systemMessageElement.innerText = {escapedHtml};

                            floatingSystemMessageInfo.style.display = 'block';
                            floatingSystemMessageInfo.style.opacity = 0.95;
                        }})();";
            _browser.ExecuteJavaScript(jsCode);

        }

        // Browser�C���X�^���X���󂯎��
        public async void SetBrowser(AvaloniaCefBrowser browser)
        {
            _browser = browser;
            HtmlContent = await _htmlProcess.InitializeChatLogToHtml();
        }

        // ButtonWrite�C���X�^���X���󂯎��
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
        public bool ChatIsRunning //�`���b�g���s���t���O
        {
            get => _chatIsRunning;
            set => SetProperty(ref _chatIsRunning, value);
        }

        private long _lastId;
        public long LastId //�ŏI�I��ID
        {
            get => _lastId;
            set => SetProperty(ref _lastId, value);
        }

        private bool _chatViewIsVisible;
        public bool ChatViewIsVisible //�_�C�A���O�\���p
        {
            get => _chatViewIsVisible;
            set => SetProperty(ref _chatViewIsVisible, value);
        }

        private bool _reEditIsOn;
        public bool ReEditIsOn //Post�{�^���̕\���؂�ւ��p
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