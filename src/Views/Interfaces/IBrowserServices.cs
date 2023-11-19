using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Events;

namespace TmCGPTD
{
    public interface IBrowserService
    {
        void InitializeBrowser(AvaloniaCefBrowser browser);
        Task ExecuteJavaScriptAsync(string script);
        void AddLoadEndEventHandler(LoadEndEventHandler handler);
        // その他の必要なメソッドをここに追加
    }

    public class BrowserService : IBrowserService
    {
        private AvaloniaCefBrowser? _browser;

        public void InitializeBrowser(AvaloniaCefBrowser browser)
        {
            _browser = browser;
            // 必要な初期化処理をここで行う
        }

        public async Task ExecuteJavaScriptAsync(string script)
        {
            if (_browser != null)
            {
                await _browser.EvaluateJavaScript<string>(script);
            }
        }

        public void AddLoadEndEventHandler(LoadEndEventHandler handler)
        {
            if (_browser != null)
            {
                _browser.LoadEnd += handler;
            }
        }

        public void ReloadBrowser()
        {
            if (_browser != null)
            {
                _browser.Reload();
            }
        }

        // その他のメソッドの実装をここに追加
    }
}
