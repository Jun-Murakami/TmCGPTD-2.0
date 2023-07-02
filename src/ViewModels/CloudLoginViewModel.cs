using CommunityToolkit.Mvvm.Input;
using System;
using TmCGPTD.Models;
using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;

namespace TmCGPTD.ViewModels
{
    public class CloudLoginViewModel : ViewModelBase
    {
        readonly SupabaseProcess _supabaseProcess = new();
        readonly DatabaseProcess _databaseProcess = new();
        readonly SyncProcess _syncProcess = new();
        public CloudLoginViewModel()
        {
            _email = AppSettings.Instance.Email;
            _password = AppSettings.Instance.Password;
            EmailLoginCommand = new AsyncRelayCommand(EmailLoginAsync);
            EmailSignupCommand = new AsyncRelayCommand(EmailSignupAsync);
            GoogleLoginCommand = new AsyncRelayCommand(GoogleLoginAsync);
            MicrosoftLoginCommand = new AsyncRelayCommand(MicrosoftLoginAsync);
            PasswordResetCommand = new AsyncRelayCommand(PasswordResetAsync);
        }

        public IAsyncRelayCommand EmailLoginCommand { get; }
        public IAsyncRelayCommand EmailSignupCommand { get; }
        public IAsyncRelayCommand GoogleLoginCommand { get; }
        public IAsyncRelayCommand MicrosoftLoginCommand { get; }
        public IAsyncRelayCommand PasswordResetCommand { get; }

        private string? _email;
        public string? Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string? _password;
        public string? Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private async Task EmailLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(Password))
            {
                var cdialog = new ContentDialog
                {
                    Title = "Please enter your email and password.",
                    CloseButtonText = "OK"
                };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                return;
            }

            try
            {
                await _supabaseProcess.EmailLoginAsync(Email!, Password!);

                for (int timeOut = 0; SupabaseStates.Instance.Supabase!.Auth.CurrentSession == null && timeOut < 30; timeOut++)
                {
                    await Task.Delay(1000);
                }

                if (SupabaseStates.Instance.Supabase.Auth.CurrentSession != null)
                {
                    VMLocator.MainViewModel.LoginStatus = 3;
                    AppSettings.Instance.SyncIsOn = true;
                    VMLocator.CloudLoggedinViewModel.Provider = "You are logged in with Email.";
                    AppSettings.Instance.Provider = "You are logged in with Email.";
                    AppSettings.Instance.Email = SupabaseStates.Instance.Supabase.Auth.CurrentSession.User!.Email;
                    VMLocator.CloudLoggedinViewModel.Email = SupabaseStates.Instance.Supabase.Auth.CurrentSession.User!.Email;
                    await SupabaseStates.Instance.Supabase.Auth.RefreshSession();
                    await _databaseProcess.CleanUpEditorLogDatabaseAsync();
                    await _syncProcess.SyncDbAsync();
                }
            }
            catch (Exception ex)
            {
                var cdialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"{ex.Message}",
                    CloseButtonText = "OK"
                };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
            }
        }

        private async Task EmailSignupAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(Password))
            {
                var cdialog = new ContentDialog
                {
                    Title = "Please enter your email and password.",
                    CloseButtonText = "OK"
                };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                return;
            }

            try
            {
                if (SupabaseStates.Instance.Supabase == null)
                {
                    await _supabaseProcess.InitializeSupabaseAsync();
                }

                if (SupabaseStates.Instance.Supabase!.Auth.CurrentSession == null)
                {
                    var session = await _supabaseProcess.EmailSignUpAsync(Email!, Password!);

                    var cdialog = new ContentDialog
                    {
                        Title = "Information",
                        Content = $"If you are a new user, A confirmation email has been sent to you.{Environment.NewLine}Please check your mailbox. Once your address is verified, you can log in.",
                        CloseButtonText = "OK"
                    };
                    await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                }
            }
            catch (Exception ex)
            {
                var cdialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"{ex.Message}",
                    CloseButtonText = "OK"
                };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
            }
        }

        private async Task PasswordResetAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                var cdialog = new ContentDialog
                {
                    Title = "Please enter your email.",
                    CloseButtonText = "OK"
                };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                return;
            }

            try
            {
                if (SupabaseStates.Instance.Supabase!.Auth.CurrentSession == null)
                {
                    bool result = await _supabaseProcess.PasswordResetAsync(Email!);

                    if (result)
                    {
                        var cdialog = new ContentDialog
                        {
                            Title = "Information",
                            Content = $"A password reset email has been sent to you. Please check your mailbox.{Environment.NewLine}After logging in with your browser, please set a new password from the menu.",
                            CloseButtonText = "OK"
                        };
                        await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                    }
                    else
                    {
                        var cdialog = new ContentDialog
                        {
                            Title = "Password reset failed. Please check your email address and try again.",
                            CloseButtonText = "OK"
                        };
                        await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                    }
                }
            }
            catch (Exception ex)
            {
                var cdialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"{ex.Message}",
                    CloseButtonText = "OK"
                };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
            }
        }

        private async Task GoogleLoginAsync()
        {
            try
            {
                if(SupabaseStates.Instance.Supabase == null)
                {
                    await _supabaseProcess.InitializeSupabaseAsync();
                }

                if (SupabaseStates.Instance.Supabase!.Auth.CurrentSession == null)
                {
                    await _supabaseProcess.GoogleAuthAsync();
                    VMLocator.MainViewModel.LoginUri = SupabaseStates.Instance.AuthState!.Uri;
                    VMLocator.MainViewModel.LoginStatus = 2;

                    for (int timeOut = 0; SupabaseStates.Instance.Supabase.Auth.CurrentSession == null && timeOut < 600; timeOut++)
                    {
                        await Task.Delay(1000);
                    }

                    if (SupabaseStates.Instance.Supabase.Auth.CurrentSession != null)
                    {
                        VMLocator.MainViewModel.LoginStatus = 3;
                        AppSettings.Instance.SyncIsOn = true;
                        VMLocator.CloudLoggedinViewModel.Provider = "You are logged in with Google.";
                        AppSettings.Instance.Email = SupabaseStates.Instance.Supabase.Auth.CurrentSession.User!.Email;
                        VMLocator.CloudLoggedinViewModel.Email = SupabaseStates.Instance.Supabase.Auth.CurrentSession.User!.Email;
                        AppSettings.Instance.Provider = "You are logged in with Google.";
                        await SupabaseStates.Instance.Supabase.Auth.RefreshSession();
                        await _databaseProcess.CleanUpEditorLogDatabaseAsync();
                        await _syncProcess.SyncDbAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var cdialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"{ex.Message}",
                    CloseButtonText = "OK"
                };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
            }
        }

        private async Task MicrosoftLoginAsync()
        {
            try
            {
                if (SupabaseStates.Instance.Supabase == null)
                {
                    await _supabaseProcess.InitializeSupabaseAsync();
                }

                if (SupabaseStates.Instance.Supabase!.Auth.CurrentSession == null)
                {
                    await _supabaseProcess.MicrosoftAuthAsync();
                    VMLocator.MainViewModel.LoginUri = SupabaseStates.Instance.AuthState!.Uri;
                    VMLocator.MainViewModel.LoginStatus = 2;

                    for (int timeOut = 0; SupabaseStates.Instance.Supabase.Auth.CurrentSession == null && timeOut < 600; timeOut++)
                    {
                        await Task.Delay(1000);
                    }

                    if (SupabaseStates.Instance.Supabase.Auth.CurrentSession != null)
                    {
                        VMLocator.MainViewModel.LoginStatus = 3;
                        AppSettings.Instance.SyncIsOn = true;
                        VMLocator.CloudLoggedinViewModel.Provider = "You are logged in with Microsoft.";
                        AppSettings.Instance.Provider = "You are logged in with Microsoft.";
                        AppSettings.Instance.Email = SupabaseStates.Instance.Supabase.Auth.CurrentSession.User!.Email;
                        VMLocator.CloudLoggedinViewModel.Email = SupabaseStates.Instance.Supabase.Auth.CurrentSession.User!.Email;
                        await SupabaseStates.Instance.Supabase.Auth.RefreshSession();
                        await _databaseProcess.CleanUpEditorLogDatabaseAsync();
                        await _syncProcess.SyncDbAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var cdialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"{ex.Message}",
                    CloseButtonText = "OK"
                };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
            }
        }
    }
}
