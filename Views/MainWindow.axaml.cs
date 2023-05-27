using Avalonia;
using Avalonia.Controls;
using System.Text.Json;
using System.IO;
using System;
using TmCGPTD.ViewModels;
using FluentAvalonia.UI.Controls;
using Avalonia.Threading;
using System.Threading.Tasks;
using TmCGPTD.Models;
using Avalonia.Interactivity;

namespace TmCGPTD.Views
{
    public partial class MainWindow : Window
    {
        public MainWindowViewModel MainWindowViewModel { get; } = new MainWindowViewModel();
        DatabaseProcess _dbProcess = new DatabaseProcess();

        public MainWindow()
        {
            InitializeComponent();

            this.Closing += (sender, e) => SaveWindowSizeAndPosition();

            this.Loaded += MainWindow_Loaded;

            DataContext = MainWindowViewModel;
            VMLocator.MainWindowViewModel = MainWindowViewModel;
        }

        private double _previousWidth;

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = LoadAppSettings();

            if (File.Exists(Path.Combine(settings.AppDataPath, "settings.json")))
            {
                this.Width = settings.Width;
                this.Height = settings.Height;
                this.Position = new PixelPoint(settings.X, settings.Y);
                this.WindowState = settings.IsMaximized ? WindowState.Maximized : WindowState.Normal;
            }
            else
            {
                var screen = Screens.Primary;
                var workingArea = screen.WorkingArea;

                double dpiScaling = screen.PixelDensity;
                this.Width = (workingArea.Height / 5) * 4;
                this.Height = (workingArea.Height / 5) * 3;

                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            if (!File.Exists(AppSettings.Instance.DbPath))
            {
                _dbProcess.CreateDatabase();
            }

            await _dbProcess.DbLoadToMemoryAsync();
            await VMLocator.MainViewModel.LoadPhraseItemsAsync();

            VMLocator.MainViewModel.SelectedPhraseItem = AppSettings.Instance.PhrasePreset;

            VMLocator.MainViewModel.SelectedLogPain = "Chat List";

            await _dbProcess.GetEditorLogDatabaseAsync();
            await _dbProcess.GetTemplateItemsAsync();

            VMLocator.EditorViewModel.EditorCommonFontSize = settings.EditorFontSize > 0 ? settings.EditorFontSize : 1;
            VMLocator.MainViewModel.SelectedPhraseItem = settings.PhrasePreset;
            VMLocator.EditorViewModel.EditorModeIsChecked = true;
            
            VMLocator.MainWindowViewModel.ApiMaxTokens = settings.ApiMaxTokens;
            VMLocator.MainWindowViewModel.ApiTemperature = settings.ApiTemperature;
            VMLocator.MainWindowViewModel.ApiTopP = settings.ApiTopP;
            VMLocator.MainWindowViewModel.ApiN = settings.ApiN;
            VMLocator.MainWindowViewModel.ApiLogprobs = settings.ApiLogprobs;
            VMLocator.MainWindowViewModel.ApiPresencePenalty = settings.ApiPresencePenalty;
            VMLocator.MainWindowViewModel.ApiFrequencyPenalty = settings.ApiFrequencyPenalty;
            VMLocator.MainWindowViewModel.ApiBestOf = settings.ApiBestOf;
            VMLocator.MainWindowViewModel.ApiStop = settings.ApiStop;
            VMLocator.MainWindowViewModel.ApiLogitBias = settings.ApiLogitBias;
            VMLocator.MainWindowViewModel.ApiModel = settings.ApiModel;
            VMLocator.MainWindowViewModel.ApiUrl = settings.ApiUrl;
            VMLocator.MainWindowViewModel.ApiKey = settings.ApiKey;
            VMLocator.MainWindowViewModel.MaxContentLength = settings.MaxContentLength;

            VMLocator.MainWindowViewModel.ApiMaxTokensIsEnable = settings.ApiMaxTokensIsEnable;
            VMLocator.MainWindowViewModel.ApiTemperatureIsEnable = settings.ApiTemperatureIsEnable;
            VMLocator.MainWindowViewModel.ApiTopPIsEnable = settings.ApiTopPIsEnable;
            VMLocator.MainWindowViewModel.ApiNIsEnable = settings.ApiNIsEnable;
            VMLocator.MainWindowViewModel.ApiLogprobIsEnable = settings.ApiLogprobIsEnable;
            VMLocator.MainWindowViewModel.ApiPresencePenaltyIsEnable = settings.ApiPresencePenaltyIsEnable;
            VMLocator.MainWindowViewModel.ApiFrequencyPenaltyIsEnable = settings.ApiFrequencyPenaltyIsEnable;
            VMLocator.MainWindowViewModel.ApiBestOfIsEnable = settings.ApiBestOfIsEnable;
            VMLocator.MainWindowViewModel.ApiStopIsEnable = settings.ApiStopIsEnable;
            VMLocator.MainWindowViewModel.ApiLogitBiasIsEnable = settings.ApiLogitBiasIsEnable;
            VMLocator.MainWindowViewModel.MaxContentLengthIsEnable = settings.MaxContentLengthIsEnable;

            await Dispatcher.UIThread.InvokeAsync(() => { VMLocator.ChatViewModel.LogPainIsOpened = false; });
            if (this.Width > 1295)
            {
                await Task.Delay(1000);
                await Dispatcher.UIThread.InvokeAsync(() => { VMLocator.ChatViewModel.LogPainIsOpened = true; });
            }

            this.GetObservable(ClientSizeProperty).Subscribe(size => OnSizeChanged(size));
            _previousWidth = ClientSize.Width;

            VMLocator.DataGridViewModel.ChatList = await _dbProcess.SearchChatDatabaseAsync();
            VMLocator.EditorViewModel.EditorModeIsChecked = settings.EditorMode;
            VMLocator.EditorViewModel.SelectedLangIndex = settings.SyntaxHighlighting;

            await _dbProcess.CleanUpEditorLogDatabaseAsync();
            VMLocator.EditorViewModel.SelectedEditorLogIndex = -1;

            if (string.IsNullOrWhiteSpace(VMLocator.MainWindowViewModel.ApiKey))
            {
                var dialog = new ContentDialog() { Title = $"Please enter your API key.", PrimaryButtonText = "OK" };
                await ContentDialogShowAsync(dialog);
                VMLocator.ChatViewModel.OpenApiSettings();
            }
        }

        private void OnSizeChanged(Size newSize)
        {
            if (_previousWidth != newSize.Width)
            {
                if (newSize.Width <= 1295)
                {
                    VMLocator.ChatViewModel.LogPainIsOpened = false;
                    VMLocator.ChatViewModel.LogPainButtonIsVisible = false;
                }
                else
                {
                    if (VMLocator.ChatViewModel.LogPainButtonIsVisible == false)
                    {
                        VMLocator.ChatViewModel.LogPainButtonIsVisible = true;
                    }
                    if (newSize.Width > _previousWidth)
                    {
                        VMLocator.ChatViewModel.LogPainIsOpened = true;
                    }
                }
                _previousWidth = newSize.Width;
            }
        }

        private AppSettings LoadAppSettings()
        {
            var settings = AppSettings.Instance;

            if (File.Exists(Path.Combine(settings.AppDataPath, "settings.json")))
            {
                var jsonString = File.ReadAllText(Path.Combine(settings.AppDataPath, "settings.json"));
                settings = JsonSerializer.Deserialize<AppSettings>(jsonString);
            }
            else
            {
                settings = new AppSettings();
            }
            return settings;
        }

        private void SaveWindowSizeAndPosition()
        {
            var settings = AppSettings.Instance;
            settings.IsMaximized = this.WindowState == WindowState.Maximized;
            this.WindowStateÅ@= WindowState.Normal;
            settings.Width = this.Width;
            settings.Height = this.Height;
            settings.X = this.Position.X;
            settings.Y = this.Position.Y;

            settings.EditorMode = VMLocator.EditorViewModel.EditorModeIsChecked;
            settings.EditorFontSize = VMLocator.EditorViewModel.EditorCommonFontSize;
            settings.PhrasePreset = VMLocator.MainViewModel.SelectedPhraseItem;
            settings.SyntaxHighlighting = VMLocator.EditorViewModel.SelectedLangIndex;

            SaveAppSettings(settings);
        }

        private void SaveAppSettings(AppSettings settings)
        {
            var jsonString = JsonSerializer.Serialize(settings);
            File.WriteAllText(Path.Combine(settings.AppDataPath, "settings.json"), jsonString);
        }

        private async Task<ContentDialogResult> ContentDialogShowAsync(ContentDialog dialog)
        {
            VMLocator.ChatViewModel.ChatViewIsVisible = false;
            VMLocator.WebChatViewModel.WebChatViewIsVisible = false;
            var dialogResult = await dialog.ShowAsync();
            VMLocator.ChatViewModel.ChatViewIsVisible = true;
            VMLocator.WebChatViewModel.WebChatViewIsVisible = true;
            return dialogResult;
        }
    }
}