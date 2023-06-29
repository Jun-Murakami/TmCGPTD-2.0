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

namespace TmCGPTD.Models
{
    public class SupabaseProcess
    {
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

                SupabaseStates.Instance.Supabase = new Supabase.Client(supabaseUrl!, supabaseKey, options);
                await SupabaseStates.Instance.Supabase.InitializeAsync();
            }
            catch (Exception ex)
            {
                var cdialog = new ContentDialog() { Title = $"Error", Content = $"{ex.Message}", CloseButtonText = "OK" };
                await cdialog.ShowAsync();
            }
        }

        public async Task GetAuthAsync()
        {
            SupabaseStates.Instance.AuthState = await SupabaseStates.Instance.Supabase!.Auth.SignIn(Constants.Provider.Google, new SignInOptions
            {
                FlowType = Constants.OAuthFlowType.PKCE,
                RedirectTo = "http://localhost:3000/oauth/callback"
            });
        }

        public async Task GetSessionAsync()
        {
            var session = await SupabaseStates.Instance.Supabase!.Auth.ExchangeCodeForSession(SupabaseStates.Instance.AuthState!.PKCEVerifier!, VMLocator.MainViewModel.AuthCode!);
            VMLocator.MainViewModel.OnLogin = false;
            if(session != null)
            {
                VMLocator.MainViewModel.SyncLogText = "Signed in.";
            }
        }

        public async Task SignOutAsync()
        {
            await SupabaseStates.Instance.Supabase!.Auth.SignOut();
            VMLocator.MainViewModel.SyncLogText = "Signed out.";
        }
    }
}
