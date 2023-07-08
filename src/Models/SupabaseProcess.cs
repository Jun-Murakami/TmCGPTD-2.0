using Avalonia.Platform;
using FluentAvalonia.UI.Controls;
using Supabase.Gotrue;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using static TmCGPTD.Views.WebLogInView;
using Supabase;
using System.Collections.Generic;

namespace TmCGPTD.Models
{
    public class SupabaseProcess
    {
        SyncProcess _syncProcess = new();
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

        public async Task DeleteAccountAsync()
        {
            var dict = new Dictionary<string, object>();
            await SupabaseStates.Instance.Supabase!.Rpc("deleteUser", dict);
        }

        public async Task ChangeEmailAsync(string email)
        {
            var attrs = new UserAttributes { Email = email };
            await SupabaseStates.Instance.Supabase!.Auth.Update(attrs);
        }

        public async Task ChangePasswordAsync(string password)
        {
            var attrs = new UserAttributes { Password = password };
            await SupabaseStates.Instance.Supabase!.Auth.Update(attrs);
        }

        public async Task<bool> PasswordResetAsync(string email)
        {
            return await SupabaseStates.Instance.Supabase!.Auth.ResetPasswordForEmail(email);
        }

        public async Task<Session> EmailLoginAsync(string email, string password)
        {
            Session? session = await SupabaseStates.Instance.Supabase!.Auth.SignIn(email, password);
            return session!;
        }
        public async Task<Session> EmailSignUpAsync(string email, string password)
        {
            Session? session = await SupabaseStates.Instance.Supabase!.Auth.SignUp(email, password);
            return session!;
        }

        public async Task GoogleAuthAsync()
        {
            SupabaseStates.Instance.AuthState = await SupabaseStates.Instance.Supabase!.Auth.SignIn(Constants.Provider.Google, new SignInOptions
            {
                FlowType = Constants.OAuthFlowType.PKCE,
                RedirectTo = "http://localhost:3000/oauth/callback"
            });
        }

        public async Task MicrosoftAuthAsync()
        {
            SupabaseStates.Instance.AuthState = await SupabaseStates.Instance.Supabase!.Auth.SignIn(Constants.Provider.Azure, new SignInOptions
            {
                FlowType = Constants.OAuthFlowType.PKCE,
                Scopes = "openid profile email offline_access",
                RedirectTo = "http://localhost:3000/oauth/callback"
            });
        }

        public async Task GetSessionAsync()
        {
            await SupabaseStates.Instance.Supabase!.Auth.ExchangeCodeForSession(SupabaseStates.Instance.AuthState!.PKCEVerifier!, VMLocator.MainViewModel.AuthCode!);
        }

        public async Task RetriveLogInAsync()
        {
            switch (AppSettings.Instance.Provider)
            {
                case "You are logged in with Google.":
                    await GoogleAuthAsync();
                    break;
                case "You are logged in with Microsoft.":
                    await MicrosoftAuthAsync();
                    break;
                case "You are logged in with Email.":
                    await EmailLoginAsync(AppSettings.Instance.Email!, AppSettings.Instance.Password!);
                    break;
            }
        }

        public async Task LogOutAsync()
        {
            await SupabaseStates.Instance.Supabase!.Auth.SignOut();
        }

        public async Task SubscribeAsync()
        {
            var channel = SupabaseStates.Instance.Supabase!.Realtime.Channel("realtime", "public", "*");

            channel.AddPostgresChangeHandler(Supabase.Realtime.PostgresChanges.PostgresChangesOptions.ListenType.All, (sender, change) =>
            {
                // The event type
                Debug.WriteLine("change.Event:" + change.Event);
                // The changed record
                Debug.WriteLine("change.Payload:" + change.Payload);
                // The table name?
                Debug.WriteLine("sender: " + sender);
                _syncProcess.SyncDbAsync();
            });

            await channel.Subscribe();
        }
    }
}
