using Avalonia.Platform;
using Avalonia;
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
using Avalonia.Threading;
using System.Threading;
using Supabase.Realtime.PostgresChanges;

namespace TmCGPTD.Models
{
    public class SupabaseProcess
    {
        SyncProcess _syncProcess = new();
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Debouncer _debouncer = new Debouncer(TimeSpan.FromMinutes(0.3));

        private static SupabaseProcess? _instance;
        public static SupabaseProcess Instance
        {
            get
            {
                _instance ??= new SupabaseProcess();

                return _instance;
            }
        }

        public async Task InitializeSupabaseAsync()
        {
            try
            {
#if WINDOWS
                using var streamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/supabaseConfig.json")));
#else
                using var streamReader = new StreamReader(AvaloniaLocator.Current.GetService<IAssetLoader>()!.Open(new Uri("avares://TmCGPTD/supabaseConfig.json")));
#endif
                string jsonString = await streamReader.ReadToEndAsync();

                SupabaseConfig config = JsonSerializer.Deserialize<SupabaseConfig>(jsonString)!;

                var supabaseUrl = config!.Url;
                var supabaseKey = config.Key;

                var options = new SupabaseOptions
                {
                    SessionHandler = new CustomSessionHandler(),
                    //AutoConnectRealtime = true,
                    AutoRefreshToken = true,
                };

                SupabaseStates.Instance.Supabase = new Supabase.Client(supabaseUrl!, supabaseKey, options);
                await SupabaseStates.Instance.Supabase.InitializeAsync();
            }
            catch (Exception ex)
            {
                var cdialog = new ContentDialog() { Title = $"Error", Content = $"{ex.Message}", CloseButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
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
            SupabaseStates.Instance.AuthState = await SupabaseStates.Instance.Supabase!.Auth.SignIn(Supabase.Gotrue.Constants.Provider.Google, new SignInOptions
            {
                FlowType = Supabase.Gotrue.Constants.OAuthFlowType.PKCE,
                RedirectTo = "http://localhost:3000/oauth/callback"
            });
        }

        public async Task MicrosoftAuthAsync()
        {
            SupabaseStates.Instance.AuthState = await SupabaseStates.Instance.Supabase!.Auth.SignIn(Supabase.Gotrue.Constants.Provider.Azure, new SignInOptions
            {
                FlowType = Supabase.Gotrue.Constants.OAuthFlowType.PKCE,
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

        public async Task SubscribeSyncAsync()
        {
            try
            {
                await SupabaseStates.Instance.Supabase!.Realtime.ConnectAsync();
                var channel = SupabaseStates.Instance.Supabase!.Realtime.Channel("realtime", "public", "*");

                channel.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.All, async(_, change) =>
                {
                    //_debouncer.Debounce(async () =>
                    //{
                        if (VMLocator.ChatViewModel.ChatIsRunning)
                        {
                            while (VMLocator.ChatViewModel.ChatIsRunning)
                            {
                                await Task.Delay(1000);
                            }
                        }

                        try
                        {
                            Debug.WriteLine("change.Event:" + change.Event);
                            Debug.WriteLine("change.Payload:" + change.Payload);

                            // セマフォスリムを使用して、一度に一つのタスクだけがSyncDbAsync()メソッドを実行
                            //await _semaphore.WaitAsync();
                            //try
                            //{
                                await _syncProcess.SyncDbAsync();
                            //}
                            //finally
                            //{
                                _semaphore.Release();
                            //}
                        }
                        catch (Exception ex)
                        {
                            ContentDialog? cdialog = null;
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                cdialog = new ContentDialog() { Title = $"Error", Content = $"{ex.Message}", CloseButtonText = "OK" };
                            });
                            await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog!);
                        }
                    //});
                });

                await channel.Subscribe();
            }
            catch (Exception ex)
            {
                ContentDialog? cdialog = null;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    cdialog = new ContentDialog() { Title = $"Error", Content = $"{ex.Message}", CloseButtonText = "OK" };
                });
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog!);
            }
        }

        public async Task DelaySyncDbAsync()
        {
            if (SupabaseStates.Instance.Supabase == null || SupabaseStates.Instance.Supabase.Auth.CurrentUser == null || !AppSettings.Instance.SyncIsOn) return;

            try
            {
                // デバウンサーで5分間の間に複数の呼び出しを1つにまとめる
                _debouncer.Debounce(async () =>
                {
                    if(VMLocator.ChatViewModel.ChatIsRunning)
                    {
                        while(VMLocator.ChatViewModel.ChatIsRunning)
                        {
                            await Task.Delay(1000);
                        }
                    }

                    // セマフォスリムを使用して、一度に一つのタスクだけがSyncDbAsync()メソッドを実行
                    await _semaphore.WaitAsync();
                    try
                    {
                        await _syncProcess.SyncDbAsync();
                    }
                    finally
                    {
                        _semaphore.Release();
                    }

                });
            }
            catch (Exception ex)
            {
                ContentDialog? cdialog = null;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    cdialog = new ContentDialog() { Title = $"Error", Content = $"{ex.Message}", CloseButtonText = "OK" };
                });
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog!);
            }   
        }

        public async Task SingleSyncDbAsync()
        {
            if (SupabaseStates.Instance.Supabase == null || SupabaseStates.Instance.Supabase.Auth.CurrentUser == null || !AppSettings.Instance.SyncIsOn) return;

            try
            {
                if (VMLocator.ChatViewModel.ChatIsRunning)
                {
                    while (VMLocator.ChatViewModel.ChatIsRunning)
                    {
                        await Task.Delay(1000);
                    }
                }

                // セマフォスリムを使用して、一度に一つのタスクだけがSyncDbAsync()メソッドを実行
                await _semaphore.WaitAsync();
                try
                {
                    await _syncProcess.SyncDbAsync();
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                ContentDialog? cdialog = null;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    cdialog = new ContentDialog() { Title = $"Error", Content = $"{ex.Message}", CloseButtonText = "OK" };
                });
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog!);
            }
        }
    }
}
