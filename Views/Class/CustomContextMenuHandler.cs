using Xilium.CefGlue;
using Xilium.CefGlue.Common.Handlers;

public class CustomContextMenuHandler : ContextMenuHandler
{
    // ここに必要なメソッドのオーバーライドを記述します。
    // 例：OnBeforeContextMenu メソッドをオーバーライドして、コンテキストメニューを無効化することができます。
    protected override void OnBeforeContextMenu(CefBrowser browser, CefFrame frame, CefContextMenuParams state, CefMenuModel model)
    {
        model.Clear();
    }
}
