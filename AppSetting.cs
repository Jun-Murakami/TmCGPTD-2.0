using System;
using System.IO;

namespace TmCGPTD
{
    public class AppSettings
    {
        private static AppSettings _instance;

        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AppSettings();
                }

                return _instance;
            }
        }

        // アプリケーション設定
        public string AppDataPath { get; }
        public string DbPath { get; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsMaximized { get; set; }
        public bool EditorMode { get; set; }
        public double EditorFontSize { get; set; }
        public int SyntaxHighlighting { get; set; }
        public string PhrasePreset { get; set; }

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


        // DefaultSetting
        public AppSettings()
        {
            EditorMode = false;
            EditorFontSize = 15;
            SyntaxHighlighting = 0;
            PhrasePreset = "";

            AppDataPath = GetAppDataDirectory();
            DbPath = Path.Combine(AppDataPath, "log_database.db");

            ApiMaxTokens = 4000;
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
            MaxContentLength = 3072;

            ApiMaxTokensIsEnable = true;
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
            string appDataPath = Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TmCGPTD" );
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            return appDataPath;
        }
    }
}
