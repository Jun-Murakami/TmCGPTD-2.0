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
using static Supabase.Gotrue.Constants;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Web;
using Supabase.Gotrue;
using System.Collections.Generic;
using EmbedIO.Actions;
using EmbedIO;
using System.Collections.Specialized;
using Avalonia.Threading;

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

            //browser.LoadEnd += Browser_LoadEnd;
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
                    _server.RunAsync();

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
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
            }
        }

        private async void Browser_DetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    //_server.Dispose();
                    //browser.Dispose();
                });
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

        private static WebServer CreateWebServer(string url)
        {
            var server = new WebServer(o => o
                    .WithUrlPrefix(url)
                    .WithMode(HttpListenerMode.EmbedIO))
                    .WithLocalSessionManager()
                    .WithStaticFolder("/", "wwwroot", true);

            return server;
        }

        private void Browser_AddressChanged(object sender, string address)
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

        private async void Browser_LoadEnd(object sender, LoadEndEventArgs e)
        {
            if (!browser.Title.Contains("#access_token="))
            {
                await Task.Delay(1000);
                using var streamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/supabaseConfig.json")));
                string jsonString = await streamReader.ReadToEndAsync();

                SupabaseConfig config = JsonSerializer.Deserialize<SupabaseConfig>(jsonString)!;

                var supabaseUrl = config!.Url;
                var supabaseKey = config.Key;

                string jsCode = $@"var supabaseUrl = '{supabaseUrl}';
                               var supabaseKey = '{supabaseKey}';
                               const {{createClient}} = supabase;
                               const supa = createClient(supabaseUrl, supabaseKey);
                               supa.auth.signInWithOAuth({{provider: 'google'}}).then(({{data, error}}) => {{
                               console.log(data);
                                 console.log(error);
                               }})";


                browser.ExecuteJavaScript(jsCode);


            }
            else
            {
                var fragment = new Uri(browser.Title).Fragment.TrimStart('#');
                fragment = fragment.Replace("&amp;", "&");

                var parameters = HttpUtility.ParseQueryString(fragment);

                var accessToken = parameters["access_token"];
                var expiresIn = parameters["expires_in"];
                var providerToken = parameters["provider_token"];
                var refreshToken = parameters["refresh_token"];
                var tokenType = parameters["token_type"];

                // ここで取得した認証情報を使用してSupabaseクライアントを認証します。
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