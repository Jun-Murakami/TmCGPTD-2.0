using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;

namespace TmCGPTD
{
    public class AppSettings: ObservableObject
    {
        private static AppSettings? _instance;

        public static AppSettings Instance
        {
            get
            {
                _instance ??= new AppSettings();

                return _instance;
            }
        }

        // アプリケーション設定
        public string AppDataPath { get; }
        public string DbPath { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsMaximized { get; set; }
        public bool EditorMode { get; set; }
        public double EditorFontSize { get; set; }
        public int SyntaxHighlighting { get; set; }
        public string PhrasePreset { get; set; }
        public bool PhraseExpanderMode { get; set; }
        public GridLength EditorHeight1 { get; set; }
        public GridLength EditorHeight2 { get; set; }
        public GridLength EditorHeight3 { get; set; }
        public GridLength EditorHeight4 { get; set; }
        public GridLength EditorHeight5 { get; set; }
        public int SeparatorMode { get; set; }
        public string? Session { get; set; }
        public string? Language { get; set; }

        private bool _syncIsOn;

        public bool SyncIsOn
        {
            get => _syncIsOn;
            set
            { 
                if(SetProperty(ref _syncIsOn, value))
                {
                    VMLocator.MainViewModel.CloudIconSelector = value;
                }
            }
        }
        public string? Provider { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }

        // ChatGPT API接続設定
        public int ApiMaxTokens { get; set; }
        public double ApiTemperature { get; set; }
        public double ApiTopP { get; set; }
        public int ApiN { get; set; }
        public int ApiLogprobs { get; set; }
        public double ApiPresencePenalty { get; set; }
        public double ApiFrequencyPenalty { get; set; }
        public int ApiBestOf { get; set; }
        public string ApiStop { get; set; }
        public string ApiLogitBias { get; set; }
        public string ApiModel { get; set; }
        public string ApiUrl { get; set; }
        public string ApiKey { get; set; }
        public int MaxContentLength { get; set; }

        public bool ApiMaxTokensIsEnable { get; set; }
        public bool ApiTemperatureIsEnable { get; set; }
        public bool ApiTopPIsEnable { get; set; }
        public bool ApiNIsEnable { get; set; }
        public bool ApiLogprobIsEnable { get; set; }
        public bool ApiPresencePenaltyIsEnable { get; set; }
        public bool ApiFrequencyPenaltyIsEnable { get; set; }
        public bool ApiBestOfIsEnable { get; set; }
        public bool ApiStopIsEnable { get; set; }
        public bool ApiLogitBiasIsEnable { get; set; }
        public bool MaxContentLengthIsEnable { get; set; }
        public bool IsAutoGenerateChatTitle { get; set; }


        // DefaultSetting
        public AppSettings()
        {
            EditorMode = false;
            EditorFontSize = 15;
            SyntaxHighlighting = 0;
            PhrasePreset = "";
            PhraseExpanderMode = true;
            SeparatorMode = 5;
            Session = null;
            SyncIsOn = false;
            Provider = "";
            Email = "";
            Password = "";
            Language = "English";
            IsAutoGenerateChatTitle = true;

            EditorHeight1 = new GridLength(0.21, GridUnitType.Star);
            EditorHeight2 = new GridLength(0.30, GridUnitType.Star);
            EditorHeight3 = new GridLength(0.17, GridUnitType.Star);
            EditorHeight4 = new GridLength(0.24, GridUnitType.Star);
            EditorHeight5 = new GridLength(0.08, GridUnitType.Star);

            AppDataPath = GetAppDataDirectory();
            DbPath = Path.Combine(AppDataPath, "log_database.db");

            ApiMaxTokens = 12800;
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
            ApiKey = "";
            MaxContentLength = 12000;

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

        private string GetAppDataDirectory()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TmCGPTD");
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            return appDataPath;
        }
    }
}
