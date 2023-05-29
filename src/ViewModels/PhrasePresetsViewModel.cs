using System.Collections.ObjectModel;
using System.Linq;

namespace TmCGPTD.ViewModels
{
    public class PhrasePresetsViewModel : ViewModelBase
    {
        public PhrasePresetsViewModel()
        {
            Phrases = new ObservableCollection<string>(Enumerable.Repeat("", 20));
        }

        private ObservableCollection<string> _phrases;
        public ObservableCollection<string> Phrases
        {
            get => _phrases;
            set => SetProperty(ref _phrases, value);
        }
    }

}