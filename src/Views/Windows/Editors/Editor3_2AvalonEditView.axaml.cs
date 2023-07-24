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
using System.Collections.ObjectModel;
using System.Threading;
using System.Diagnostics;
using Avalonia;

namespace TmCGPTD.Views
{
    public partial class Editor3_2AvalonEditView : UserControl
    {
        private TextEditor _editor2;
        private EditorViewModel? _editorViewModel;
        private FoldingManager? _foldingManager;
        private readonly SemaphoreSlim _updateSemaphore = new SemaphoreSlim(1, 1);

        public Editor3_2AvalonEditView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;

            _editor2 = this.FindControl<TextEditor>("Editor3_2Avalon")!;
            _editor2.Document.Text = string.Empty;

            _editor2.AttachedToVisualTree += OnEditor2AttachedToVisualTree;

            ConfigureTextEditor(_editor2);
        }

        private void OnEditor2AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Language language = _editorViewModel!.SelectedLang!;

            if (_foldingManager != null)
            {
                _foldingManager.Clear();
                FoldingManager.Uninstall(_foldingManager);
            }
            if (language != null && language.Extensions != null)
            {
                string scopeName = _registryOptions!.GetScopeByLanguageId(language.Id);
                _textMateInstallation!.SetGrammar(scopeName);
                if (language.Id == "xml")
                {
                    _foldingManager = FoldingManager.Install(_editor2.TextArea);
                    return;
                }
            }
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_editorViewModel != null)
            {
                // Unsubscribe from the previous ViewModel's PropertyChanged event
                _editorViewModel.PropertyChanged -= OnEditorViewModelPropertyChanged;
            }

            _editorViewModel = DataContext as EditorViewModel;
            if (_editorViewModel != null)
            {
                if (VMLocator.EditorViewModel.Languages == null)
                {
                    ConfigureTextEditor(_editor2);
                }

                // Subscribe to the new ViewModel's PropertyChanged event
                _editorViewModel.PropertyChanged += OnEditorViewModelPropertyChanged;
            }
        }

        private void OnEditorViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorViewModel.SelectedLang))
            {
                Language language = _editorViewModel!.SelectedLang!;

                if (_foldingManager != null)
                {
                    _foldingManager.Clear();
                    FoldingManager.Uninstall(_foldingManager);
                }
                if (language != null && language.Extensions != null)
                {
                    string scopeName = _registryOptions!.GetScopeByLanguageId(language.Id);
                    _textMateInstallation!.SetGrammar(scopeName);
                    if (language.Id == "xml")
                    {
                        _foldingManager = FoldingManager.Install(_editor2.TextArea);
                        return;
                    }
                }
            }
        }

        private TextMate.Installation? _textMateInstallation;
        private ElementGenerator _generator = new ElementGenerator();
        private RegistryOptions? _registryOptions;
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

            editor.ContextMenu = new ContextMenu
            {
                ItemsSource = new List<MenuItem>
                {
                    new MenuItem { Header = "Copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) },
                    new MenuItem { Header = "Paste", InputGesture = new KeyGesture(Key.V, KeyModifiers.Control) },
                    new MenuItem { Header = "Cut", InputGesture = new KeyGesture(Key.X, KeyModifiers.Control) }
                }
            };

            VMLocator.EditorViewModel.Languages = new ObservableCollection<Language>(_registryOptions.GetAvailableLanguages());

        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
