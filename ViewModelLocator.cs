using TmCGPTD.ViewModels;

namespace TmCGPTD
{
    internal class VMLocator
    {
        private static MainWindowViewModel _mainWindowViewModel;
        public static MainWindowViewModel MainWindowViewModel
        {
            get => _mainWindowViewModel ??= new MainWindowViewModel();
            set => _mainWindowViewModel = value;
        }

        private static MainViewModel _mainViewModel;
        public static MainViewModel MainViewModel
        {
            get => _mainViewModel ??= new MainViewModel();
            set => _mainViewModel = value;
        }

        private static ChatViewModel _chatViewModel;
        public static ChatViewModel ChatViewModel
        {
            get => _chatViewModel ??= new ChatViewModel();
            set => _chatViewModel = value;
        }

        private static WebChatViewModel _webChatViewModel;
        public static WebChatViewModel WebChatViewModel
        {
            get => _webChatViewModel ??= new WebChatViewModel();
            set => _webChatViewModel = value;
        }

        private static DataGridViewModel _dataGridViewModel;
        public static DataGridViewModel DataGridViewModel
        {
            get => _dataGridViewModel ??= new DataGridViewModel();
            set => _dataGridViewModel = value;
        }

        private static EditorViewModel _editorViewModel;
        public static EditorViewModel EditorViewModel
        {
            get => _editorViewModel ??= new EditorViewModel();
            set => _editorViewModel = value;
        }

        private static PreviewViewModel _previewViewModel;
        public static PreviewViewModel PreviewViewModel
        {
            get => _previewViewModel ??= new PreviewViewModel();
            set => _previewViewModel = value;
        }

        private static PhrasePresetsViewModel _phrasePresetsViewModel;
        public static PhrasePresetsViewModel PhrasePresetsViewModel
        {
            get => _phrasePresetsViewModel ??= new PhrasePresetsViewModel();
            set => _phrasePresetsViewModel = value;
        }

        private static Editor2TextBoxViewModel _editor2TextBoxViewModel;
        public static Editor2TextBoxViewModel Editor2TextBoxViewModel
        {
            get => _editor2TextBoxViewModel ??= new Editor2TextBoxViewModel();
            set => _editor2TextBoxViewModel = value;
        }

        private static Editor2AvalonEditViewModel _editor2AvalonEditViewModel;
        public static Editor2AvalonEditViewModel Editor2AvalonEditViewModel
        {
            get => _editor2AvalonEditViewModel ??= new Editor2AvalonEditViewModel();
            set => _editor2AvalonEditViewModel = value;
        }

        private static Editor4TextBoxViewModel _editor4TextBoxViewModel;
        public static Editor4TextBoxViewModel Editor4TextBoxViewModel
        {
            get => _editor4TextBoxViewModel ??= new Editor4TextBoxViewModel();
            set => _editor4TextBoxViewModel = value;
        }

        private static Editor4AvalonEditViewModel _editor4AvalonEditViewModel;
        public static Editor4AvalonEditViewModel Editor4AvalonEditViewModel
        {
            get => _editor4AvalonEditViewModel ??= new Editor4AvalonEditViewModel();
            set => _editor4AvalonEditViewModel = value;
        }

    }
}
