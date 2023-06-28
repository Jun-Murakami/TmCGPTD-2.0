using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Xilium.CefGlue.Avalonia;
using FluentAvalonia.UI.Controls;
using TmCGPTD.Models;

namespace TmCGPTD.ViewModels
{
    public class WebChatViewModel : ViewModelBase
    {
        private AvaloniaCefBrowser _browser;
        HtmlProcess _htmlProcess = new HtmlProcess();

        public WebChatViewModel()
        {
            SearchPrev = new AsyncRelayCommand(async () => await TextSearch(VMLocator.MainViewModel.SearchKeyword, false));
            SearchNext = new AsyncRelayCommand(async () => await TextSearch(VMLocator.MainViewModel.SearchKeyword, true));

            ImportWebChatLogCommand = new AsyncRelayCommand(async () => await ImportWebChatLog());
            UpdateBrowserCommand = new RelayCommand(UpdateBrowser);

            WebChatViewIsVisible = true;
        }

        public async Task PostWebChat()
        {
            try
            {
                string escapedString = JsonSerializer.Serialize(VMLocator.EditorViewModel.GetRecentText());

                string script = @"const mainTag = document.querySelector('main');
                        const formTag = mainTag.querySelector('form');
                        const textarea = formTag.querySelector('textarea');"+
                        $"textarea.value = {escapedString};";
                await _browser.EvaluateJavaScript<string>(script);

                await Task.Delay(300);

                script = @"const mainTag = document.querySelector('main');
                        const formTag = mainTag.querySelector('form');
                        const textarea = formTag.querySelector('textarea');
                        var event = new Event('input', { bubbles: true });  // �C�x���g���쐬
                        textarea.dispatchEvent(event);  // �C�x���g���f�B�X�p�b�`";
                await _browser.EvaluateJavaScript<string>(script);

                await Task.Delay(300);

                script = @"const mainTag = document.querySelector('main');
                        const formTag = mainTag.querySelector('form');
                        const button = formTag.querySelector('button');
                        button.click();";
                await _browser.EvaluateJavaScript<string>(script);
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog() { Title = "Error : " + ex.Message, PrimaryButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
                throw;
            }
        }
        public async Task ImportWebChatLog()
        {
            var htmlSource = await _browser.EvaluateJavaScript<string>("return document.documentElement.outerHTML;");
            var msg = await _htmlProcess.GetWebChatLogAsync(htmlSource);
            if (msg == "Cancel")
            {
                return;
            }
            //var dialog = new ContentDialog() { Title = msg, PrimaryButtonText = "OK" };
            //await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
        }

        public async Task TextSearch(string searchKeyword, bool searchDirection, bool searchReset = false)
        {
            if (_browser == null || string.IsNullOrEmpty(searchKeyword))
            {
                return;
            }

            string searchTextFunction = @"if (!window.myCustomSearchFunction) {
                let lastKeyword = """";
                let currentSearchIndex = 0;
                let firstSearch = true;
                let timeoutID1 = null;
                let timeoutID2 = null;

              window.myCustomSearchFunction = function (keyword, searchForward, resetSearchIndex = false) {
                

                function findOverflowYAutoElement(element) {
                    // �q�v�f���擾
                    const children = element.children;

                    for (let i = 0; i < children.length; i++) {
                        const child = children[i];
                        const style = window.getComputedStyle(child);

                        // overflow-y��auto�̏ꍇ�A���̗v�f��Ԃ�
                        if (style.overflowY === 'auto') {
                            return child;
                        }

                        // �q�v�f������Ɏq�v�f�������Ă���ꍇ�A�ċA�I�Ɍ���
                        const result = findOverflowYAutoElement(child);
                        if (result) {
                            return result;
                        }
                    }

                    // ������Ȃ������ꍇ��null��Ԃ�
                    return null;
                }



                if (!document.getElementById('searchDisplay')) {
                  // Create the div element with the id 'searchDisplay'
                  const searchDisplay = document.createElement('div');
                  searchDisplay.id = 'searchDisplay';

                  // Add the element to the body as the first child
                  document.body.insertBefore(searchDisplay, document.body.firstChild);

                  // Add the style to the element
                  const style = document.createElement('style');
                  style.textContent = `
                          #searchDisplay {
	                        position: fixed;
	                        width: 172px;
	                        top: 7px;
	                        right: 57px;
	                        background: #3a3b47;
	                        border-radius: 6px;
	                        border-width: 1px;
	                        border: #545563 solid;
	                        padding: 0px 15px 0px 15px;
                            color: #fff;
	                        display: none;
	                        outline: none;
	                        transition: opacity 0.6s;
	                        font-size: 0.9em;
                            z-index: 10000;
                            box-shadow: inset 0 -3em 3em rgba(0,0,0,0.1), 0.3em 0.3em 1em rgba(0,0,0,0.3);
                          }
                        `;

                  // Add the style to the head of the document
                  document.head.appendChild(style);
                }


                searchText(keyword, searchForward);

                function searchText(keyword, searchForward, resetSearchIndex = false) {
                  // �����L�[���[�h���������ɕϊ�
                  keyword = keyword.toLowerCase();

                  // �O��̃L�[���[�h�Ɣ�r���A�قȂ�ꍇ�͌����C���f�b�N�X�����Z�b�g
                  if (lastKeyword !== keyword || resetSearchIndex) {
                    currentSearchIndex = 0;
                    lastKeyword = keyword;
                    firstSearch = true;
                  }

                  // �y�[�W����main�^�O�v�f���擾
                  const mainElement = document.querySelector('main');

                  // main�^�O�ȉ��̃e�L�X�g�v�f���擾
                  const textNodes = [];
                  const walk = document.createTreeWalker(mainElement, NodeFilter.SHOW_TEXT, null, false);
                  let node;
                  while (node = walk.nextNode()) {
                    textNodes.push(node);
                  }

                  // �������ʃ��X�g�𐶐�
                  const searchResults = [];
                  textNodes.forEach((textNode, index) => {
                    const content = textNode.textContent.toLowerCase();
                    let lastIndex = 0;

                    while (lastIndex !== -1) {
                      const keywordIndex = content.indexOf(keyword, lastIndex);

                      if (keywordIndex !== -1) {
                        searchResults.push({
                          index,
                          node: textNode,
                          keywordIndex
                        });
                        lastIndex = keywordIndex + keyword.length;
                      } else {
                        lastIndex = -1;
                      }
                    }
                  });

                  // �����q�b�g����0�̏ꍇ�A�uNo match found.�v��\��
                  if (searchResults.length === 0) {
                    const searchDisplay = document.getElementById('searchDisplay');
                    searchDisplay.textContent = 'No match found.';
                    searchDisplay.style.opacity = '0.9';
                    searchDisplay.style.display = 'block';

                    setTimeout(() => {
                      searchDisplay.style.opacity = '0';
                      setTimeout(() => {
                        searchDisplay.style.display = 'none';
                      }, 3000);
                    }, 3000);

                    return;
                  }

                  // �����C���f�b�N�X�̌v�Z
                  if (searchForward) {
                    if (!firstSearch) {
                      currentSearchIndex = (currentSearchIndex + 1) % searchResults.length;
                    }
                  } else {
                    currentSearchIndex = (currentSearchIndex - 1 + searchResults.length) % searchResults.length;
                  }
                  firstSearch = false;

                  // �e�L�X�g��I����Ԃɂ���
                  const selectedResult = searchResults[currentSearchIndex];
                  const range = document.createRange();
                  range.setStart(selectedResult.node, selectedResult.keywordIndex);
                  range.setEnd(selectedResult.node, selectedResult.keywordIndex + keyword.length);
                  const selection = window.getSelection();
                  selection.removeAllRanges();
                  selection.addRange(range);

                  // �I�������e�L�X�g�܂ŃX�N���[��
                  const rect = range.getBoundingClientRect();
                  const scrollParent = findOverflowYAutoElement(mainElement);
                  if (scrollParent) {
                    const parentRect = scrollParent.getBoundingClientRect();
                    scrollParent.scrollTop += rect.top - parentRect.top - scrollParent.clientHeight / 2;
                  } else {
                    window.scrollTo({
                      top: rect.top + window.pageYOffset - window.innerHeight / 2
                    });
                  }

                  const searchDisplay = document.getElementById(""searchDisplay"");

                  // �������q�b�g�����ꍇ
                  if (searchResults.length > 0) {
                    searchDisplay.textContent = `${currentSearchIndex + 1} / ${searchResults.length} results`;
                  }

                  // Opacity��Display��ݒ�
                  searchDisplay.style.display = ""block"";
                  searchDisplay.style.opacity = ""0.9"";

                  // �^�C���A�E�g���ݒ肳��Ă����ꍇ�A�N���A
                  if (timeoutID1) clearTimeout(timeoutID1);
                  if (timeoutID2) clearTimeout(timeoutID2);

                  // Opacity��0�ɖ߂��^�C���A�E�g��ݒ�
                  timeoutID1 = setTimeout(() => {
                    searchDisplay.style.opacity = ""0"";

                    // Display��none�ɖ߂��^�C���A�E�g��ݒ�
                    timeoutID2 = setTimeout(() => {
                      searchDisplay.style.display = ""none"";
                    }, 3000);
                  }, 3000);

                }
              };
            }";

            try
            {
                string script = $"{searchTextFunction} window.myCustomSearchFunction('{searchKeyword}', {searchDirection.ToString().ToLower()});";
                await _browser.EvaluateJavaScript<ValueTuple<int, int>>(script);
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog() { Title = "Error : " + ex.Message, PrimaryButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
            }
            return;
        }

        private void UpdateBrowser()
        {
            _browser?.Reload();
        }

        // Browser�C���X�^���X���󂯎��
        public void SetBrowser(AvaloniaCefBrowser browser)
        {
            _browser = browser;
        }

        public IAsyncRelayCommand SearchPrev { get; }
        public IAsyncRelayCommand SearchNext { get; }
        public IAsyncRelayCommand ImportWebChatLogCommand { get; }
        public ICommand UpdateBrowserCommand { get; }

        private bool _webChatViewIsVisible;
        public bool WebChatViewIsVisible//�_�C�A���O�\���p
        {
            get => _webChatViewIsVisible;
            set => SetProperty(ref _webChatViewIsVisible, value);
        }
    }
}