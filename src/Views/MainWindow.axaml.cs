using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System.Text.Json;
using System.IO;
using System;
using TmCGPTD.ViewModels;
using FluentAvalonia.UI.Controls;
using Avalonia.Threading;
using System.Threading.Tasks;
using TmCGPTD.Models;
using Avalonia.Interactivity;
using System.Globalization;
using Avalonia.Markup.Xaml.Styling;
using System.Linq;
using Avalonia.Markup.Xaml;
using System.Diagnostics;
using Avalonia.Input;
using Supabase.Gotrue.Interfaces;
using Supabase.Gotrue;
using static Supabase.Gotrue.Constants;
using System.Reflection;

namespace TmCGPTD.Views
{
    public partial class MainWindow : Window
    {
        public MainWindowViewModel MainWindowViewModel { get; } = new MainWindowViewModel();
        readonly DatabaseProcess _dbProcess = new();
        readonly SupabaseProcess _supabaseProcess = new();
        readonly SyncProcess _syncProcess = new();

        public MainWindow()
        {
            InitializeComponent();

            this.Closing += (sender, e) => SaveWindowSizeAndPosition();

            this.Loaded += MainWindow_Loaded;

            DataContext = MainWindowViewModel;
            VMLocator.MainWindowViewModel = MainWindowViewModel;

            // キーイベントハンドラ
            this.KeyDown += MainWindow_KeyDown;
            this.KeyUp += MainWindow_KeyUp;


            // Get the current culture info
            var cultureInfo = CultureInfo.CurrentCulture;
            if (cultureInfo.Name == "ja-JP")
            {
                Translate("ja-JP");
            }
        }

        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            {
                VMLocator.PhrasePresetsViewModel.AltKeyIsDown = true;
            }
            else if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                VMLocator.PhrasePresetsViewModel.CtrlKeyIsDown = true;
            }
        }

        private void MainWindow_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            {
                VMLocator.PhrasePresetsViewModel.AltKeyIsDown = false;
            }
            else if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                VMLocator.PhrasePresetsViewModel.CtrlKeyIsDown = false;
            }
        }

        private double _previousWidth;

        private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                var settings = await LoadAppSettingsAsync();

                if (File.Exists(Path.Combine(settings.AppDataPath, "settings.json")))
                {
                    this.Width = settings.Width - 1;
                    this.Position = new PixelPoint(settings.X, settings.Y);
                    this.Height = settings.Height;
                    this.Width = settings.Width;
                    this.WindowState = settings.IsMaximized ? WindowState.Maximized : WindowState.Normal;
                }
                else
                {
                    var screen = Screens.Primary;
                    var workingArea = screen!.WorkingArea;

                    double dpiScaling = screen.PixelDensity;
                    this.Width = 1300 * dpiScaling;
                    this.Height = 840 * dpiScaling;

                    this.Position = new PixelPoint(5, 0);
                }

                VMLocator.DatabaseSettingsViewModel.DatabasePath = settings.DbPath;

                if (!File.Exists(settings.DbPath))
                {
                    DatabaseProcess.CreateDatabase();
                }

                await _dbProcess.DbLoadToMemoryAsync();
                await VMLocator.MainViewModel.LoadPhraseItemsAsync();

                VMLocator.MainViewModel.SelectedPhraseItem = settings.PhrasePreset;

                VMLocator.MainViewModel.SelectedLogPain = "Chat List";

                VMLocator.MainViewModel.PhraseExpanderIsOpened = settings.PhraseExpanderMode;

                await _dbProcess.GetEditorLogDatabaseAsync();
                await _dbProcess.GetTemplateItemsAsync();

                VMLocator.EditorViewModel.EditorCommonFontSize = settings.EditorFontSize > 0 ? settings.EditorFontSize : 1;
                VMLocator.MainViewModel.SelectedPhraseItem = settings.PhrasePreset;
                VMLocator.EditorViewModel.EditorModeIsChecked = true;

                AppSettings.Instance.Email = settings.Email;
                AppSettings.Instance.Password = settings.Password;
                AppSettings.Instance.Provider = settings.Provider;
                AppSettings.Instance.Session = settings.Session;
                AppSettings.Instance.SyncIsOn = settings.SyncIsOn;
                VMLocator.CloudLoginViewModel.Email = settings.Email;
                VMLocator.CloudLoginViewModel.Password = settings.Password;
                VMLocator.CloudLoggedinViewModel.Provider = settings.Provider;

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

                VMLocator.EditorViewModel.EditorHeight1 = settings.EditorHeight1;
                VMLocator.EditorViewModel.EditorHeight2 = settings.EditorHeight2;
                VMLocator.EditorViewModel.EditorHeight3 = settings.EditorHeight3;
                VMLocator.EditorViewModel.EditorHeight4 = settings.EditorHeight4;
                VMLocator.EditorViewModel.EditorHeight5 = settings.EditorHeight5;

                await Dispatcher.UIThread.InvokeAsync(() => VMLocator.MainViewModel.LogPainIsOpened = false);
                if (this.Width > 1295)
                {
                    //await Task.Delay(1000);
                    await Dispatcher.UIThread.InvokeAsync(() => VMLocator.MainViewModel.LogPainIsOpened = true);
                }

                this.GetObservable(ClientSizeProperty).Subscribe(size => OnSizeChanged(size));
                _previousWidth = ClientSize.Width;

                await _dbProcess.UpdateChatLogDatabaseAsync();

                VMLocator.DataGridViewModel.ChatList = await _dbProcess.SearchChatDatabaseAsync();
                VMLocator.DataGridViewModel.SelectedItemIndex = -1;
                VMLocator.EditorViewModel.EditorModeIsChecked = settings.EditorMode;
                VMLocator.EditorViewModel.SelectedLangIndex = settings.SyntaxHighlighting;
                VMLocator.EditorViewModel.EditorSeparateMode = settings.SeparatorMode;

                VMLocator.EditorViewModel.SelectedEditorLogIndex = -1;

                if (string.IsNullOrWhiteSpace(VMLocator.MainWindowViewModel.ApiKey))
                {
                    var dialog = new ContentDialog() { Title = "Please enter your API key.", PrimaryButtonText = "OK" };
                    await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
                    VMLocator.ChatViewModel.OpenApiSettings();
                }

                await _supabaseProcess.InitializeSupabaseAsync();

                if (SupabaseStates.Instance.Supabase != null && settings.Session != null)
                {
                    SupabaseStates.Instance.Supabase.Auth.LoadSession();
                    for (int timeOut = 0; SupabaseStates.Instance.Supabase!.Auth.CurrentSession == null && timeOut < 10; timeOut++)
                    {
                        await Task.Delay(1000);
                    }

                    var session = await SupabaseStates.Instance.Supabase.Auth.RetrieveSessionAsync();
                    if (session == null && settings.Session != null)
                    {
                        //ログイン復帰再試行

                        var dialog = new ContentDialog() { Title = "Cloud sync session expired. Please sign in again.", PrimaryButtonText = "OK" };
                        await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);

                        VMLocator.MainViewModel.LoginStatus = 1;
                        VMLocator.CloudLoggedinViewModel.Provider = "";
                        AppSettings.Instance.SyncIsOn = false;
                    }
                    else
                    {
                        VMLocator.CloudLoggedinViewModel.Provider = settings.Provider;
                        await _dbProcess.CleanUpEditorLogDatabaseAsync();
                        await _syncProcess.SyncDbAsync();
                        await _supabaseProcess.SubscribeAsync();
                    }
                }
                else
                {
                    await _dbProcess.CleanUpEditorLogDatabaseAsync();
                }
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog() { Title = $"Error", Content = ex.Message, PrimaryButtonText = "OK" };
                await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
            }
        }

        private void OnSizeChanged(Size newSize)
        {
            if (_previousWidth != newSize.Width)
            {
                if (newSize.Width <= 1295)
                {
                    VMLocator.MainViewModel.LogPainIsOpened = false;
                    VMLocator.MainViewModel.LogPainButtonIsVisible = false;
                }
                else
                {
                    if (!VMLocator.MainViewModel.LogPainButtonIsVisible)
                    {
                        VMLocator.MainViewModel.LogPainButtonIsVisible = true;
                    }
                    if (newSize.Width > _previousWidth)
                    {
                        VMLocator.MainViewModel.LogPainIsOpened = true;
                    }
                }
                _previousWidth = newSize.Width;
            }
        }

        private async Task<AppSettings> LoadAppSettingsAsync()
        {
            var settings = AppSettings.Instance;

            if (File.Exists(Path.Combine(settings.AppDataPath, "settings.json")))
            {
                try
                {
                    var options = new JsonSerializerOptions();
                    options.Converters.Add(new GridLengthConverter());

                    var jsonString = File.ReadAllText(Path.Combine(settings.AppDataPath, "settings.json"));
                    settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(jsonString, options);
                }
                catch (Exception)
                {
                    var dialog = new ContentDialog() { Title = $"Invalid setting file. Reset to default values.", PrimaryButtonText = "OK" };
                    await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
                    File.Delete(Path.Combine(settings!.AppDataPath, "settings.json"));
                }
            }

            //Aes暗号化・複合化キーを下記の形式でappsettings.json設定ファイルに保存し,プロジェクトのルートに置いてAvaloniaResourceとしてビルドしてください。
            //aes.GenerateKey();、 aes.GenerateIV();メソッドを呼び出すことでC#上で生成することもできます。
            //{
            //  "Key": "Aes 256-bit Key",
            //  "Iv": "IV"
            //}

            using var streamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/appsettings.json")));
            string aesJson = await streamReader.ReadToEndAsync();

            SupabaseStates.Instance.AesSettings = System.Text.Json.JsonSerializer.Deserialize<AesSettings>(aesJson)!;
            var aesKey = SupabaseStates.Instance.AesSettings!.Key;
            var aesIv = SupabaseStates.Instance.AesSettings.Iv;

            if (!string.IsNullOrWhiteSpace(settings!.Email)) settings.Email = AesEncryption.Decrypt(new AesSettings { Text = settings.Email, Key = aesKey, Iv = aesIv });
            if (!string.IsNullOrWhiteSpace(settings.Password)) settings.Password = AesEncryption.Decrypt(new AesSettings { Text = settings.Password, Key = aesKey, Iv = aesIv });

            return settings!;
        }

        public void SaveWindowSizeAndPosition()
        {
            var settings = AppSettings.Instance;
            settings.IsMaximized = this.WindowState == WindowState.Maximized;
            this.WindowState = WindowState.Normal;
            settings.Width = this.Width;
            settings.Height = this.Height;
            settings.X = this.Position.X;
            settings.Y = this.Position.Y;

            settings.EditorMode = VMLocator.EditorViewModel.EditorModeIsChecked;
            settings.EditorFontSize = VMLocator.EditorViewModel.EditorCommonFontSize;
            settings.PhrasePreset = VMLocator.MainViewModel.SelectedPhraseItem!;
            settings.SyntaxHighlighting = VMLocator.EditorViewModel.SelectedLangIndex;
            settings.PhraseExpanderMode = VMLocator.MainViewModel.PhraseExpanderIsOpened;

            settings.EditorHeight1 = VMLocator.EditorViewModel.EditorHeight1;
            settings.EditorHeight2 = VMLocator.EditorViewModel.EditorHeight2;
            settings.EditorHeight3 = VMLocator.EditorViewModel.EditorHeight3;
            settings.EditorHeight4 = VMLocator.EditorViewModel.EditorHeight4;
            settings.EditorHeight5 = VMLocator.EditorViewModel.EditorHeight5;

            settings.SeparatorMode = VMLocator.EditorViewModel.EditorSeparateMode;

            if (SupabaseStates.Instance.Supabase?.Auth.CurrentSession != null)
            {
                settings.Session = System.Text.Json.JsonSerializer.Serialize(SupabaseStates.Instance.Supabase.Auth.CurrentSession);
                settings.Provider = VMLocator.CloudLoggedinViewModel.Provider;
            }
            else
            {
                settings.Session = null;
                settings.Provider = null;
            }

            settings.Email = VMLocator.CloudLoginViewModel.Email;
            settings.Password = VMLocator.CloudLoginViewModel.Password;
            var aesKey = SupabaseStates.Instance.AesSettings!.Key;
            var aesIv = SupabaseStates.Instance.AesSettings.Iv;

            if (!string.IsNullOrWhiteSpace(settings.Email)) settings.Email = AesEncryption.Encrypt(new AesSettings { Text = settings.Email, Key = aesKey, Iv = aesIv });
            if (!string.IsNullOrWhiteSpace(settings.Password)) settings.Password = AesEncryption.Encrypt(new AesSettings { Text = settings.Password, Key = aesKey, Iv = aesIv });

            SaveAppSettings(settings);
        }

        private void SaveAppSettings(AppSettings settings)
        {
            var jsonString = System.Text.Json.JsonSerializer.Serialize(settings);
            File.WriteAllText(Path.Combine(settings.AppDataPath, "settings.json"), jsonString);
        }

        public void Translate(string targetLanguage)
        {
            var translations = App.Current!.Resources.MergedDictionaries.OfType<ResourceInclude>().FirstOrDefault(x => x.Source?.OriginalString?.Contains("/Lang/") ?? false);

            if (translations != null)
                App.Current.Resources.MergedDictionaries.Remove(translations);

            App.Current.Resources.MergedDictionaries.Add(
                (ResourceDictionary)AvaloniaXamlLoader.Load(
                    new Uri($"avares://TmCGPTD/Assets/Lang/{targetLanguage}.axaml")
                    )
                );
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}