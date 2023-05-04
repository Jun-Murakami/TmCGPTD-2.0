using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.Folding;
using AvaloniaEdit.TextMate;
using Avalonia.Media;
using TextMateSharp.Grammars;
using System;
using System.Collections.Generic;
using Avalonia.Input;
using TmCGPTD.ViewModels;
using System.Threading;

namespace TmCGPTD.Views
{
    public partial class Editor4AvalonEditView : UserControl
    {
        private TextEditor _editor4;
        private EditorViewModel _editorViewModel;
        private FoldingManager _foldingManager;
        private readonly SemaphoreSlim _updateSemaphore = new SemaphoreSlim(1, 1);
        public Editor4AvalonEditView()
        {
            InitializeComponent();
            // DataContextが変更されたときに、ViewModelのプロパティ変更を購読
            DataContextChanged += OnDataContextChanged;

            _editor4 = this.FindControl<TextEditor>("Editor4Avalon");
            _editor4.Document.TextChanged += OnEditorTextChenged;
            _editor4.AttachedToVisualTree += OnAttachedToVisualTree;
            _editor4.Document.Text = string.Empty;
        }
        private void OnAttachedToVisualTree(object sender, EventArgs e)
        {
            if (_editor4.Document.Text != VMLocator.EditorViewModel.Editor4Text)
            {
                _editor4.Document.Text = VMLocator.EditorViewModel.Editor4Text;
            }
        }
        private void OnEditorTextChenged(object sender, EventArgs e)
        {
            VMLocator.EditorViewModel.Editor4Text = _editor4.Document.Text;
        }
            private void OnDataContextChanged(object sender, EventArgs e)
        {
            if (_editorViewModel != null)
            {
                // Unsubscribe from the previous ViewModel's PropertyChanged event
                _editorViewModel.PropertyChanged -= OnEditorViewModelPropertyChanged;
            }

            _editorViewModel = DataContext as EditorViewModel;
            if (_editorViewModel != null)
            {
                    ConfigureTextEditor(_editor4);

                // Subscribe to the new ViewModel's PropertyChanged event
                _editorViewModel.PropertyChanged += OnEditorViewModelPropertyChanged;
            }
        }

        private async void OnEditorViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorViewModel.SelectedLang))
            {
                Language language = _editorViewModel.SelectedLang;

                if (_foldingManager != null)
                {
                    _foldingManager.Clear();
                    FoldingManager.Uninstall(_foldingManager);
                }
                if (language != null && language.Extensions != null)
                {
                    string scopeName = _registryOptions.GetScopeByLanguageId(language.Id);
                    _textMateInstallation.SetGrammar(scopeName);
                    if (language.Id == "xml")
                    {
                        _foldingManager = FoldingManager.Install(_editor4.TextArea);
                        return;
                    }
                }
            }
            else if (e.PropertyName == nameof(VMLocator.EditorViewModel.Editor4Text) && _editor4.Document.Text != VMLocator.EditorViewModel.Editor4Text)
            {
                // セマフォを取得（他の操作が完了するまで待機）
                await _updateSemaphore.WaitAsync();

                try
                {
                    _editor4.Document.Text = VMLocator.EditorViewModel.Editor4Text;
                }
                finally
                {
                    // セマフォを解放
                    _updateSemaphore.Release();
                }
            }
        }

        private TextMate.Installation _textMateInstallation;
        private ElementGenerator _generator = new ElementGenerator();
        private RegistryOptions _registryOptions;
        private int _currentTheme = 14;

        private void ConfigureTextEditor(TextEditor editor)
        {
            editor.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
            editor.ShowLineNumbers = true;
            editor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(150, 155, 138, 255));
            editor.TextArea.SelectionForeground = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            editor.Options.EnableImeSupport = true;
            editor.Options.ShowSpaces = true;
            editor.Options.ShowTabs = true;
            editor.TextArea.TextView.ElementGenerators.Add(_generator);

            _registryOptions = new RegistryOptions((ThemeName)_currentTheme);
            _textMateInstallation = editor.InstallTextMate(_registryOptions);

            //Debug.WriteLine("Lang :" + string.Join(Environment.NewLine,_registryOptions.GetAvailableLanguages()));

            editor.ContextMenu = new ContextMenu
            {
                ItemsSource = new List<MenuItem>
                {
                    new MenuItem { Header = "Copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) },
                    new MenuItem { Header = "Paste", InputGesture = new KeyGesture(Key.V, KeyModifiers.Control) },
                    new MenuItem { Header = "Cut", InputGesture = new KeyGesture(Key.X, KeyModifiers.Control) }
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
