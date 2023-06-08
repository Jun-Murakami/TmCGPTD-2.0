using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xilium.CefGlue.Avalonia;
using FluentAvalonia.UI.Controls;
using TmCGPTD.Models;

namespace TmCGPTD.ViewModels
{
    public class WebChatBardViewModel : ViewModelBase
    {
        private AvaloniaCefBrowser _browser;
        HtmlProcess _htmlProcess = new HtmlProcess();

        public WebChatBardViewModel()
        {
            SearchPrev = new AsyncRelayCommand(async () => await TextSearch(VMLocator.MainViewModel.SearchKeyword, false));
            SearchNext = new AsyncRelayCommand(async () => await TextSearch(VMLocator.MainViewModel.SearchKeyword, true));

            ImportWebChatLogCommand = new AsyncRelayCommand(async () => await ImportWebChatLog());

            WebChatBardViewIsVisible = true;
        }

        public async Task PostWebChat()
        {
            try
            {
                string escapedString = JsonSerializer.Serialize(VMLocator.EditorViewModel.GetRecentText());

                string script = @"const mainTag = document.querySelector('main');
                        const textarea = mainTag.querySelector('textarea');" +
                        $"textarea.value = {escapedString};";
                await _browser.EvaluateJavaScript<string>(script);

                await Task.Delay(300);

                script = @"const mainTag = document.querySelector('main');
                        const textarea = mainTag.querySelector('textarea');
                        var event = new Event('input', { bubbles: true });  // �C�x���g���쐬
                        textarea.dispatchEvent(event);  // �C�x���g���f�B�X�p�b�`";
                await _browser.EvaluateJavaScript<string>(script);

                await Task.Delay(300);

                script = @"const mainTag = document.querySelector('main');
                        const sendDiv = mainTag.querySelector('div.send-button-container');
                        const button = sendDiv.querySelector('button');
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
            var msg = await _htmlProcess.GetWebChatLogBardAsync(htmlSource);
            if (msg == "Cancel")
            {
                return;
            }
            var dialog = new ContentDialog() { Title = msg, PrimaryButtonText = "OK" };
            await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
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
                

                function getScrollParent(element) {
                  while (element && element !== document.body) {
                    // Element�^�łȂ���΁A�e�v�f�Ɉړ�
                    if (!(element instanceof Element) || element.tagName.toLowerCase() === 'pre') {
                      element = element.parentElement;
                      continue;
                    }

                    const style = window.getComputedStyle(element);
                    const overflowRegex = /(auto|scroll)/;
                    const overflow = style.getPropertyValue('overflow') + style.getPropertyValue('overflow-y') + style.getPropertyValue('overflow-x');

                    if (overflowRegex.test(overflow)) {
                      return element;
                    }

                    element = element.parentElement;
                  }

                  return document.scrollingElement || document.documentElement;
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
                            color: #dcdcdc;
	                        border-radius: 6px;
	                        border-width: 1px;
	                        border: #545563 solid;
	                        padding: 0px 15px 0px 15px;
	                        display: none;
	                        outline: none;
	                        transition: opacity 0.6s;
	                        font-size: 0.9em;
                            z-index: 10000;
                          }
                        `;

                  // Add the style to the head of the document
                  document.head.appendChild(style);
                }


                searchText(keyword, searchForward);

                function searchText(keyword, searchForward, resetSearchIndex = false) {
                    debugger;
                  // �����L�[���[�h���������ɕϊ�
                  keyword = keyword.toLowerCase();

                  // �O��̃L�[���[�h�Ɣ�r���A�قȂ�ꍇ�͌����C���f�b�N�X�����Z�b�g
                  if (lastKeyword !== keyword || resetSearchIndex) {
                    currentSearchIndex = 0;
                    lastKeyword = keyword;
                    firstSearch = true;
                  }

                  // �y�[�W���̃e�L�X�g�v�f���擾
                  const textNodes = [];
                  const walk = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, null, false);
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
                  const scrollParent = getScrollParent(selectedResult.node);
                  if (scrollParent) {
                    const parentRect = scrollParent.getBoundingClientRect();
                    scrollParent.scrollTop += rect.top - parentRect.top - scrollParent.clientHeight / 2;
                  } else {
                    window.scrollTo({
                      top: rect.top + window.pageYOffset - window.innerHeight / 2
                    });
                  }

                  console.log({ scrollParent });

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

        // Browser�C���X�^���X���󂯎��
        public void SetBrowser(AvaloniaCefBrowser browser)
        {
            _browser = browser;
        }

        public IAsyncRelayCommand SearchPrev { get; }
        public IAsyncRelayCommand SearchNext { get; }
        public IAsyncRelayCommand ImportWebChatLogCommand { get; }

        private bool _webChatBardViewIsVisible;
        public bool WebChatBardViewIsVisible//�_�C�A���O�\���p
        {
            get => _webChatBardViewIsVisible;
            set => SetProperty(ref _webChatBardViewIsVisible, value);
        }
    }
}