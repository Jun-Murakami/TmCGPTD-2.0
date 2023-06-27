using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Xilium.CefGlue.Avalonia;
using TmCGPTD.ViewModels;
using Avalonia.Interactivity;
using Xilium.CefGlue.Common.Events;
using Avalonia.Platform;
using System.IO;
using System.Text.Json;
using FluentAvalonia.UI.Controls;
using System.ComponentModel;
using Avalonia;
using System.Net.Http;
using System;
using TmCGPTD.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Web;
using Supabase.Gotrue;
using System.Collections.Generic;
using EmbedIO;
using System.Collections.Specialized;
using Avalonia.Threading;
using EmbedIO.WebApi;
using Swan.Logging;
using Swan;

namespace TmCGPTD.Views
{
    public partial class WebLogInView : UserControl
    {
        private WebServer _server;
        private AvaloniaCefBrowser browser;

        public WebLogInView()
        {
            InitializeComponent();

            var browserWrapper = this.FindControl<Decorator>("WebLogInBrowserWrapper");

            browser = new AvaloniaCefBrowser
            {
                LifeSpanHandler = new CustomLifeSpanHandler(),
            };
            browser.AttachedToVisualTree += Browser_AttachedToVisualTree;
            browser.DetachedFromVisualTree += Browser_DetachedFromVisualTree;
            browser.ContextMenuHandler = new CustomContextMenuHandler();
            browserWrapper.Child = browser;


            browser.AddressChanged += Browser_AddressChanged;
        }

        public class SupabaseConfig
        {
            public string? Url { get; set; }
            public string? Key { get; set; }
        }

        HtmlProcess _htmlProcess = new HtmlProcess();
        private async void Browser_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            //var htmlContent = await _htmlProcess.InitializeLogInToHtml();
            //var encodedHtml = Uri.EscapeDataString(htmlContent);
            //var dataUrl = $"data:text/html;charset=utf-8,{encodedHtml}";
            //browser.Address = dataUrl;

            try
            {
                var url = "https://localhost:9999/";

                using (_server = CreateWebServer(url))
                {
                    
                    //await _server.RunAsync();

                }

                browser.Address = VMLocator.MainViewModel.LoginUri.ToString();
            }
            catch (Exception ex)
            {
                var cdialog = new ContentDialog
                {
                    Title = "Error",
                    Content = ex.Message,
                    CloseButtonText = "OK"
                };
                //await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                //throw;
            }
        }
        private static WebServer CreateWebServer(string url)
        {
                var server = new WebServer(o => o
                        .WithUrlPrefix(url)
                        .WithMode(HttpListenerMode.EmbedIO));

                return server;
        }


        private void Browser_DetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            try
            {
                if (_server != null) _server.Dispose();
                if (browser != null) browser.Dispose();
            }
            catch (Exception ex)
            {
                var cdialog = new ContentDialog
                {
                    Title = "Error",
                    Content = ex.Message,
                    CloseButtonText = "OK"
                };
                //await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                throw;
            }
        }

        private void Browser_AddressChanged(object sender, string address)
        {
            try
            {
                Uri uri = new Uri(address);
                string query = uri.Query;
                NameValueCollection queryParameters = HttpUtility.ParseQueryString(query);
                string code = queryParameters["code"];

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
                //await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                throw;
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

    internal class ClientOptions<T> : ClientOptions
    {
        public string? Url { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}