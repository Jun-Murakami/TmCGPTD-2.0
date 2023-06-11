using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace TmCGPTD.ViewModels
{
    public partial class MainWindowViewModel: ViewModelBase
    {
        public MainWindowViewModel()
        {
            ApiSettingIsOpened = false;
            ClosingApiSettingsCommand = new RelayCommand(ClosingApiSettings);
            ResetApiSettingsCommand = new RelayCommand(ResetApiSettings);
            ValidateTextInputCommand = new RelayCommand<string>(ValidateTextInput);

            PhrasePresetsViewModel _phrasePresetsVM = VMLocator.PhrasePresetsViewModel;

            CtrlKey1Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 1);
            CtrlKey2Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 2);
            CtrlKey3Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 3);
            CtrlKey4Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 4);
            CtrlKey5Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 5);
            CtrlKey6Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 6);
            CtrlKey7Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 7);
            CtrlKey8Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 8);
            CtrlKey9Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 9);
            CtrlKey0Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 10);

            AltKey1Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 11);
            AltKey2Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 12);
            AltKey3Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 13);
            AltKey4Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 14);
            AltKey5Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 15);
            AltKey6Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 16);
            AltKey7Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 17);
            AltKey8Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 18);
            AltKey9Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 19);
            AltKey0Command = new RelayCommand(() => _phrasePresetsVM.KeyDownNum = 20);
        }

        public ICommand CtrlKey1Command { get; }
        public ICommand CtrlKey2Command { get; }
        public ICommand CtrlKey3Command { get; }
        public ICommand CtrlKey4Command { get; }
        public ICommand CtrlKey5Command { get; }
        public ICommand CtrlKey6Command { get; }
        public ICommand CtrlKey7Command { get; }
        public ICommand CtrlKey8Command { get; }
        public ICommand CtrlKey9Command { get; }
        public ICommand CtrlKey0Command { get; }

        public ICommand AltKey1Command { get; }
        public ICommand AltKey2Command { get; }
        public ICommand AltKey3Command { get; }
        public ICommand AltKey4Command { get; }
        public ICommand AltKey5Command { get; }
        public ICommand AltKey6Command { get; }
        public ICommand AltKey7Command { get; }
        public ICommand AltKey8Command { get; }
        public ICommand AltKey9Command { get; }
        public ICommand AltKey0Command { get; }

        public ICommand ValidateTextInputCommand { get; }
        public ICommand ClosingApiSettingsCommand { get; }
        public ICommand ResetApiSettingsCommand { get; }


        private void ClosingApiSettings()
        {
            ApiSettingIsOpened = false;
            VMLocator.ChatViewModel.ChatViewIsVisible = true;
            VMLocator.WebChatViewModel.WebChatViewIsVisible = true;
            VMLocator.WebChatBardViewModel.WebChatBardViewIsVisible = true;
        }

        private void ValidateTextInput(string text)
        {
            Regex regex = new Regex("[^0-9.-]+");
            if (regex.IsMatch(text))
            {
                return;
            }
        }

        private bool _apiSettingIsOpened;
        public bool ApiSettingIsOpened
        {
            get => _apiSettingIsOpened;
            set => SetProperty(ref _apiSettingIsOpened, value);
        }

        private void ResetApiSettings()
        {
            ApiMaxTokens = 2048;
            ApiTemperature = 1;
            ApiTopP = 1.0;
            ApiN = 1;
            ApiLogprobs = 1;
            ApiPresencePenalty = 0.0;
            ApiFrequencyPenalty = 0.0;
            ApiBestOf = 1;
            ApiStop = "";
            ApiLogitBias = "";
            ApiModel = "gpt-3.5-turbo";
            ApiUrl = "https://api.openai.com/v1/chat/completions";
            //ApiKey = "";
            MaxContentLength = 3072;

            ApiMaxTokensIsEnable = false;
            ApiTemperatureIsEnable = false;
            ApiTopPIsEnable = false;
            ApiNIsEnable = false;
            ApiLogprobIsEnable = false;
            ApiPresencePenaltyIsEnable = false;
            ApiFrequencyPenaltyIsEnable = false;
            ApiBestOfIsEnable = false;
            ApiStopIsEnable = false;
            ApiLogitBiasIsEnable = false;
            MaxContentLengthIsEnable = false;
        }

        public List<string> ModelList { get; } = new List<string>
        {
            "gpt-4",
            "gpt-4-0314",
            "gpt-4-32k",
            "gpt-4-32k-0314",
            "gpt-3.5-turbo",
            "gpt-3.5-turbo-0301"
        };

        private AppSettings _appSettings => AppSettings.Instance;

        public int ApiMaxTokens
        {
            get => _appSettings.ApiMaxTokens;
            set
            {
                if (_appSettings.ApiMaxTokens != value)
                {
                    _appSettings.ApiMaxTokens = value;
                    OnPropertyChanged();
                }
            }
        }

        public double ApiTemperature
        {
            get => _appSettings.ApiTemperature;
            set
            {
                if (_appSettings.ApiTemperature != value)
                {
                    _appSettings.ApiTemperature = value;
                    OnPropertyChanged();
                }
            }
        }

        public double ApiTopP
        {
            get => _appSettings.ApiTopP;
            set
            {
                if (_appSettings.ApiTopP != value)
                {
                    _appSettings.ApiTopP = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ApiN
        {
            get => _appSettings.ApiN;
            set
            {
                if (_appSettings.ApiN != value)
                {
                    _appSettings.ApiN = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ApiLogprobs
        {
            get => _appSettings.ApiLogprobs;
            set
            {
                if (_appSettings.ApiLogprobs != value)
                {
                    _appSettings.ApiLogprobs = value;
                    OnPropertyChanged();
                }
            }
        }

        public double ApiPresencePenalty
        {
            get => _appSettings.ApiPresencePenalty;
            set
            {
                if (_appSettings.ApiPresencePenalty != value)
                {
                    _appSettings.ApiPresencePenalty = value;
                    OnPropertyChanged();
                }
            }
        }

        public double ApiFrequencyPenalty
        {
            get => _appSettings.ApiFrequencyPenalty;
            set
            {
                if (_appSettings.ApiFrequencyPenalty != value)
                {
                    _appSettings.ApiFrequencyPenalty = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ApiBestOf
        {
            get => _appSettings.ApiBestOf;
            set
            {
                if (_appSettings.ApiBestOf != value)
                {
                    _appSettings.ApiBestOf = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ApiStop
        {
            get => _appSettings.ApiStop;
            set
            {
                if (_appSettings.ApiStop != value)
                {
                    _appSettings.ApiStop = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ApiLogitBias
        {
            get => _appSettings.ApiLogitBias;
            set
            {
                if (_appSettings.ApiLogitBias != value)
                {
                    _appSettings.ApiLogitBias = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ApiModel
        {
            get => _appSettings.ApiModel;
            set
            {
                if (_appSettings.ApiModel != value)
                {
                    _appSettings.ApiModel = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ApiUrl
        {
            get => _appSettings.ApiUrl;
            set
            {
                if (_appSettings.ApiUrl != value)
                {
                    _appSettings.ApiUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ApiKey
        {
            get => _appSettings.ApiKey;
            set
            {
                if (_appSettings.ApiKey != value)
                {
                    _appSettings.ApiKey = value;
                    OnPropertyChanged();
                }
            }
        }

        public int MaxContentLength
        {
            get => _appSettings.MaxContentLength;
            set
            {
                if (_appSettings.MaxContentLength != value)
                {
                    _appSettings.MaxContentLength = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ApiMaxTokensIsEnable
        {
            get => _appSettings.ApiMaxTokensIsEnable;
            set
            {
                if (_appSettings.ApiMaxTokensIsEnable != value)
                {
                    _appSettings.ApiMaxTokensIsEnable = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ApiTemperatureIsEnable
        {
            get => _appSettings.ApiTemperatureIsEnable;
            set
            {
                if (_appSettings.ApiTemperatureIsEnable != value)
                {
                    _appSettings.ApiTemperatureIsEnable = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ApiTopPIsEnable
        {
            get => _appSettings.ApiTopPIsEnable;
            set
            {
                if (_appSettings.ApiTopPIsEnable != value)
                {
                    _appSettings.ApiTopPIsEnable = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ApiNIsEnable
        {
            get => _appSettings.ApiNIsEnable;
            set
            {
                if (_appSettings.ApiNIsEnable != value)
                {
                    _appSettings.ApiNIsEnable = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool ApiLogprobIsEnable
        {
            get => _appSettings.ApiLogprobIsEnable;
            set
            {
                if (_appSettings.ApiLogprobIsEnable != value)
                {
                    _appSettings.ApiLogprobIsEnable = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool ApiPresencePenaltyIsEnable
        {
            get => _appSettings.ApiPresencePenaltyIsEnable;
            set
            {
                if (_appSettings.ApiPresencePenaltyIsEnable != value)
                {
                    _appSettings.ApiPresencePenaltyIsEnable = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool ApiFrequencyPenaltyIsEnable
        {
            get => _appSettings.ApiFrequencyPenaltyIsEnable;
            set
            {
                if (_appSettings.ApiFrequencyPenaltyIsEnable != value)
                {
                    _appSettings.ApiFrequencyPenaltyIsEnable = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool ApiBestOfIsEnable
        {
            get => _appSettings.ApiBestOfIsEnable;
            set
            {
                if (_appSettings.ApiBestOfIsEnable != value)
                {
                    _appSettings.ApiBestOfIsEnable = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool ApiStopIsEnable
        {
            get => _appSettings.ApiStopIsEnable;
            set
            {
                if (_appSettings.ApiStopIsEnable != value)
                {
                    _appSettings.ApiStopIsEnable = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool ApiLogitBiasIsEnable
        {
            get => _appSettings.ApiLogitBiasIsEnable;
            set
            {
                if (_appSettings.ApiLogitBiasIsEnable != value)
                {
                    _appSettings.ApiLogitBiasIsEnable = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool MaxContentLengthIsEnable
        {
            get => _appSettings.MaxContentLengthIsEnable;
            set
            {
                if (_appSettings.MaxContentLengthIsEnable != value)
                {
                    _appSettings.MaxContentLengthIsEnable = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}