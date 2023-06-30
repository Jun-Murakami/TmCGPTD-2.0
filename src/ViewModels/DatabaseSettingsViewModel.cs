using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia;
using FluentAvalonia.UI.Controls;
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using TmCGPTD.Views;
using TmCGPTD.Models;

namespace TmCGPTD.ViewModels
{
    public class DatabaseSettingsViewModel : ViewModelBase
    {
        DatabaseProcess _dbProcess = new DatabaseProcess();
        SupabaseProcess _supabaseProcess = new SupabaseProcess();

        public DatabaseSettingsViewModel()
        {
            ProcessLog = "";

            MoveDatabaseCommand = new AsyncRelayCommand(MoveDatabaseAsync);
            LoadDatabaseCommand = new AsyncRelayCommand(LoadDatabaseAsync);
        }

        public IAsyncRelayCommand MoveDatabaseCommand { get; }
        public IAsyncRelayCommand LoadDatabaseCommand { get; }

        private AppSettings _appSettings => AppSettings.Instance;

        public string DatabasePath
        {
            get => _appSettings.DbPath;
            set
            {
                if (_appSettings.DbPath != value)
                {
                    _appSettings.DbPath = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _processLog;
        public string? ProcessLog
        {
            get => _processLog;
            set => SetProperty(ref _processLog, value);
        }

        private async Task MoveDatabaseAsync()
        {
            var dialog = new FilePickerSaveOptions
            {
                Title = "Move database file",
                SuggestedFileName = "log_database",
                FileTypeChoices = new List<FilePickerFileType>
            {new("Database files (*.db)") { Patterns = new[] { "*.db" } },
            new("All files (*.*)") { Patterns = new[] { "*" } }}
            };

            try
            {
                var result = await (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow!.StorageProvider.SaveFilePickerAsync(dialog);

                if (result != null)
                {
                    var selectedFilePath = result.Path.LocalPath;
                    string extension = Path.GetExtension(selectedFilePath);
                    if (string.IsNullOrEmpty(extension))
                    {
                        selectedFilePath += ".db";
                    }

                    if (selectedFilePath == DatabasePath)
                    {
                        return;
                    }

                    File.Copy(DatabasePath, selectedFilePath, true);

                    if (File.Exists(selectedFilePath))
                    {
                        File.Delete(DatabasePath);

                        DatabasePath = selectedFilePath;
                        MainWindow mainWindow = (MainWindow)(Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow!;
                        mainWindow.SaveWindowSizeAndPosition();
                        ProcessLog = "Database file moved successfully.";
                    }
                }
            }
            catch (Exception ex)
            {
                ProcessLog = "Error: " + ex.Message;
            }
        }

        private async Task LoadDatabaseAsync()
        {
            var dialog = new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Select database file",
                FileTypeFilter = new List<FilePickerFileType>
                    {new("TXT files (*.db)") { Patterns = new[] { "*.db" } },
                    new("All files (*.*)") { Patterns = new[] { "*" } }}
            };
            var result = await (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow!.StorageProvider.OpenFilePickerAsync(dialog);

            if (result.Count > 0)
            {
                try
                {
                    var selectedFilePath = result[0].Path.LocalPath;
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(selectedFilePath);

                    if (selectedFilePath == DatabasePath)
                    {
                        return;
                    }

                    if(!await _dbProcess.CheckTableExists(selectedFilePath))
                    {
                        ProcessLog = "Error: Invalid database file.";
                        return;
                    }

                    DatabasePath = selectedFilePath;

                    await _dbProcess.DbLoadToMemoryAsync();
                    await VMLocator.MainViewModel.LoadPhraseItemsAsync();
                    VMLocator.MainViewModel.SelectedPhraseItem = "";

                    await _dbProcess.GetEditorLogDatabaseAsync();
                    await _dbProcess.GetTemplateItemsAsync();

                    VMLocator.DataGridViewModel.ChatList = await _dbProcess.SearchChatDatabaseAsync();

                    VMLocator.EditorViewModel.SelectedEditorLogIndex = -1;
                    VMLocator.EditorViewModel.SelectedTemplateItemIndex = -1;

                    MainWindow mainWindow = (MainWindow)(Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow!;
                    mainWindow.SaveWindowSizeAndPosition();

                    ProcessLog = "Database file loaded successfully.";
                }
                catch (Exception ex)
                {
                    ProcessLog = "Error: " + ex.Message;
                }
            }
        }

    }
}