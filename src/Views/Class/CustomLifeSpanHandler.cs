using System.Diagnostics;
using Xilium.CefGlue;

namespace TmCGPTD
{
    public class CustomLifeSpanHandler : Xilium.CefGlue.Common.Handlers.LifeSpanHandler
    {
        protected override bool OnBeforePopup(CefBrowser browser, CefFrame frame, string targetUrl, string targetFrameName, CefWindowOpenDisposition targetDisposition, bool userGesture, CefPopupFeatures popupFeatures, CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings, ref CefDictionaryValue extraInfo, ref bool noJavascriptAccess)
        {
            if (targetUrl == "https://promptediton/")
            {
                VMLocator.ChatViewModel.PromptEditOn();
            }
            else if(targetUrl == "https://prompteditoff/")
            {
                VMLocator.ChatViewModel.PromptEditOff();
            }
            else if (targetUrl == "https://stopgenerating/")
            {
                VMLocator.MainViewModel.CancelPost();
            }
            else
            {
                // OSの既定のブラウザでリンクを開く
                Process.Start(new ProcessStartInfo(targetUrl) { UseShellExecute = true });
            }

            // 新しいウィンドウの作成をキャンセル
            return true;
        }
    }
}
