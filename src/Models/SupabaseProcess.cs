using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using Supabase.Gotrue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static TmCGPTD.Views.WebLogInView;
using TmCGPTD.Views;
using Supabase;
using TmCGPTD.Models;

namespace TmCGPTD
{
    public class SupabaseProcess
    {
        SyncProcess _sqliteProcess = new SyncProcess();

        public async Task InitializeSupabaseAsync()
        {
            try
            {
                using var streamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/supabaseConfig.json")));
                string jsonString = await streamReader.ReadToEndAsync();

                SupabaseConfig config = JsonSerializer.Deserialize<SupabaseConfig>(jsonString)!;

                var supabaseUrl = config!.Url;
                var supabaseKey = config.Key;

                var options = new SupabaseOptions
                {
                    SessionHandler = new CustomSessionHandler()
                };

                VMLocator.MainViewModel._supabase = new Supabase.Client(supabaseUrl!, supabaseKey, options);
                await VMLocator.MainViewModel._supabase.InitializeAsync();
            }
            catch (Exception ex)
            {
                var cdialog = new ContentDialog() { Title = $"Error", Content = $"{ex.Message}", CloseButtonText = "OK" };
                await cdialog.ShowAsync();
            }
        }

        public async Task GetAuthAsync()
        {
            VMLocator.MainViewModel._authState = await VMLocator.MainViewModel._supabase!.Auth.SignIn(Constants.Provider.Google, new SignInOptions
            {
                FlowType = Constants.OAuthFlowType.PKCE,
                RedirectTo = "http://localhost:3000/oauth/callback"
            });
        }

        public async Task GetSessionAsync()
        {
            VMLocator.MainViewModel._session = await VMLocator.MainViewModel._supabase!.Auth.ExchangeCodeForSession(VMLocator.MainViewModel._authState!.PKCEVerifier!, VMLocator.MainViewModel.AuthCode);
            VMLocator.MainViewModel.OnLogin = false;
            Debug.WriteLine($"Session: {VMLocator.MainViewModel._supabase.Auth.CurrentSession!.User!.Email}");
        }

        public async Task SignOutAsync()
        {
            await VMLocator.MainViewModel._supabase!.Auth.SignOut();
        }
    }
}
