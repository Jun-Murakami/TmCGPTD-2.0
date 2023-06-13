using System.Collections.ObjectModel;
using System.Linq;

namespace TmCGPTD.ViewModels
{
    public class PhrasePresetsViewModel : ViewModelBase
    {
        public PhrasePresetsViewModel()
        {
            Phrases = new ObservableCollection<string>(Enumerable.Repeat("", 20));

            CtrlKeyIsDown = false;
            AltKeyIsDown = false;
        }

        private ObservableCollection<string> _phrases;
        public ObservableCollection<string> Phrases
        {
            get => _phrases;
            set => SetProperty(ref _phrases, value);
        }

        private int _keyDownNum;
        public int KeyDownNum
        {
            get => _keyDownNum;
            set => SetProperty(ref _keyDownNum, value);
        }

        private bool _ctrlKeyIsDown;
        public bool CtrlKeyIsDown
        {
            get => _ctrlKeyIsDown;
            set => SetProperty(ref _ctrlKeyIsDown, value);
        }

        private bool _altKeyIsDown;
        public bool AltKeyIsDown
        {
            get => _altKeyIsDown;
            set => SetProperty(ref _altKeyIsDown, value);
        }
    }

}