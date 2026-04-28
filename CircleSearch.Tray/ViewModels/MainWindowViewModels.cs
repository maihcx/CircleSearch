namespace CircleSearch.Tray.ViewModels
{
    public partial class MainWindowViewModels : ObservableObject
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _applicationTitle = "CircleSearch";

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems;

        private ConfluxService MainService;
        private ConfluxService CoreService;

        public MainWindowViewModels()
        {
            if (!_isInitialized)
            {
                InitializeViewModel();
            }
        }

        private void InitializeViewModel()
        {
            _isInitialized = true;

            createTrayIcons();

            CoreService = new ConfluxService();
            CoreService.CreateNoWindow = true;
            CoreService.Register("CircleSearch.Core.exe", "CircleSearch.TrayToCore", "CircleSearch.CoreToTray");

            CoreService.OnMessageReceived += (name, value) =>
            {
                App.Current.Dispatcher.Invoke(() => {
                    if (name == "main-event")
                    {
                        switch (value)
                        {
                            case "OnLanguageChanged":
                                UserDataStore.Reload();
                                TranslationSource.Instance.CurrentCulture = LanguageBase.GetSetupLanguage();
                                createTrayIcons();
                                break;

                            case "OnRadiusChanged":
                                UserDataStore.Reload();
                                Application.Current.Resources["ControlCornerRadius"] = new CornerRadius(UserDataStore.GetValue<int>("ObjectCornerRadius"));
                                break;

                            case "OnMaterialChanged":
                                UserDataStore.Reload();
                                AppRuntime.ThemeManagerService.SetBackdropType(Enum.Parse<WindowBackdropType>(AppRuntime.ThemeManagerService.GetMaterialCBBSelected().Value));
                                AppRuntime.ThemeManagerService.SetApplicationTheme(Enum.Parse<ThemeConfigs.IThemeType>(AppRuntime.ThemeManagerService.GetThemeCBBSelected().Value));
                                break;

                            case "OnThemeChanged":
                                UserDataStore.Reload();
                                AppRuntime.ThemeManagerService.SetApplicationTheme(Enum.Parse<ThemeConfigs.IThemeType>(AppRuntime.ThemeManagerService.GetThemeCBBSelected().Value));
                                break;
                        }
                    }
                });
            };

            _ = CoreService.StartServiceAsync();
            AppRuntime.CoreService = CoreService;
        }

        [RelayCommand]
        public void OnTrayExecute(string? tag)
        {
            switch (tag)
            {
                case "tray_open":
                    CoreService.StartApp();
                    CoreService.Send("state", "start");
                    break;
                case "tray_home":
                    CoreService.StartApp();
                    CoreService.Send("tray-event", "OnGoHome");
                    break;
                case "tray_settings":
                    CoreService.StartApp();
                    CoreService.Send("tray-event", "OnGoSettings");
                    break;
                case "tray_close":
                    Application.Current.Shutdown();
                    break;
            }
        }

        private void createTrayIcons()
        {
            TrayMenuItems = new ObservableCollection<MenuItem>
            {
                new() {
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Open24 },
                    Header = LocalizationHelper.GetLang("open_title"), Tag = "tray_open",
                    Command = TrayExecuteCommand, CommandParameter = "tray_open" },
                new() {
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                    Header = LocalizationHelper.GetLang("page_home_title"), Tag = "tray_home",
                    Command = TrayExecuteCommand, CommandParameter = "tray_home" },
                new() {
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                    Header = LocalizationHelper.GetLang("page_settings_title"), Tag = "tray_settings",
                    Command = TrayExecuteCommand, CommandParameter = "tray_settings" },
                new() {
                    Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowExit20 },
                    Header = LocalizationHelper.GetLang("exit_title"), Tag = "tray_close",
                    Command = TrayExecuteCommand, CommandParameter = "tray_close" }
            };
        }
    }
}
