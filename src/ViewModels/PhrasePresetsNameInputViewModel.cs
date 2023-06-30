using System;
using FluentAvalonia.UI.Controls;

namespace TmCGPTD.ViewModels
{
    public class PhrasePresetsNameInputViewModel : ViewModelBase
    {
        private readonly ContentDialog dialog;

        public PhrasePresetsNameInputViewModel(ContentDialog dialog)
        {
            if (dialog is null)
            {
                throw new ArgumentNullException(nameof(dialog));
            }
            this.dialog = dialog;
        }

        private string? _UserInput;
        public string? UserInput
        {
            get => _UserInput;
            set
            {
                if (SetProperty(ref _UserInput, value))
                {
                    HandleUserInput();
                }
            }
        }

        private void HandleUserInput()
        {
            switch (UserInput)
            {
                case "OK":
                    dialog.Hide(ContentDialogResult.Primary);
                    break;

                case "Cancel":
                    dialog.Hide();
                    break;
            }
        }
    }
}