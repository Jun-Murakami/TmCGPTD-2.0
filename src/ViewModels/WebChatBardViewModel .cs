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
                        var event = new Event('input', { bubbles: true });  // イベントを作成
                        textarea.dispatchEvent(event);  // イベントをディスパッチ";
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
                    // Element型でなければ、親要素に移動
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
                  // 検索キーワードを小文字に変換
                  keyword = keyword.toLowerCase();

                  // 前回のキーワードと比較し、異なる場合は検索インデックスをリセット
                  if (lastKeyword !== keyword || resetSearchIndex) {
                    currentSearchIndex = 0;
                    lastKeyword = keyword;
                    firstSearch = true;
                  }

                  // ページ内のテキスト要素を取得
                  const textNodes = [];
                  const walk = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, null, false);
                  let node;
                  while (node = walk.nextNode()) {
                    textNodes.push(node);
                  }

                  // 検索結果リストを生成
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

                  // 検索ヒット数が0の場合、「No match found.」を表示
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

                  // 検索インデックスの計算
                  if (searchForward) {
                    if (!firstSearch) {
                      currentSearchIndex = (currentSearchIndex + 1) % searchResults.length;
                    }
                  } else {
                    currentSearchIndex = (currentSearchIndex - 1 + searchResults.length) % searchResults.length;
                  }
                  firstSearch = false;

                  // テキストを選択状態にする
                  const selectedResult = searchResults[currentSearchIndex];
                  const range = document.createRange();
                  range.setStart(selectedResult.node, selectedResult.keywordIndex);
                  range.setEnd(selectedResult.node, selectedResult.keywordIndex + keyword.length);
                  const selection = window.getSelection();
                  selection.removeAllRanges();
                  selection.addRange(range);

                  // 選択したテキストまでスクロール
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

                  // 検索がヒットした場合
                  if (searchResults.length > 0) {
                    searchDisplay.textContent = `${currentSearchIndex + 1} / ${searchResults.length} results`;
                  }

                  // OpacityとDisplayを設定
                  searchDisplay.style.display = ""block"";
                  searchDisplay.style.opacity = ""0.9"";

                  // タイムアウトが設定されていた場合、クリア
                  if (timeoutID1) clearTimeout(timeoutID1);
                  if (timeoutID2) clearTimeout(timeoutID2);

                  // Opacityを0に戻すタイムアウトを設定
                  timeoutID1 = setTimeout(() => {
                    searchDisplay.style.opacity = ""0"";

                    // Displayをnoneに戻すタイムアウトを設定
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

        // Browserインスタンスを受け取る
        public void SetBrowser(AvaloniaCefBrowser browser)
        {
            _browser = browser;
        }

        public IAsyncRelayCommand SearchPrev { get; }
        public IAsyncRelayCommand SearchNext { get; }
        public IAsyncRelayCommand ImportWebChatLogCommand { get; }

        private bool _webChatBardViewIsVisible;
        public bool WebChatBardViewIsVisible//ダイアログ表示用
        {
            get => _webChatBardViewIsVisible;
            set => SetProperty(ref _webChatBardViewIsVisible, value);
        }
    }
}