using Avalonia.Controls.ApplicationLifetimes;
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

namespace TmCGPTD.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        DatabaseProcess _dbProcess = new DatabaseProcess();
        public MainViewModel()
        {
            Editor1Clear = new RelayCommand(() => ExecuteClear(1));
            Editor2Clear = new RelayCommand(() => ExecuteClear(2));
            Editor3Clear = new RelayCommand(() => ExecuteClear(3));
            Editor4Clear = new RelayCommand(() => ExecuteClear(4));
            Editor5Clear = new RelayCommand(() => ExecuteClear(5));
            EditorAllClear = new RelayCommand(ExecuteClearAll);

            ImportChatLogCommand = new AsyncRelayCommand(ImportChatLogAsync);
            ExportChatLogCommand = new AsyncRelayCommand(ExportChatLogAsync);
            DeleteChatLogCommand = new AsyncRelayCommand(DeleteChatLogAsync);
            LoadChatListCommand = new RelayCommand<string>(async (keyword) => await LoadChatListAsync(keyword));

            PostCommand = new AsyncRelayCommand(PostAsync);

            SavePhrasesCommand = new AsyncRelayCommand(SavePhrasesAsync);
            RenamePhrasesCommand = new AsyncRelayCommand(RenamePhrasesAsync);
            DeletePhrasesCommand = new AsyncRelayCommand(DeletePhrasesAsync);
            ImportPhrasesCommand = new AsyncRelayCommand(ImportPhrasesAsync);
            ExportPhrasesCommand = new AsyncRelayCommand(ExportPhrasesAsync);
            ClearPhrasesCommand = new AsyncRelayCommand(ClearPhrasesAsync);

            CopyToClipboardCommand = new AsyncRelayCommand(async () => await CopyToClipboard());

            HotKeyDisplayCommand = new AsyncRelayCommand(HotKeyDisplayAsync);

            PhrasePresetsItems = new ObservableCollection<string>();
        }

        public IAsyncRelayCommand SavePhrasesCommand { get; }
        public IAsyncRelayCommand RenamePhrasesCommand { get; }
        public IAsyncRelayCommand DeletePhrasesCommand { get; }
        public IAsyncRelayCommand ImportPhrasesCommand { get; }
        public IAsyncRelayCommand ExportPhrasesCommand { get; }
        public IAsyncRelayCommand ClearPhrasesCommand { get; }
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
        public IAsyncRelayCommand HotKeyDisplayCommand { get; }


        private string _searchLogKeyword;
        public string SearchLogKeyword
        {
            get => _searchLogKeyword;
            set
            {
                if (SetProperty(ref _searchLogKeyword, value))
                {
                    LoadChatListCommand.Execute(value);
                }
            }
        }

        private bool _logPainIsOpened;
        public bool LogPainIsOpened
        {
            get => _logPainIsOpened;
            set => SetProperty(ref _logPainIsOpened, value);
        }

        private UserControl _selectedLeftView;
        public UserControl SelectedLeftView
        {
            get => _selectedLeftView;
            set => SetProperty(ref _selectedLeftView, value);
        }

        private string _selectedLeftPane;
        public string SelectedLeftPane
        {
            get => _selectedLeftPane;
            set => SetProperty(ref _selectedLeftPane, value);
        }

        public List<string> LeftPanes { get; } = new List<string>
        {
            "Chat",
            "WebChat"
        };


        private UserControl _selectedRightView;
        public UserControl SelectedRightView
        {
            get => _selectedRightView;
            set => SetProperty(ref _selectedRightView, value);
        }

        private string _selectedRightPane;
        public string SelectedRightPane
        {
            get => _selectedRightPane;
            set => SetProperty(ref _selectedRightPane, value);
        }

        public List<string> RightPanes { get; } = new List<string>
        {
            "Editor",
            "Preview"
        };

        public List<string> LogPanes { get; } = new List<string>
        {
            "Chat List"
        };

        private string _selectedLogPain;
        public string SelectedLogPain
        {
            get => _selectedLogPain;
            set => SetProperty(ref _selectedLogPain, value);
        }


        private ObservableCollection<string> _phrasePresetsItems;
        public ObservableCollection<string> PhrasePresetsItems
        {
            get => _phrasePresetsItems;
            set => SetProperty(ref _phrasePresetsItems, value);
        }

        private string _selectedPhraseItem;
        public string SelectedPhraseItem
        {
            get => _selectedPhraseItem;
            set
            {
                if (SetProperty(ref _selectedPhraseItem, value))
                {
                    SelectedPhraseItemChangedAsync();
                }
            }
        }


        private async Task PostAsync()
        {
            if (string.IsNullOrWhiteSpace(VMLocator.EditorViewModel.RecentText) || VMLocator.ChatViewModel.ChatIsRunning)
            {
                return;
            }

            try
            {
                await _dbProcess.InserEditorLogDatabasetAsync();

                if (SelectedLeftPane == "WebChat")
                {
                    await VMLocator.WebChatViewModel.PostWebChat();
                }
                else
                {
                    await VMLocator.ChatViewModel.GoChatAsync();
                    VMLocator.DataGridViewModel.ChatList = await _dbProcess.SearchChatDatabaseAsync();
                    VMLocator.DataGridViewModel.SelectedItemIndex = 0;
                }

                VMLocator.EditorViewModel.TextClear();
                await _dbProcess.GetEditorLogDatabaseAsync();
                VMLocator.EditorViewModel.SelectedEditorLogIndex = -1;

            }
            catch (Exception)
            {
                VMLocator.ChatViewModel.ChatIsRunning = false;
                //var cdialog = new ContentDialog() { Title = "Error: " + ex.Message, PrimaryButtonText = "OK" };
                //await ContentDialogShowAsync(cdialog);
            }
        }

        private async Task ImportChatLogAsync()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Title = "Select a .csv file",
                Filters = new List<FileDialogFilter>
                    {
                        new FileDialogFilter { Name = "CSV Files", Extensions = new List<string> { "csv" } },
                        new FileDialogFilter { Name = "All Files", Extensions = new List<string> { "*" } }
                    }
            };

            string[] result = await openFileDialog.ShowAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

            if (result != null && result.Length > 0)
            {
                var selectedFilePath = result[0];
                try
                {
                    var msg = await _dbProcess.ImportCsvToTableAsync(selectedFilePath);
                    var cdialog = new ContentDialog() { Title = msg, PrimaryButtonText = "OK" };
                    await ContentDialogShowAsync(cdialog);
                }
                catch (Exception ex)
                {
                    var cdialog = new ContentDialog() { Title = "Failed to import log." + Environment.NewLine + "Error: " + ex.Message, PrimaryButtonText = "OK" };
                    await ContentDialogShowAsync(cdialog);
                }
                VMLocator.DataGridViewModel.ChatList = await _dbProcess.SearchChatDatabaseAsync();
            }
        }

        private async Task ExportChatLogAsync()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Save CSV File",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "CSV Files", Extensions = new List<string> { "csv" } },
                    new FileDialogFilter { Name = "All Files", Extensions = new List<string> { "*" } }
                },
                DefaultExtension = "csv"
            };

            string result = await saveFileDialog.ShowAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

            if (!string.IsNullOrEmpty(result))
            {
                var selectedFilePath = result;

                try
                {
                    var msg = await _dbProcess.ExportTableToCsvAsync(selectedFilePath);
                    var cdialog = new ContentDialog() { Title = msg, PrimaryButtonText = "OK" };
                    await ContentDialogShowAsync(cdialog);
                }
                catch (Exception ex)
                {
                    var cdialog = new ContentDialog() { Title = "Failed to export log." + Environment.NewLine + "Error: " + ex.Message, PrimaryButtonText = "OK" };
                    await ContentDialogShowAsync(cdialog);
                }
            }
        }

        private async Task DeleteChatLogAsync()
        {
            if (VMLocator.DataGridViewModel.SelectedItem == null)
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
                VMLocator.DataGridViewModel.ChatList = await _dbProcess.SearchChatDatabaseAsync();
            }
            catch (Exception ex)
            {
                dialog = new ContentDialog() { Title = "Failed to delete log." + Environment.NewLine + "Error: " + ex.Message, PrimaryButtonText = "OK" };
                await ContentDialogShowAsync(dialog);
            }
        }

        private async Task LoadChatListAsync(string keyword)
        {
            VMLocator.DataGridViewModel.ChatList = await _dbProcess.SearchChatDatabaseAsync(keyword);
        }

        private async void SelectedPhraseItemChangedAsync()
        {
            try
            {
                var loadedPhrases = await _dbProcess.GetPhrasePresetsAsync(SelectedPhraseItem);
                VMLocator.PhrasePresetsViewModel.Phrases.Clear();
                foreach (var phrase in loadedPhrases)
                {
                    VMLocator.PhrasePresetsViewModel.Phrases.Add(phrase);
                }
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog(){ Title = $"Error: {ex.Message}", PrimaryButtonText = "OK" };
                await ContentDialogShowAsync(dialog);
                await ClearPhrasesAsync();
            }
            AppSettings.Instance.PhrasePreset = SelectedPhraseItem;
        }

        private async Task SavePhrasesAsync()
        {
            string phrasesText;
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
                        phrasesText = string.Join(Environment.NewLine, VMLocator.PhrasePresetsViewModel.Phrases);
                        await _dbProcess.UpdatePhrasePresetAsync(SelectedPhraseItem, phrasesText);
                        return;
                    }
                    else if(dialogResult != ContentDialogResult.Secondary)
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

                phrasesText = string.Join(Environment.NewLine, VMLocator.PhrasePresetsViewModel.Phrases);
                await _dbProcess.SavePhrasesAsync(viewModel.UserInput, phrasesText);

                await LoadPhraseItemsAsync();
                SelectedPhraseItem = viewModel.UserInput;
                }
            catch (Exception ex)
            {
                dialog = new ContentDialog() { Title = $"Error: {ex.Message}", PrimaryButtonText = "OK" };
                await ContentDialogShowAsync(dialog);
            }
        }

        private async Task RenamePhrasesAsync()
        {
            if(string.IsNullOrWhiteSpace(SelectedPhraseItem))
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
                dialog = new ContentDialog() { Title = $"Error: {ex.Message}", PrimaryButtonText = "OK" };
                await ContentDialogShowAsync(dialog);
            }
            await LoadPhraseItemsAsync();
            SelectedPhraseItem = viewModel.UserInput;
        }

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
                dialog = new ContentDialog() { Title = $"Error: {ex.Message}", PrimaryButtonText = "OK" };
                await ContentDialogShowAsync(dialog);
            }
            await LoadPhraseItemsAsync();
            SelectedPhraseItem = "";
        }

        private async Task ImportPhrasesAsync()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Title = "Select a .txt file",
                Filters = new List<FileDialogFilter>
                    {
                        new FileDialogFilter { Name = "TXT Files", Extensions = new List<string> { "txt" } },
                        new FileDialogFilter { Name = "All Files", Extensions = new List<string> { "*" } }
                    }
            };

            string[] result = await openFileDialog.ShowAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

            if (result != null && result.Length > 0)
            {
                try
                {
                    var selectedFilePath = result[0];
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(selectedFilePath);

                    var importedPhrases = await _dbProcess.ImportPhrasesFromTxtAsync(selectedFilePath);
                    var phrasesText = string.Join(Environment.NewLine, importedPhrases);

                    await _dbProcess.SavePhrasesAsync(fileNameWithoutExtension, phrasesText);

                    await LoadPhraseItemsAsync();
                    SelectedPhraseItem = fileNameWithoutExtension;
                }
                catch(Exception ex)
                {
                    var cdialog = new ContentDialog() { Title = $"Error: {ex.Message}", PrimaryButtonText = "OK" };
                    await ContentDialogShowAsync(cdialog);
                }
            }
        }

        private async Task ExportPhrasesAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedPhraseItem))
            {
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Save txt File",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "TXT Files", Extensions = new List<string> { "txt" } },
                    new FileDialogFilter { Name = "All Files", Extensions = new List<string> { "*" } }
                },
                DefaultExtension = "txt"
            };

            string result = await saveFileDialog.ShowAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

            if (!string.IsNullOrEmpty(result))
            {
                var selectedFilePath = result;

                var phrasesText = string.Join(Environment.NewLine, VMLocator.PhrasePresetsViewModel.Phrases);
                try
                {
                    await File.WriteAllTextAsync(selectedFilePath, phrasesText);

                    var cdialog = new ContentDialog() { Title = $"Successfully exported phrase preset.", PrimaryButtonText = "OK" };
                    await ContentDialogShowAsync(cdialog);
                }
                catch (Exception ex)
                {
                    var cdialog = new ContentDialog() { Title = $"Error: {ex.Message}", PrimaryButtonText = "OK" };
                    await ContentDialogShowAsync(cdialog);
                }
            }
        }

        private async Task ClearPhrasesAsync()
        {
            VMLocator.PhrasePresetsViewModel.Phrases = new ObservableCollection<string>(Enumerable.Repeat("", 20));
        }

        public async Task LoadPhraseItemsAsync()
        {
            var phrases = await _dbProcess.GetPhrasesAsync();
            PhrasePresetsItems.Clear();

            foreach (var phrase in phrases)
            {
                PhrasePresetsItems.Add(phrase);
            }
        }

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
        }

        private async Task CopyToClipboard()
        {
            if(Avalonia.Application.Current.Clipboard != null)
            {
                await Avalonia.Application.Current.Clipboard.SetTextAsync(VMLocator.EditorViewModel.RecentText);
                var dialog = new ContentDialog() { Title = $"Copied to clipboard.", PrimaryButtonText = "OK" };
                await ContentDialogShowAsync(dialog);
            }
        }

        private async Task HotKeyDisplayAsync()
        {
            var dialog = new ContentDialog()
            {
                Title = $"Keyboard shortcuts",
                PrimaryButtonText = "OK"
            };

            dialog.Content = new HotKeyDisplayView();
            await ContentDialogShowAsync(dialog);
        }

        private async Task<ContentDialogResult> ContentDialogShowAsync(ContentDialog dialog)
        {
            VMLocator.ChatViewModel.ChatViewIsVisible = false;
            VMLocator.WebChatViewModel.WebChatViewIsVisible = false;
            var dialogResult = await dialog.ShowAsync();
            VMLocator.ChatViewModel.ChatViewIsVisible = true;
            VMLocator.WebChatViewModel.WebChatViewIsVisible = true;
            return dialogResult;
        }

    }
}