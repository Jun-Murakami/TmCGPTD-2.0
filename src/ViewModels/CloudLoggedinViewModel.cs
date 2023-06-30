using CommunityToolkit.Mvvm.Input;
using System;
using FluentAvalonia.UI.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TmCGPTD.Models;
using TmCGPTD.Views;

namespace TmCGPTD.ViewModels
{
    public class CloudLoggedinViewModel : ViewModelBase
    {
        readonly SupabaseProcess _supabaseProcess = new();
        readonly SupabaseStates _supabaseStates = SupabaseStates.Instance;
        public CloudLoggedinViewModel()
        {
            if (!string.IsNullOrWhiteSpace(AppSettings.Instance.Provider))
            {
                Provider = AppSettings.Instance.Provider;
            }

            if (SupabaseStates.Instance.Supabase?.Auth.CurrentSession != null)
            {
                Email = SupabaseStates.Instance.Supabase.Auth.CurrentSession!.User!.Email;
            }

            LogOutCommand = new AsyncRelayCommand(LogOutAsync);
            ChangeEmailCommand = new AsyncRelayCommand(ChangeEmailAsync);
            DeleteAccountCommand = new AsyncRelayCommand(DeleteAccountAsync);

        }

        public IAsyncRelayCommand LogOutCommand { get; }
        public IAsyncRelayCommand ChangeEmailCommand { get; }
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

        private async Task LogOutAsync()
        {
            await _supabaseProcess.LogOutAsync();
            VMLocator.MainViewModel.SyncLogText = "Logged out.";
            AppSettings.Instance.SyncIsOn = false;
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
                var dialog = new ContentDialog()
                {
                    Title = "Please enter new email address.",
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

                var cdialog = new ContentDialog() { Title = $"Information", Content = $"A confirmation email has been sent to your new address.{Environment.NewLine}Click on the link to activate the changes.", CloseButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
            }
            catch (Exception ex)
            {
                var cdialog = new ContentDialog() { Title = $"Error", Content = $"{ex.Message}", CloseButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
            }
        }

        private async Task DeleteAccountAsync()
        {
            try
            {
                var cdialog = new ContentDialog()
                {
                    Title = $"Warning",
                    Content = $"All your data and credentials will be deleted from the cloud database.{Environment.NewLine}This operation cannot be undone. Are you sure ?",
                    CloseButtonText = "Cancel",
                    PrimaryButtonText = "Delete"
                };
                var result = await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                if (result == ContentDialogResult.Primary)
                {
                    await _supabaseProcess.DeleteAccountAsync();
                    AppSettings.Instance.SyncIsOn = false;
                    VMLocator.MainViewModel.LoginStatus = 4;

                    var cdialog2 = new ContentDialog() { Title = $"Your account has been deleted.", CloseButtonText = "OK" };
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
