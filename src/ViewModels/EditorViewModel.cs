using Avalonia;
using Avalonia.Controls;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TmCGPTD.Views;
using TmCGPTD.Models;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive;
using Microsoft.DeepDev;
using TextMateSharp.Grammars;


namespace TmCGPTD.ViewModels
{
    public class EditorViewModel : ViewModelBase
    {
        readonly DatabaseProcess _dbProcess = new();
        private readonly Subject<Unit> _textChanged = new();
        private ITokenizer? _tokenizer;

        public EditorViewModel()
        {
            EditorCommonFontSize = 16;
            EditorModeIsChecked = true;
            EditorSeparateMode = 5;
            SeparatorResetFive();

            InitializeTokenizer();
            TextClear();

            _editorLogLists = new ObservableCollection<EditorLogs>();

            PrevCommand = new RelayCommand(OnPrevCommand, () => SelectedEditorLogIndex > 0);
            NextCommand = new RelayCommand(OnNextCommand, () => SelectedEditorLogIndex < EditorLogLists!.Count - 1);

            SaveTemplateCommand = new AsyncRelayCommand(SaveTemplateAsync);
            RenameTemplateCommand = new AsyncRelayCommand(RenameTemplateAsync);
            DeleteTemplateCommand = new AsyncRelayCommand(DeleteTemplateAsync);
            ImportTemplateCommand = new AsyncRelayCommand(ImportTemplateAsync);
            ExportTemplateCommand = new AsyncRelayCommand(ExportTemplateAsync);

            _textChanged
                .Throttle(TimeSpan.FromMilliseconds(200)) // 200ミリ秒のデバウンス時間を設定
                .Subscribe(_ => GetRecentText());
        }

        public ICommand PrevCommand { get; }
        public ICommand NextCommand { get; }
        public IAsyncRelayCommand SaveTemplateCommand { get; }
        public IAsyncRelayCommand RenameTemplateCommand { get; }
        public IAsyncRelayCommand DeleteTemplateCommand { get; }
        public IAsyncRelayCommand ImportTemplateCommand { get; }
        public IAsyncRelayCommand ExportTemplateCommand { get; }


        private ObservableCollection<EditorLogs>? _editorLogLists;
        public ObservableCollection<EditorLogs>? EditorLogLists
        {
            get => _editorLogLists;
            set => SetProperty(ref _editorLogLists, value);
        }

        private EditorLogs? _selectedEditorLog;
        public EditorLogs? SelectedEditorLog
        {
            get => _selectedEditorLog;
            set
            {
                if (SetProperty(ref _selectedEditorLog, value))
                {
                    if (SelectedEditorLog != null && SelectedEditorLogIndex != -1)
                    {
                        DatabaseProcess _databaseProcess = new DatabaseProcess();
                        _databaseProcess.ShowEditorLogDatabaseAsync(SelectedEditorLog.Id);
                        SelectedTemplateItemIndex = -1;
                    }
                }
            }
        }

        private long _selectedEditorLogIndex;
        public long SelectedEditorLogIndex
        {
            get => _selectedEditorLogIndex;
            set
            {
                SetProperty(ref _selectedEditorLogIndex, value);
                ((RelayCommand)PrevCommand).NotifyCanExecuteChanged();
                ((RelayCommand)NextCommand).NotifyCanExecuteChanged();
            }
        }

        private void OnPrevCommand()
        {
            SelectedEditorLogIndex--;
        }

        private void OnNextCommand()
        {
            if (SelectedEditorLogIndex == -1)
            {
                SelectedEditorLogIndex = 0;
            }
            else
            {
                SelectedEditorLogIndex++;
            }
        }

        private ObservableCollection<Language>? _languages;
        public ObservableCollection<Language>? Languages
        {
            get => _languages;
            set => SetProperty(ref _languages, value);
        }
        private Language? _selectedLang;
        public Language? SelectedLang
        {
            get => _selectedLang;
            set => SetProperty(ref _selectedLang, value);
        }
        
        private int _selectedLangIndex;
        public int SelectedLangIndex
        {
            get => _selectedLangIndex;
            set => SetProperty(ref _selectedLangIndex, value);
        }

        private bool _editorModeIsChecked;
        public bool EditorModeIsChecked
        {
            get => _editorModeIsChecked;
            set => SetProperty(ref _editorModeIsChecked, value);
        }

        private ObservableCollection<PromptTemplate>? _templateItems;
        public ObservableCollection<PromptTemplate>? TemplateItems
        {
            get => _templateItems;
            set => SetProperty(ref _templateItems, value);
        }

        private PromptTemplate? _selectedTemplateItem;
        public PromptTemplate? SelectedTemplateItem
        {
            get => _selectedTemplateItem;
            set
            {
                if (SetProperty(ref _selectedTemplateItem, value) && _selectedTemplateItem != null && SelectedTemplateItemIndex != -1)
                {
                    _dbProcess.ShowTemplateAsync(_selectedTemplateItem.Id);
                    SelectedEditorLogIndex = -1;
                }
            }
        }

        private long _selectedTemplateItemIndex;
        public long SelectedTemplateItemIndex
        {
            get => _selectedTemplateItemIndex;
            set => SetProperty(ref _selectedTemplateItemIndex, value);
        }

        private GridLength _editorHeight1;
        public GridLength EditorHeight1
        {
            get => _editorHeight1;
            set => SetProperty(ref _editorHeight1, value);
        }
        private GridLength _editorHeight2;
        public GridLength EditorHeight2
        {
            get => _editorHeight2;
            set => SetProperty(ref _editorHeight2, value);
        }
        private GridLength _editorHeight3;
        public GridLength EditorHeight3
        {
            get => _editorHeight3;
            set => SetProperty(ref _editorHeight3, value);
        }
        private GridLength _editorHeight4;
        public GridLength EditorHeight4
        {
            get => _editorHeight4;
            set => SetProperty(ref _editorHeight4, value);
        }
        private GridLength _editorHeight5;
        public GridLength EditorHeight5
        {
            get => _editorHeight5;
            set => SetProperty(ref _editorHeight5, value);
        }

        private int _editorSeparateMode;
        public int EditorSeparateMode
        {
            get => _editorSeparateMode;
            set => SetProperty(ref _editorSeparateMode, value);
        }


        private async Task SaveTemplateAsync()
        {
            ContentDialog dialog;
            ContentDialogResult dialogResult;
            try
            {
                if (SelectedTemplateItemIndex > -1)
                {
                    dialog = new ContentDialog() { Title = $"Overwrite '{SelectedTemplateItem!.Title}' prompt template?", PrimaryButtonText = "Overwrite", SecondaryButtonText = "New", CloseButtonText = "Cancel" };
                    dialogResult = await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
                    if (dialogResult == ContentDialogResult.Primary)
                    {
                        await _dbProcess.UpdateTemplateAsync(SelectedTemplateItem.Title!);
                        return;
                    }
                    else if (dialogResult != ContentDialogResult.Secondary)
                    {
                        return;
                    }
                }
                else if (string.IsNullOrWhiteSpace(GetRecentText()))
                {
                    return;
                }

                dialog = new ContentDialog()
                {
                    Title = "Please enter prompt template name.",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel"
                };

                var viewModel = new PhrasePresetsNameInputViewModel(dialog);
                dialog.Content = new PhrasePresetsNameInput()
                {
                    DataContext = viewModel
                };

                dialogResult = await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
                if (dialogResult != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(viewModel.UserInput))
                {
                    return;
                }

                await _dbProcess.InsertTemplateDatabasetAsync(viewModel.UserInput);

                await _dbProcess.GetTemplateItemsAsync();

                SelectedTemplateItemIndex = -1;
            }
            catch (Exception ex)
            {
                dialog = new ContentDialog() { Title = $"Error: {ex.Message + ex.StackTrace}", PrimaryButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
            }
        }

        private async Task RenameTemplateAsync()
        {
            if (SelectedTemplateItemIndex < 0)
            {
                return;
            }

            var dialog = new ContentDialog()
            {
                Title = $"Please enter a new name to change from '{SelectedTemplateItem!.Title}'.",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };

            var viewModel = new PhrasePresetsNameInputViewModel(dialog);
            dialog.Content = new PhrasePresetsNameInput()
            {
                DataContext = viewModel
            };

            var contentDialogResult = await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
            if (contentDialogResult != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(viewModel.UserInput))
            {
                return;
            }

            try
            {
                await _dbProcess.UpdateTemplateNameAsync(SelectedTemplateItem.Title!, viewModel.UserInput);
            }
            catch (Exception ex)
            {
                dialog = new ContentDialog() { Title = $"Error: {ex.Message + ex.StackTrace}", PrimaryButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
            }
            await _dbProcess.GetTemplateItemsAsync();
        }

        private async Task DeleteTemplateAsync()
        {
            if (SelectedTemplateItemIndex < 0)
            {
                return;
            }

            var dialog = new ContentDialog()
            {
                Title = $"Delete propmt template '{SelectedTemplateItem!.Title}' - are you sure? ",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };

            var contentDialogResult = await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
            if (contentDialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            try
            {
                await _dbProcess.DeleteTemplateAsync(SelectedTemplateItem!.Title!);
            }
            catch (Exception ex)
            {
                dialog = new ContentDialog() { Title = $"Error: {ex.Message + ex.StackTrace}", PrimaryButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
            }
            SelectedTemplateItemIndex = -1;
            await _dbProcess.GetTemplateItemsAsync();
        }

        private async Task ImportTemplateAsync()
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

                    SelectedTemplateItemIndex = -1;

                    var importedTemplate = await _dbProcess.ImportTemplateFromTxtAsync(selectedFilePath);

                    await _dbProcess.InsertTemplateDatabasetAsync(fileNameWithoutExtension);

                    await _dbProcess.GetTemplateItemsAsync();
                }
                catch (Exception ex)
                {
                    var cdialog = new ContentDialog() { Title = $"Error: {ex.Message + ex.StackTrace}", PrimaryButtonText = "OK" };
                    await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                }
            }
        }

        private async Task ExportTemplateAsync()
        {
            if (SelectedTemplateItemIndex < 0 || string.IsNullOrWhiteSpace(GetRecentText()))
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

                var _editorViewModel = VMLocator.EditorViewModel;

                List<string> inputText = new()
                {
                    string.Join(Environment.NewLine, _editorViewModel.Editor1Text),
                    string.Join(Environment.NewLine, _editorViewModel.Editor2Text),
                    string.Join(Environment.NewLine, _editorViewModel.Editor3Text),
                    string.Join(Environment.NewLine, _editorViewModel.Editor4Text),
                    string.Join(Environment.NewLine, _editorViewModel.Editor5Text)
                };
                string finalText = string.Join(Environment.NewLine + "<---TMCGPT--->" + Environment.NewLine, inputText);

                try
                {
                    await File.WriteAllTextAsync(selectedFilePath, finalText);

                    var cdialog = new ContentDialog() { Title = $"Successfully exported prompt template.", PrimaryButtonText = "OK" };
                    await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                }
                catch (Exception ex)
                {
                    var cdialog = new ContentDialog() { Title = $"Error: {ex.Message + ex.StackTrace}", PrimaryButtonText = "OK" };
                    await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                }
            }
        }

        public void SeparatorResetFive()
        {
            EditorHeight1 = new GridLength(0.21, GridUnitType.Star);
            EditorHeight2 = new GridLength(0.30, GridUnitType.Star);
            EditorHeight3 = new GridLength(0.17, GridUnitType.Star);
            EditorHeight4 = new GridLength(0.24, GridUnitType.Star);
            EditorHeight5 = new GridLength(0.08, GridUnitType.Star);
        }

        public void SeparatorResetThree()
        {
            EditorHeight1 = new GridLength(0.34, GridUnitType.Star);
            EditorHeight2 = new GridLength(0.33, GridUnitType.Star);
            EditorHeight3 = new GridLength(0.33, GridUnitType.Star);
        }

        private async void InitializeTokenizer()
        {
            _tokenizer = await TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo"); // トークナイザーの初期化
        }

        public string GetRecentText()
        {
            List<string> inputText = new List<string>
            {
                string.Join(Environment.NewLine, Editor1Text!.Trim()),
                string.Join(Environment.NewLine, Editor2Text!.Trim()),
                string.Join(Environment.NewLine, Editor3Text!.Trim()),
                string.Join(Environment.NewLine, Editor4Text!.Trim()),
                string.Join(Environment.NewLine, Editor5Text!.Trim())
            };

            var outputText = inputText;
            outputText.RemoveAll(s => string.IsNullOrWhiteSpace(s)); // 空行を削除
            string outputTextStr = string.Join(Environment.NewLine + "---" + Environment.NewLine, outputText);

            VMLocator.MainViewModel.InputTokens = _tokenizer!.Encode(outputTextStr, Array.Empty<string>()).Count.ToString() + " Tokens"; // トークナイズ

            return outputTextStr;
        }


        public void TextClear()
        {
            Editor1Text = string.Empty;
            Editor2Text = string.Empty;
            Editor3Text = string.Empty;
            Editor4Text = string.Empty;
            Editor5Text = string.Empty;
            SelectedTemplateItemIndex = -1;
        }

        private double _editorCommonFontSize;
        public double EditorCommonFontSize
        {
            get => _editorCommonFontSize;
            set
            {
                if (SetProperty(ref _editorCommonFontSize, value))
                {
                    AppSettings.Instance.EditorFontSize = value;
                }
            }
        }

        private string? _editor1Text;
        public string? Editor1Text
        {
            get => _editor1Text;
            set
            {
                if (SetProperty(ref _editor1Text, value))
                {
                    _textChanged.OnNext(Unit.Default);
                }
            }
        }

        private string? _editor2Text;
        public string? Editor2Text
        {
            get => _editor2Text;
            set
            {
                if (SetProperty(ref _editor2Text, value))
                {
                    _textChanged.OnNext(Unit.Default);
                }
            }
        }

        private string? _editor3Text;
        public string? Editor3Text
        {
            get => _editor3Text;
            set
            {
                if (SetProperty(ref _editor3Text, value))
                {
                    _textChanged.OnNext(Unit.Default);
                }
            }
        }

        private string? _editor4Text;
        public string? Editor4Text
        {
            get => _editor4Text;
            set
            {
                if (SetProperty(ref _editor4Text, value))
                {
                    _textChanged.OnNext(Unit.Default);
                }
            }
        }

        private string? _editor5Text;
        public string? Editor5Text
        {
            get => _editor5Text;
            set
            {
                if (SetProperty(ref _editor5Text, value))
                {
                    _textChanged.OnNext(Unit.Default);
                }
            }
        }
    }
}