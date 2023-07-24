using CommunityToolkit.Mvvm.Input;
using System;
using FluentAvalonia.UI.Controls;
using Avalonia.Controls;
using Avalonia;
using System.Threading.Tasks;
using TmCGPTD.Models;
using TmCGPTD.Views;

namespace TmCGPTD.ViewModels
{
    public class CloudLoggedinViewModel : ViewModelBase
    {
        readonly SupabaseProcess _supabaseProcess = new();
        public CloudLoggedinViewModel()
        {
            if (!string.IsNullOrWhiteSpace(AppSettings.Instance.Provider))
            {
                _provider = AppSettings.Instance.Provider;
            }

            if (SupabaseStates.Instance.Supabase?.Auth.CurrentSession != null)
            {
                _email = SupabaseStates.Instance.Supabase.Auth.CurrentSession!.User!.Email;
            }

            LogOutCommand = new AsyncRelayCommand(LogOutAsync);
            ChangeEmailCommand = new AsyncRelayCommand(ChangeEmailAsync);
            ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync);
            DeleteAccountCommand = new AsyncRelayCommand(DeleteAccountAsync);
        }

        public IAsyncRelayCommand LogOutCommand { get; }
        public IAsyncRelayCommand ChangeEmailCommand { get; }
        public IAsyncRelayCommand ChangePasswordCommand { get; }
        public IAsyncRelayCommand DeleteAccountCommand { get; }

        private string? _provider;
        public string? Provider
        {
            get => _provider;
            set => SetProperty(ref _provider, value);
        }

        private string? _email;
        public string? Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public async Task LogOutAsync()
        {
            await _supabaseProcess.LogOutAsync();
            SupabaseStates.Instance.Supabase!.Auth.Shutdown();
            VMLocator.MainViewModel.SyncLogText = "Logged out.";
            AppSettings.Instance.SyncIsOn = false;
            AppSettings.Instance.Provider = null;
            AppSettings.Instance.Email = null;
            AppSettings.Instance.Password = null;
            AppSettings.Instance.Session = null;
            VMLocator.MainViewModel.LoginStatus = 1;
            if (SupabaseStates.Instance.Supabase?.Auth.CurrentSession != null)
            {
                SupabaseStates.Instance.Supabase?.Auth.Shutdown();
            }
        }

        private async Task ChangeEmailAsync()
        {
            try
            {
                Application.Current!.TryFindResource("My.Strings.NewEmailInfo", out object? resource1);
                var dialog = new ContentDialog()
                {
                    Title = resource1,
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel"
                };

                var viewModel = new PhrasePresetsNameInputViewModel(dialog);
                dialog.Content = new PhrasePresetsNameInput()
                {
                    DataContext = viewModel
                };

                var dialogResult = await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
                if (dialogResult != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(viewModel.UserInput))
                {
                    return;
                }

                await _supabaseProcess.ChangeEmailAsync(viewModel.UserInput);

                Application.Current!.TryFindResource("My.Strings.EmailAddressChange", out object? resource2);
                var cdialog = new ContentDialog() { Title = "Information", Content = resource2, CloseButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
            }
            catch (Exception ex)
            {
                var cdialog = new ContentDialog() { Title = "Error", Content = $"{ex.Message}", CloseButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
            }
        }

        private async Task ChangePasswordAsync()
        {
            try
            {
                Application.Current!.TryFindResource("My.Strings.NewPasswordInfo", out object? resource1);
                var dialog = new ContentDialog()
                {
                    Title = resource1,
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel"
                };

                var viewModel = new PhrasePresetsNameInputViewModel(dialog);
                dialog.Content = new PhrasePresetsNameInput()
                {
                    DataContext = viewModel
                };

                var dialogResult = await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
                if (dialogResult != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(viewModel.UserInput))
                {
                    return;
                }

                await _supabaseProcess.ChangePasswordAsync(viewModel.UserInput);

                Application.Current!.TryFindResource("My.Strings.PasswordChanged", out object? resource2);
                var cdialog = new ContentDialog() { Title = "Information", Content = resource2, CloseButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
            }
            catch (Exception ex)
            {
                var cdialog = new ContentDialog() { Title = "Error", Content = $"{ex.Message}", CloseButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
            }
        }

        private async Task DeleteAccountAsync()
        {
            try
            {
                Application.Current!.TryFindResource("My.Strings.DeleteAccount", out object? resource1);
                var cdialog = new ContentDialog()
                {
                    Title = "Warning",
                    Content = resource1,
                    CloseButtonText = "Cancel",
                    PrimaryButtonText = "Delete"
                };
                var result = await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                if (result == ContentDialogResult.Primary)
                {
                    SyncProcess _syncProcess = new();
                    await SupabaseStates.Instance.Supabase!.Auth!.RefreshSession();
                    await _supabaseProcess.DeleteAccountAsync();
                    SupabaseStates.Instance.Supabase!.Auth.Shutdown();
                    await _syncProcess.DeleteManagementTableDbAsync();
                    AppSettings.Instance.SyncIsOn = false;
                    AppSettings.Instance.Provider = null;
                    AppSettings.Instance.Email = null;
                    AppSettings.Instance.Password = null;
                    VMLocator.CloudLoginViewModel.Email = null;
                    VMLocator.CloudLoginViewModel.Password = null;
                    AppSettings.Instance.Session = null;
                    VMLocator.MainViewModel.LoginStatus = 1;
                    Application.Current!.TryFindResource("My.Strings.DeletedAccount", out object? resource2);
                    var cdialog2 = new ContentDialog() { Title = resource2, CloseButtonText = "OK" };
                    await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog2);
                }
            }
            catch (Exception ex)
            {
                var cdialog = new ContentDialog() { Title = $"Error", Content = $"{ex.Message}", CloseButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
            }

        }
    }
}
