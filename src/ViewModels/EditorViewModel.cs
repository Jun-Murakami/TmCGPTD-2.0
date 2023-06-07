using Avalonia;
using Avalonia.Controls;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TextMateSharp.Grammars;
using TmCGPTD.Views;
using TmCGPTD.Models;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace TmCGPTD.ViewModels
{
    public class EditorViewModel : ViewModelBase
    {
        DatabaseProcess _dbProcess = new DatabaseProcess();
        public EditorViewModel()
        {
            EditorCommonFontSize = 16;
            EditorModeIsChecked = true;

            TextClear();

            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName.StartsWith("Editor"))
                {
                    TextInput_TextChanged();
                }
            };

            _editorLogLists = new ObservableCollection<EditorLogs>();

            PrevCommand = new RelayCommand(OnPrevCommand, () => SelectedEditorLogIndex > 0);
            NextCommand = new RelayCommand(OnNextCommand, () => SelectedEditorLogIndex < EditorLogLists.Count - 1);

            SaveTemplateCommand = new AsyncRelayCommand(SaveTemplateAsync);
            RenameTemplateCommand = new AsyncRelayCommand(RenameTemplateAsync);
            DeleteTemplateCommand = new AsyncRelayCommand(DeleteTemplateAsync);
            ImportTemplateCommand = new AsyncRelayCommand(ImportTemplateAsync);
            ExportTemplateCommand = new AsyncRelayCommand(ExportTemplateAsync);

        }

        public ICommand PrevCommand { get; }
        public ICommand NextCommand { get; }
        public IAsyncRelayCommand SaveTemplateCommand { get; }
        public IAsyncRelayCommand RenameTemplateCommand { get; }
        public IAsyncRelayCommand DeleteTemplateCommand { get; }
        public IAsyncRelayCommand ImportTemplateCommand { get; }
        public IAsyncRelayCommand ExportTemplateCommand { get; }


        private ObservableCollection<EditorLogs> _editorLogLists;
        public ObservableCollection<EditorLogs> EditorLogLists
        {
            get => _editorLogLists;
            set => SetProperty(ref _editorLogLists, value);
        }


        private EditorLogs _selectedEditorLog;
        public EditorLogs SelectedEditorLog
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

        private ObservableCollection<Language> _languages;
        public ObservableCollection<Language> Languages
        {
            get => _languages;
            set => SetProperty(ref _languages, value);
        }
        private Language _selectedLang;
        public Language SelectedLang
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

        private UserControl _selectedEditor2View;
        public UserControl SelectedEditor2View
        {
            get => _selectedEditor2View;
            set => SetProperty(ref _selectedEditor2View, value);
        }

        private UserControl _selectedEditor4View;
        public UserControl SelectedEditor4View
        {
            get => _selectedEditor4View;
            set => SetProperty(ref _selectedEditor4View, value);
        }

        private bool _editorModeIsChecked;
        public bool EditorModeIsChecked
        {
            get => _editorModeIsChecked;
            set
            {
                if (SetProperty(ref _editorModeIsChecked, value))
                {
                    UpdateSelectedEditorView();
                }
            }
        }

        private Editor2AvalonEditView _editor2AvalonEditView;
        private Editor2TextBoxView _editor2TextBoxView;
        private Editor4AvalonEditView _editor4AvalonEditView;
        private Editor4TextBoxView _editor4TextBoxView;
        private void UpdateSelectedEditorView()
        {
            if (_editorModeIsChecked)
            {
                if (_editor2AvalonEditView == null)
                {
                    _editor2AvalonEditView = new Editor2AvalonEditView();
                }
                if (_editor4AvalonEditView == null)
                {
                    _editor4AvalonEditView = new Editor4AvalonEditView();
                }
                SelectedEditor2View = _editor2AvalonEditView;
                SelectedEditor4View = _editor4AvalonEditView;
            }
            else
            {
                if (_editor2TextBoxView == null)
                {
                    _editor2TextBoxView = new Editor2TextBoxView();
                }
                if (_editor4TextBoxView == null)
                {
                    _editor4TextBoxView = new Editor4TextBoxView();
                }
                SelectedEditor2View = _editor2TextBoxView;
                SelectedEditor4View = _editor4TextBoxView;
            }
        }

        private ObservableCollection<PromptTemplate> _templateItems;
        public ObservableCollection<PromptTemplate> TemplateItems
        {
            get => _templateItems;
            set => SetProperty(ref _templateItems, value);
        }

        private PromptTemplate _selectedTemplateItem;
        public PromptTemplate SelectedTemplateItem
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


        private async Task SaveTemplateAsync()
        {
            string phrasesText;
            ContentDialog dialog;
            ContentDialogResult dialogResult;
            try
            {
                if (SelectedTemplateItemIndex > -1)
                {
                    dialog = new ContentDialog() { Title = $"Overwrite '{SelectedTemplateItem.Title}' prompt template?", PrimaryButtonText = "Overwrite", SecondaryButtonText = "New", CloseButtonText = "Cancel" };
                    dialogResult = await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
                    if (dialogResult == ContentDialogResult.Primary)
                    {
                        await _dbProcess.UpdateTemplateAsync(SelectedTemplateItem.Title);
                        return;
                    }
                    else if (dialogResult != ContentDialogResult.Secondary)
                    {
                        return;
                    }
                }
                else if(string.IsNullOrWhiteSpace(RecentText))
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
                dialog = new ContentDialog() { Title = $"Error: {ex.Message}", PrimaryButtonText = "OK" };
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
                Title = $"Please enter a new name to change from '{SelectedTemplateItem.Title}'.",
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
                await _dbProcess.UpdateTemplateNameAsync(SelectedTemplateItem.Title, viewModel.UserInput);
            }
            catch (Exception ex)
            {
                dialog = new ContentDialog() { Title = $"Error: {ex.Message}", PrimaryButtonText = "OK" };
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
                Title = $"Delete propmt template '{SelectedTemplateItem.Title}' - are you sure? ",
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
                await _dbProcess.DeleteTemplateAsync(SelectedTemplateItem.Title);
            }
            catch (Exception ex)
            {
                dialog = new ContentDialog() { Title = $"Error: {ex.Message}", PrimaryButtonText = "OK" };
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
            var result = await (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow.StorageProvider.OpenFilePickerAsync(dialog);

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
                    var cdialog = new ContentDialog() { Title = $"Error: {ex.Message}", PrimaryButtonText = "OK" };
                    await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                }
            }
        }

        private async Task ExportTemplateAsync()
        {
            if (SelectedTemplateItemIndex < 0 || string.IsNullOrWhiteSpace(RecentText))
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

            var result = await (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow.StorageProvider.SaveFilePickerAsync(dialog);

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
                    var cdialog = new ContentDialog() { Title = $"Error: {ex.Message}", PrimaryButtonText = "OK" };
                    await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                }
            }
        }

        public void SeparatorReset()
        {
            EditorHeight1 = new GridLength(0.21, GridUnitType.Star);
            EditorHeight2 = new GridLength(0.30, GridUnitType.Star);
            EditorHeight3 = new GridLength(0.17, GridUnitType.Star);
            EditorHeight4 = new GridLength(0.24, GridUnitType.Star);
            EditorHeight5 = new GridLength(0.08, GridUnitType.Star);
        }

        public void TextInput_TextChanged()
        {
            List<string> inputText = new List<string>();
            inputText.Clear();
            inputText.Add(string.Join(Environment.NewLine, Editor1Text));
            inputText.Add(string.Join(Environment.NewLine, Editor2Text));
            inputText.Add(string.Join(Environment.NewLine, Editor3Text));
            inputText.Add(string.Join(Environment.NewLine, Editor4Text));
            inputText.Add(string.Join(Environment.NewLine, Editor5Text));

            var outputText = inputText;
            outputText.RemoveAll(s => string.IsNullOrWhiteSpace(s)); // ‹ós‚ðíœ

            RecentText = string.Join(Environment.NewLine + "---" + Environment.NewLine, outputText);
        }

        public void TextClear()
        {
            Editor1Text = string.Empty;
            Editor2Text = string.Empty;
            Editor3Text = string.Empty;
            Editor4Text = string.Empty;
            Editor5Text = string.Empty;
            RecentText = string.Empty;
            SelectedTemplateItemIndex = -1;
        }

        private double _editorCommonFontSize;
        public double EditorCommonFontSize
        {
            get => _editorCommonFontSize;
            set => SetProperty(ref _editorCommonFontSize, value);
        }

        private string _recentText;
        public string RecentText
        {
            get => _recentText;
            set => SetProperty(ref _recentText, value);
        }

        private string _editor1Text;
        public string Editor1Text
        {
            get => _editor1Text;
            set => SetProperty(ref _editor1Text, value);
        }

        private string _editor2Text;
        public string Editor2Text
        {
            get => _editor2Text;
            set => SetProperty(ref _editor2Text, value);
        }

        private string _editor3Text;
        public string Editor3Text
        {
            get => _editor3Text;
            set => SetProperty(ref _editor3Text, value);
        }

        private string _editor4Text;
        public string Editor4Text
        {
            get => _editor4Text;
            set => SetProperty(ref _editor4Text, value);
        }

        private string _editor5Text;
        public string Editor5Text
        {
            get => _editor5Text;
            set => SetProperty(ref _editor5Text, value);
        }
    }

}