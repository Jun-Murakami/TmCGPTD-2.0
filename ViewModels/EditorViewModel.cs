using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TextMateSharp.Grammars;
using TmCGPTD.Views;
using TmCGPTD.Models;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace TmCGPTD.ViewModels
{
    public class EditorViewModel : ViewModelBase
    {
        DatabaseProcess _dbProcess = new DatabaseProcess();
        public EditorViewModel()
        {
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

            SelectedEditorLogIndex = -1;
        }

        public ICommand PrevCommand { get; }
        public ICommand NextCommand { get; }

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
                    if (SelectedEditorLog != null)
                    {
                        DatabaseProcess _databaseProcess = new DatabaseProcess();
                        _databaseProcess.ShowEditorLogDatabaseAsync(SelectedEditorLog.Id);
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