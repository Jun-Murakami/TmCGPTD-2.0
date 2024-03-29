﻿using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using TmCGPTD.Views;
using TmCGPTD.Models;
using FluentAvalonia.UI.Controls;
using Avalonia;
using Avalonia.Platform.Storage;
using System.Threading;
using Avalonia.Threading;
using System.Diagnostics;

namespace TmCGPTD.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        readonly SupabaseProcess _supabaseProcess = new();
        readonly DatabaseProcess _dbProcess = new();

        public MainViewModel()
        {
            _postButtonText = "Post";

            Editor1Clear = new RelayCommand(() => ExecuteClear(1));
            Editor2Clear = new RelayCommand(() => ExecuteClear(2));
            Editor3Clear = new RelayCommand(() => ExecuteClear(3));
            Editor4Clear = new RelayCommand(() => ExecuteClear(4));
            Editor5Clear = new RelayCommand(() => ExecuteClear(5));
            EditorAllClear = new RelayCommand(ExecuteClearAll);

            ImportChatLogCommand = new AsyncRelayCommand(ImportChatLogAsync);
            ExportChatLogCommand = new AsyncRelayCommand(ExportChatLogAsync);
            DeleteChatLogCommand = new AsyncRelayCommand(DeleteChatLogAsync);
            LoadChatListCommand = new RelayCommand<string>(async (keyword) => await LoadChatListAsync(keyword!));

            PostCommand = new AsyncRelayCommand(PostAsync);

            SavePhrasesCommand = new AsyncRelayCommand(SavePhrasesAsync);
            RenamePhrasesCommand = new AsyncRelayCommand(RenamePhrasesAsync);
            DeletePhrasesCommand = new AsyncRelayCommand(DeletePhrasesAsync);
            ImportPhrasesCommand = new AsyncRelayCommand(ImportPhrasesAsync);
            ExportPhrasesCommand = new AsyncRelayCommand(ExportPhrasesAsync);
            ClearPhrasesCommand = new RelayCommand(ClearPhrases);

            CopyToClipboardCommand = new AsyncRelayCommand(async () => await CopyToClipboard());

            EditorOneCommand = new RelayCommand(SetEditorOne);
            EditorThreeCommand = new RelayCommand(SetEditorThree);
            EditorFiveCommand = new RelayCommand(SetEditorFive);
            SystemMessageCommand = new RelayCommand(InsertSystemMessage);
            OpenOptionSettingsCommand = new RelayCommand(OpenOptionSettings);
            CloudSyncCommand = new AsyncRelayCommand(CloudSyncAsync);
            PhrasePresetsItems = new ObservableCollection<string>();
        }

        public IAsyncRelayCommand SavePhrasesCommand { get; }
        public IAsyncRelayCommand RenamePhrasesCommand { get; }
        public IAsyncRelayCommand DeletePhrasesCommand { get; }
        public IAsyncRelayCommand ImportPhrasesCommand { get; }
        public IAsyncRelayCommand ExportPhrasesCommand { get; }
        public ICommand ClearPhrasesCommand { get; }
        public IAsyncRelayCommand ImportChatLogCommand { get; }
        public IAsyncRelayCommand ExportChatLogCommand { get; }
        public IAsyncRelayCommand DeleteChatLogCommand { get; }
        public IAsyncRelayCommand PostCommand { get; }
        public ICommand LoadChatListCommand { get; }
        public ICommand Editor1Clear { get; }
        public ICommand Editor2Clear { get; }
        public ICommand Editor3Clear { get; }
        public ICommand Editor4Clear { get; }
        public ICommand Editor5Clear { get; }
        public ICommand EditorAllClear { get; }
        public ICommand CopyToClipboardCommand { get; }
        public ICommand EditorFiveCommand { get; }
        public ICommand EditorThreeCommand { get; }
        public ICommand EditorOneCommand { get; }
        public ICommand SystemMessageCommand { get; }
        public ICommand OpenOptionSettingsCommand { get; }
        public IAsyncRelayCommand CloudSyncCommand { get; }


        private string? _searchLogKeyword;
        public string? SearchLogKeyword
        {
            get => _searchLogKeyword;
            set
            {
                if (SetProperty(ref _searchLogKeyword, value))
                {
                    LoadChatListCommand.Execute(value);
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        SearchKeyword = string.Empty;
                        VMLocator.DataGridViewModel.SelectedItemIndex = -1;
                    }
                }
            }
        }

        private string? _searchKeyword;
        public string? SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private bool _autoSaveIsChecked;
        public bool AutoSaveIsChecked
        {
            get => _autoSaveIsChecked;
            set => SetProperty(ref _autoSaveIsChecked, value);
        }

        private bool _logPainIsOpened;
        public bool LogPainIsOpened
        {
            get => _logPainIsOpened;
            set => SetProperty(ref _logPainIsOpened, value);
        }

        private bool _logPainButtonIsVisible;
        public bool LogPainButtonIsVisible
        {
            get => _logPainButtonIsVisible;
            set => SetProperty(ref _logPainButtonIsVisible, value);
        }

        private string? _selectedLeftPane;
        public string? SelectedLeftPane
        {
            get => _selectedLeftPane;
            set => SetProperty(ref _selectedLeftPane, value);
        }

        public List<string> LeftPanes { get; } = new List<string>
        {
            "API Chat",
            "ChatGPT",
            "Gemini"
        };

        private string? _selectedRightPane;
        public string? SelectedRightPane
        {
            get => _selectedRightPane;
            set => SetProperty(ref _selectedRightPane, value);
        }

        public List<string> RightPanes { get; } = new List<string>
        {
            "Prompt Editor",
            "Preview"
        };

        public List<string> LogPanes { get; } = new List<string>
        {
            "Chat List"
        };

        private string? _selectedLogPain;
        public string? SelectedLogPain
        {
            get => _selectedLogPain;
            set => SetProperty(ref _selectedLogPain, value);
        }

        private ObservableCollection<string>? _phrasePresetsItems;
        public ObservableCollection<string>? PhrasePresetsItems
        {
            get => _phrasePresetsItems;
            set => SetProperty(ref _phrasePresetsItems, value);
        }

        private string? _selectedPhraseItem;
        public string? SelectedPhraseItem
        {
            get => _selectedPhraseItem;
            set
            {
                if (SetProperty(ref _selectedPhraseItem, value))
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        SelectedPhraseItemChangedAsync();
                    }
                }
            }
        }

        private bool _phraseExpanderIsOpened;
        public bool PhraseExpanderIsOpened
        {
            get => _phraseExpanderIsOpened;
            set => SetProperty(ref _phraseExpanderIsOpened, value);
        }

        private bool _isCopyButtonClicked;
        public bool IsCopyButtonClicked
        {
            get => _isCopyButtonClicked;
            set => SetProperty(ref _isCopyButtonClicked, value);
        }

        private string? _postButtonText;
        public string? PostButtonText
        {
            get => _postButtonText;
            set => SetProperty(ref _postButtonText, value);
        }

        private string? _inputTokens;
        public string? InputTokens
        {
            get => _inputTokens;
            set => SetProperty(ref _inputTokens, value);
        }

        private int _loginStatus;
        public int LoginStatus
        {
            get => _loginStatus;
            set => SetProperty(ref _loginStatus, value);
        }

        private string? _authCode;
        public string? AuthCode
        {
            get => _authCode;
            set
            {
                if (SetProperty(ref _authCode, value))
                {
                    GetSessionAdaptarAsync();
                }
            }
        }

        private async void GetSessionAdaptarAsync()
        {
            await _supabaseProcess.GetSessionAsync();
        }

        private Uri? _loginUri;
        public Uri? LoginUri
        {
            get => _loginUri;
            set => SetProperty(ref _loginUri, value);
        }

        private string? _syncLogText;
        public string? SyncLogText
        {
            get => _syncLogText;
            set => SetProperty(ref _syncLogText, value);
        }

        private bool _cloudIconSelector;
        public bool CloudIconSelector
        {
            get => _cloudIconSelector;
            set => SetProperty(ref _cloudIconSelector, value);
        }

        // ----------------------------------------------------------------------------------------------------------------------------
        private CancellationTokenSource cts = new CancellationTokenSource();

        private async Task PostAsync()
        {
            if (string.IsNullOrWhiteSpace(VMLocator.EditorViewModel.GetRecentText()) || VMLocator.ChatViewModel.ChatIsRunning)
            {
                return;
            }

            CancellationToken token = cts.Token; // キャンセルトークンを作成

            List<Dictionary<string, object>>? backupConversationHistory = null;

            try
            {
                if (VMLocator.ChatViewModel.ReEditIsOn && SelectedLeftPane == "API Chat")
                {
                    string? jsonCopy = System.Text.Json.JsonSerializer.Serialize(VMLocator.ChatViewModel.ConversationHistory);
                    backupConversationHistory = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonCopy);

                    jsonCopy = System.Text.Json.JsonSerializer.Serialize(VMLocator.ChatViewModel.LastConversationHistory);
                    VMLocator.ChatViewModel.ConversationHistory = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonCopy)!;

                }
                await _dbProcess.InserEditorLogDatabasetAsync();

                if (SelectedLeftPane == "ChatGPT")
                {
                    await VMLocator.WebChatViewModel.PostWebChat();
                }
                else if (SelectedLeftPane == "Gemini")
                {
                    await VMLocator.WebChatBardViewModel.PostWebChat();
                }
                else
                {
                    bool isNotCancelBeforeResponce = await VMLocator.ChatViewModel.GoChatAsync(token);

                    if (isNotCancelBeforeResponce)
                    {
                        VMLocator.DataGridViewModel.DataGridIsFocused = false;
                        VMLocator.DataGridViewModel.ChatList = await _dbProcess.SearchChatDatabaseAsync();
                        VMLocator.DataGridViewModel.SelectedItemIndex = 0;
                    }
                }

                VMLocator.EditorViewModel.TextClear();
                await _dbProcess.GetEditorLogDatabaseAsync();
                VMLocator.EditorViewModel.SelectedEditorLogIndex = -1;
                VMLocator.EditorViewModel.SelectedTemplateItemIndex = -1;
            }
            catch (Exception ex)
            {
                VMLocator.ChatViewModel.ChatIsRunning = false;
                var cdialog = new ContentDialog() { Title = "Error: " + ex.Message + ex.StackTrace, PrimaryButtonText = "OK" };
                await ContentDialogShowAsync(cdialog);

                if (backupConversationHistory != null)
                {
                    VMLocator.ChatViewModel.ConversationHistory = backupConversationHistory;
                }
                //throw;
            }
        }

        public void CancelPost()
        {
            cts.Cancel(); // キャンセル
            cts = new CancellationTokenSource(); // CancellationTokenSourceを作り直す
        }

        // ----------------------------------------------------------------------------------------------------------------------------

        private async Task ImportChatLogAsync()
        {
            var dialog = new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Select CSV file",
                FileTypeFilter = new List<FilePickerFileType>
                    {new("CSV files (*.csv)") { Patterns = new[] { "*.csv" } },
                    new("All files (*.*)") { Patterns = new[] { "*" } }}
            };

            var result = await (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow!.StorageProvider.OpenFilePickerAsync(dialog);

            if (result.Count > 0)
            {
                var selectedFilePath = result[0].Path.LocalPath;
                try
                {
                    var msg = await _dbProcess.ImportCsvToTableAsync(selectedFilePath);
                    var cdialog = new ContentDialog() { Title = msg, PrimaryButtonText = "OK" };
                    await ContentDialogShowAsync(cdialog);
                }
                catch (Exception ex)
                {
                    var cdialog = new ContentDialog() { Title = "Failed to import log." + Environment.NewLine + "Error: " + ex.Message + ex.StackTrace, PrimaryButtonText = "OK" };
                    await ContentDialogShowAsync(cdialog);
                }
                VMLocator.DataGridViewModel.ChatList = await _dbProcess.SearchChatDatabaseAsync();
            }
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private async Task ExportChatLogAsync()
        {
            var dialog = new FilePickerSaveOptions
            {
                Title = "Export CSV file",
                FileTypeChoices = new List<FilePickerFileType>
                    {new("CSV files (*.csv)") { Patterns = new[] { "*.csv" } },
                    new("All files (*.*)") { Patterns = new[] { "*" } }}
            };

            var result = await (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow!.StorageProvider.SaveFilePickerAsync(dialog);

            if (result != null)
            {
                var selectedFilePath = result.Path.LocalPath;
                string extension = Path.GetExtension(selectedFilePath);

                if (string.IsNullOrEmpty(extension))
                {
                    selectedFilePath += ".csv";
                }

                try
                {
                    var msg = await _dbProcess.ExportTableToCsvAsync(selectedFilePath);
                    var cdialog = new ContentDialog() { Title = msg, PrimaryButtonText = "OK" };
                    await ContentDialogShowAsync(cdialog);
                }
                catch (Exception ex)
                {
                    var cdialog = new ContentDialog() { Title = "Failed to export log." + Environment.NewLine + "Error: " + ex.Message + ex.StackTrace, PrimaryButtonText = "OK" };
                    await ContentDialogShowAsync(cdialog);
                }
            }
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private async Task DeleteChatLogAsync()
        {
            if (VMLocator.DataGridViewModel.SelectedItem == null || VMLocator.DataGridViewModel.SelectedItemIndex == -1)
            {
                return;
            }

            var dialog = new ContentDialog()
            {
                Title = $"Delete this chat log?{Environment.NewLine}{Environment.NewLine}'{VMLocator.DataGridViewModel.SelectedItem.Title}'",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };

            var contentDialogResult = await ContentDialogShowAsync(dialog);
            if (contentDialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            try
            {
                await _dbProcess.DeleteChatLogDatabaseAsync(VMLocator.DataGridViewModel.SelectedItem.Id);
                await VMLocator.ChatViewModel.InitializeChatAsync();
                VMLocator.DataGridViewModel.ChatList = await _dbProcess.SearchChatDatabaseAsync();
                VMLocator.DataGridViewModel.SelectedItemIndex = -1;
            }
            catch (Exception ex)
            {
                dialog = new ContentDialog() { Title = "Failed to delete log." + Environment.NewLine + "Error: " + ex.Message + ex.StackTrace, PrimaryButtonText = "OK" };
                await ContentDialogShowAsync(dialog);
            }
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private async Task LoadChatListAsync(string keyword)
        {
            VMLocator.DataGridViewModel.ChatList = await _dbProcess.SearchChatDatabaseAsync(keyword);
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private async void SelectedPhraseItemChangedAsync()
        {
            try
            {
                var loadedPhrases = await _dbProcess.GetPhrasePresetsAsync(SelectedPhraseItem!);
                VMLocator.PhrasePresetsViewModel.Phrases!.Clear();
                foreach (var phrase in loadedPhrases)
                {
                    VMLocator.PhrasePresetsViewModel.Phrases.Add(phrase);
                }
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog() { Title = $"Error: {ex.Message + ex.StackTrace}", PrimaryButtonText = "OK" };
                await ContentDialogShowAsync(dialog);
                ClearPhrases();
            }
            AppSettings.Instance.PhrasePreset = SelectedPhraseItem!;
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private async Task SavePhrasesAsync()
        {
            string? phrasesText;
            ContentDialog dialog;
            ContentDialogResult dialogResult;
            try
            {
                if (!string.IsNullOrWhiteSpace(SelectedPhraseItem))
                {
                    dialog = new ContentDialog() { Title = $"Overwrite '{SelectedPhraseItem}' preset?", PrimaryButtonText = "Overwrite", SecondaryButtonText = "New", CloseButtonText = "Cancel" };
                    dialogResult = await ContentDialogShowAsync(dialog);
                    if (dialogResult == ContentDialogResult.Primary)
                    {
                        phrasesText = string.Join(Environment.NewLine, VMLocator.PhrasePresetsViewModel.Phrases!);
                        await _dbProcess.UpdatePhrasePresetAsync(SelectedPhraseItem, phrasesText);
                        return;
                    }
                    else if (dialogResult != ContentDialogResult.Secondary)
                    {
                        return;
                    }
                }

                dialog = new ContentDialog()
                {
                    Title = "Please enter preset name.",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel"
                };

                var viewModel = new PhrasePresetsNameInputViewModel(dialog);
                dialog.Content = new PhrasePresetsNameInput()
                {
                    DataContext = viewModel
                };

                dialogResult = await ContentDialogShowAsync(dialog);
                if (dialogResult != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(viewModel.UserInput))
                {
                    return;
                }

                phrasesText = string.Join(Environment.NewLine, VMLocator.PhrasePresetsViewModel.Phrases!);
                await _dbProcess.SavePhrasesAsync(viewModel.UserInput, phrasesText);

                SelectedPhraseItem = "";
                await LoadPhraseItemsAsync();
                SelectedPhraseItem = viewModel.UserInput;
            }
            catch (Exception ex)
            {
                dialog = new ContentDialog() { Title = $"Error: {ex.Message + ex.StackTrace}", PrimaryButtonText = "OK" };
                await ContentDialogShowAsync(dialog);
            }
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private async Task RenamePhrasesAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedPhraseItem))
            {
                return;
            }

            var dialog = new ContentDialog()
            {
                Title = $"Please enter a new name to change from '{SelectedPhraseItem}'.",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };

            var viewModel = new PhrasePresetsNameInputViewModel(dialog);
            dialog.Content = new PhrasePresetsNameInput()
            {
                DataContext = viewModel
            };

            var contentDialogResult = await ContentDialogShowAsync(dialog);
            if (contentDialogResult != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(viewModel.UserInput))
            {
                return;
            }

            try
            {
                await _dbProcess.UpdatePhrasePresetNameAsync(SelectedPhraseItem, viewModel.UserInput);
            }
            catch (Exception ex)
            {
                dialog = new ContentDialog() { Title = $"Error: {ex.Message + ex.StackTrace}", PrimaryButtonText = "OK" };
                await ContentDialogShowAsync(dialog);
            }

            SelectedPhraseItem = "";
            await LoadPhraseItemsAsync();
            SelectedPhraseItem = viewModel.UserInput;
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private async Task DeletePhrasesAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedPhraseItem))
            {
                return;
            }

            var dialog = new ContentDialog()
            {
                Title = $"Delete preset '{SelectedPhraseItem}' - are you sure? ",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };

            var contentDialogResult = await ContentDialogShowAsync(dialog);
            if (contentDialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            try
            {
                await _dbProcess.DeletePhrasePresetAsync(SelectedPhraseItem);
            }
            catch (Exception ex)
            {
                dialog = new ContentDialog() { Title = $"Error: {ex.Message + ex.StackTrace}", PrimaryButtonText = "OK" };
                await ContentDialogShowAsync(dialog);
            }
            await LoadPhraseItemsAsync();
            SelectedPhraseItem = "";
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private async Task ImportPhrasesAsync()
        {
            var dialog = new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Select TXT file",
                FileTypeFilter = new List<FilePickerFileType>
                    {new("TXT files (*.txt)") { Patterns = new[] { "*.txt" } },
                    new("All files (*.*)") { Patterns = new[] { "*" } }}
            };
            var result = await (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow!.StorageProvider.OpenFilePickerAsync(dialog);

            if (result.Count > 0)
            {
                try
                {
                    var selectedFilePath = result[0].Path.LocalPath;
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(selectedFilePath);

                    var importedPhrases = await _dbProcess.ImportPhrasesFromTxtAsync(selectedFilePath);
                    var phrasesText = string.Join(Environment.NewLine, importedPhrases);

                    await _dbProcess.SavePhrasesAsync(fileNameWithoutExtension, phrasesText);

                    SelectedPhraseItem = "";
                    await LoadPhraseItemsAsync();
                    SelectedPhraseItem = fileNameWithoutExtension;
                }
                catch (Exception ex)
                {
                    var cdialog = new ContentDialog() { Title = $"Error: {ex.Message + ex.StackTrace}", PrimaryButtonText = "OK" };
                    await ContentDialogShowAsync(cdialog);
                }
            }
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private async Task ExportPhrasesAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedPhraseItem))
            {
                return;
            }

            var dialog = new FilePickerSaveOptions
            {
                Title = "Export TXT file",
                FileTypeChoices = new List<FilePickerFileType>
                    {new("TXT files (*.txt)") { Patterns = new[] { "*.txt" } },
                    new("All files (*.*)") { Patterns = new[] { "*" } }}
            };

            var result = await (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow!.StorageProvider.SaveFilePickerAsync(dialog);

            if (result != null)
            {
                var selectedFilePath = result.Path.LocalPath;
                string extension = Path.GetExtension(selectedFilePath);
                if (string.IsNullOrEmpty(extension))
                {
                    selectedFilePath += ".txt";
                }

                string? phrasesText = string.Join(Environment.NewLine, VMLocator.PhrasePresetsViewModel.Phrases!);
                try
                {
                    await File.WriteAllTextAsync(selectedFilePath, phrasesText);

                    var cdialog = new ContentDialog() { Title = $"Successfully exported phrase preset.", PrimaryButtonText = "OK" };
                    await ContentDialogShowAsync(cdialog);
                }
                catch (Exception ex)
                {
                    var cdialog = new ContentDialog() { Title = $"Error: {ex.Message + ex.StackTrace}", PrimaryButtonText = "OK" };
                    await ContentDialogShowAsync(cdialog);
                }
            }
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private void ClearPhrases()
        {
            VMLocator.PhrasePresetsViewModel.Phrases = new ObservableCollection<string>(Enumerable.Repeat("", 20));
            SelectedPhraseItem = "";
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        public async Task LoadPhraseItemsAsync()
        {
            var phrases = await _dbProcess.GetPhrasesAsync();
            PhrasePresetsItems!.Clear();

            foreach (var phrase in phrases)
            {
                PhrasePresetsItems.Add(phrase);
            }
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private void ExecuteClear(int editorNumber)
        {
            var editorViewModel = VMLocator.EditorViewModel;

            switch (editorNumber)
            {
                case 1:
                    editorViewModel.Editor1Text = string.Empty;
                    break;
                case 2:
                    editorViewModel.Editor2Text = string.Empty;
                    break;
                case 3:
                    editorViewModel.Editor3Text = string.Empty;
                    break;
                case 4:
                    editorViewModel.Editor4Text = string.Empty;
                    break;
                case 5:
                    editorViewModel.Editor5Text = string.Empty;
                    break;
            }
        }
        private void ExecuteClearAll()
        {
            for (int i = 1; i <= 5; i++)
            {
                ExecuteClear(i);
            }

            VMLocator.EditorViewModel.SelectedEditorLogIndex = -1;
            VMLocator.EditorViewModel.SelectedTemplateItemIndex = -1;
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private async Task CopyToClipboard()
        {
            if (string.IsNullOrWhiteSpace(VMLocator.EditorViewModel.GetRecentText()))
            {
                return;
            }

            IsCopyButtonClicked = true;
            if (ApplicationExtensions.GetTopLevel(Avalonia.Application.Current!)!.Clipboard != null)

            {
                await _dbProcess.InserEditorLogDatabasetAsync();
                await ApplicationExtensions.GetTopLevel(Avalonia.Application.Current!)!.Clipboard!.SetTextAsync(VMLocator.EditorViewModel.GetRecentText());


                await _dbProcess.GetEditorLogDatabaseAsync();
                VMLocator.EditorViewModel.SelectedEditorLogIndex = -1;
            }
            await Task.Delay(500);
            IsCopyButtonClicked = false;
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private void SetEditorOne()
        {
            VMLocator.EditorViewModel.EditorSeparateMode = 1;
        }

        private void SetEditorThree()
        {
            if (VMLocator.EditorViewModel.EditorSeparateMode != 3)
            {
                VMLocator.EditorViewModel.EditorSeparateMode = 3;
            }
            VMLocator.EditorViewModel.SeparatorResetThree();
        }

        private void SetEditorFive()
        {
            if (VMLocator.EditorViewModel.EditorSeparateMode != 5)
            {
                VMLocator.EditorViewModel.EditorSeparateMode = 5;
            }
            VMLocator.EditorViewModel.SeparatorResetFive();
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private void InsertSystemMessage()
        {
            Application.Current!.TryFindResource("My.Strings.SystemMessage", out object? resource1);
            VMLocator.EditorViewModel.Editor1Text = $"#System{Environment.NewLine}{Environment.NewLine}{resource1}";
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        public void OpenOptionSettings()
        {
            VMLocator.ChatViewModel.ChatViewIsVisible = false;
            VMLocator.WebChatViewModel.WebChatViewIsVisible = false;
            VMLocator.WebChatBardViewModel.WebChatBardViewIsVisible = false;
            VMLocator.MainWindowViewModel.OptionSettingsIsOpened = true;
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        private async Task CloudSyncAsync()
        {
            if (SupabaseStates.Instance.Supabase == null)
            {
                await _supabaseProcess.InitializeSupabaseAsync();
            }

            //1=ログイン前、2=ログイン中、3=ログイン後
            if (SupabaseStates.Instance.Supabase != null && SupabaseStates.Instance.Supabase.Auth.CurrentSession == null)
            {
                LoginStatus = 1;
            }
            else if (SupabaseStates.Instance.Supabase?.Auth.CurrentSession != null)
            {
                LoginStatus = 3;
            }
        }
        // ----------------------------------------------------------------------------------------------------------------------------

        public async Task<ContentDialogResult> ContentDialogShowAsync(ContentDialog dialog)
        {
            ContentDialogResult dialogResult;
            if (Dispatcher.UIThread.CheckAccess())
            {
                VMLocator.ChatViewModel.ChatViewIsVisible = false;
                VMLocator.WebChatViewModel.WebChatViewIsVisible = false;
                VMLocator.WebChatBardViewModel.WebChatBardViewIsVisible = false;
                dialogResult = await dialog.ShowAsync();
                VMLocator.ChatViewModel.ChatViewIsVisible = true;
                VMLocator.WebChatViewModel.WebChatViewIsVisible = true;
                VMLocator.WebChatBardViewModel.WebChatBardViewIsVisible = true;
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VMLocator.ChatViewModel.ChatViewIsVisible = false;
                    VMLocator.WebChatViewModel.WebChatViewIsVisible = false;
                    VMLocator.WebChatBardViewModel.WebChatBardViewIsVisible = false;
                });
                dialogResult = await dialog.ShowAsync();
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VMLocator.ChatViewModel.ChatViewIsVisible = true;
                    VMLocator.WebChatViewModel.WebChatViewIsVisible = true;
                    VMLocator.WebChatBardViewModel.WebChatBardViewIsVisible = true;
                });
            }

            return dialogResult;
        }
    }
}