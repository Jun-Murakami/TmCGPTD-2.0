using Xilium.CefGlue;

namespace TmCGPTD
{
    class CustomSchemeHandler : CefSchemeHandlerFactory
    {
        protected override CefResourceHandler Create(CefBrowser browser, CefFrame frame, string schemeName, CefRequest request)
        {
            throw new System.NotImplementedException();
        }
    }
}