using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Events;
using FluentAvalonia.UI.Controls;
using Avalonia;
using System;
using System.Web;
using System.Collections.Specialized;

namespace TmCGPTD.Views
{
  public partial class WebLogInView : UserControl
  {
    private readonly AvaloniaCefBrowser browser;
    private readonly Decorator browserWrapper;

    public WebLogInView()
    {
      InitializeComponent();

      browserWrapper = this.FindControl<Decorator>("WebLogInBrowserWrapper")!;

      browser = new AvaloniaCefBrowser
      {
        LifeSpanHandler = new CustomLifeSpanHandler(),
      };
      browser.AttachedToVisualTree += Browser_AttachedToVisualTree;
      browser.DetachedFromVisualTree += Browser_DetachedFromVisualTree;
      browser.LoadEnd += Browser_LoadEnd;
      browser.ContextMenuHandler = new CustomContextMenuHandler();
      browserWrapper!.Child = browser;

      browser.AddressChanged += Browser_AddressChanged;
    }

    public class SupabaseConfig
    {
      public string? Url { get; set; }
      public string? Key { get; set; }
    }

    private void Browser_LoadEnd(object? sender, LoadEndEventArgs e)
    {
      string jsCode = @"document.body.style.backgroundColor = '#343541';
                                 document.documentElement.style.backgroundColor = '#343541';";
      browser.ExecuteJavaScript(jsCode);
    }

    private async void Browser_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
      try
      {
        browser.Address = VMLocator.MainViewModel.LoginUri!.ToString();
        //browserWrapper.IsVisible = true;
      }
      catch (Exception ex)
      {
        var cdialog = new ContentDialog
        {
          Title = "Error",
          Content = ex.Message,
          CloseButtonText = "OK"
        };
        await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
      }
    }

    private async void Browser_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
      try
      {
        //if (browser != null) browser.Dispose();
        //browserWrapper.IsVisible = false;
      }
      catch (Exception ex)
      {
        var cdialog = new ContentDialog
        {
          Title = "Error",
          Content = ex.Message,
          CloseButtonText = "OK"
        };
        await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
      }
    }

    private async void Browser_AddressChanged(object? sender, string address)
    {
      try
      {
        Uri uri = new Uri(address);
        string query = uri.Query;
        NameValueCollection queryParameters = HttpUtility.ParseQueryString(query);
        string? code = queryParameters["code"];

        if (code != null)
        {
          VMLocator.MainViewModel.AuthCode = code;
        }
      }
      catch (Exception ex)
      {
        var cdialog = new ContentDialog
        {
          Title = "Error",
          Content = ex.Message,
          CloseButtonText = "OK"
        };
        await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
      }
    }

    public void Dispose()
    {
      browser.Dispose();
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }
  }
}