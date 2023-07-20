using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TmCGPTD.ViewModels;
using System.Diagnostics;
using System;
using TmCGPTD.Models;

namespace TmCGPTD.Views
{
    public partial class CloudLoggedinView : UserControl
    {
        readonly SupabaseStates _supabaseStates = SupabaseStates.Instance;
        readonly AppSettings _appSettings = AppSettings.Instance;
        public CloudLoggedinViewModel CloudLoggedinViewModel { get; } = new CloudLoggedinViewModel();
        public CloudLoggedinView()
        {
            InitializeComponent();

            DataContext = CloudLoggedinViewModel;
            VMLocator.CloudLoggedinViewModel = CloudLoggedinViewModel;

            AttachedToLogicalTree += CloudLoggedinView_AttachedToLogicalTree;
        }

        private async void CloudLoggedinView_AttachedToLogicalTree(object? sender, Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
        {
            if (_supabaseStates.Supabase?.Auth.CurrentSession != null && !string.IsNullOrEmpty(_appSettings.Provider))
            {
                CloudLoggedinViewModel.Provider = _appSettings.Provider!;
            }

            var dictionary2 = _supabaseStates.Supabase?.Auth.CurrentUser?.AppMetadata;
            if (dictionary2 != null && dictionary2.TryGetValue("provider", out object? value2))
            {
                //Debug.WriteLine($"You are logged in with {value2}.");
            }

            await SupabaseProcess.Instance.SingleSyncDbAsync();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
