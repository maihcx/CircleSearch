using static CircleSearch.Resources.ThemeConfigs;

namespace CircleSearch.ViewModels.PagesBottom
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        private static ApplicationThemeManagerService ThemeManagerService = WindowHelper.ThemeManagerService;

        [ObservableProperty]
        private string _appVersion = String.Empty;

        #region Navigation panel auto hide
        [ObservableProperty]
        private bool _autoHideNavigationPanel = WindowHelper.IsAutoHideNavPanel;

        partial void OnAutoHideNavigationPanelChanged(bool oldValue, bool newValue)
        {
            WindowHelper.IsAutoHideNavPanel = AutoHideNavigationPanel = newValue;
        }
        #endregion

        #region Language list handle
        [ObservableProperty]
        private LanguageItem _selectedLanguage = LanguageBase.GetCurrentLanguageItem();

        [ObservableProperty]
        private ObservableCollection<LanguageItem> _languages = LanguageBase.GetLanguageItems();

        partial void OnSelectedLanguageChanged(LanguageItem value)
        {
            LanguageBase.SetLanguage(value.Code);
        }
        #endregion

        #region Theme list handle
        [ObservableProperty]
        private Models.ComboBoxItem _selectedTheme = ThemeManagerService.GetThemeCBBSelected();

        [ObservableProperty]
        private ObservableCollection<Models.ComboBoxItem> _themeList = ThemeManagerService.GetThemeCBBs();

        partial void OnSelectedThemeChanged(Models.ComboBoxItem value)
        {
            ThemeManagerService.SetApplicationTheme(Enum.Parse<IThemeType>(value.Value));
            ConfluxManager.cfsCircleSearchCore.Send("main-event", "OnThemeChanged");
        }
        #endregion

        #region Material list handle
        [ObservableProperty]
        private Models.ComboBoxItem _selectedMaterial = ThemeManagerService.GetMaterialCBBSelected();

        [ObservableProperty]
        private ObservableCollection<Models.ComboBoxItem> _materialList = ThemeManagerService.GetMaterialCBBs();

        partial void OnSelectedMaterialChanged(Models.ComboBoxItem value)
        {
            ThemeManagerService.SetBackdropType(Enum.Parse<WindowBackdropType>(value.Value));
            ThemeManagerService.SetApplicationTheme(Enum.Parse<IThemeType>(SelectedTheme.Value));
            ConfluxManager.cfsCircleSearchCore.Send("main-event", "OnMaterialChanged");
        }
        #endregion

        #region CornerRadius list handle
        [ObservableProperty]
        private int _sliderCornerRadius = ThemeManagerService.GlobalCornerRadius;

        partial void OnSliderCornerRadiusChanged(int oldValue, int newValue)
        {
            ThemeManagerService.GlobalCornerRadius = newValue;
            ConfluxManager.cfsCircleSearchCore.Send("main-event", "OnRadiusChanged");
        }
        #endregion

        #region StartWithWin
        [ObservableProperty]
        private bool _isStartWithWin = UserDataStore.GetValue<bool>("IsStartAtBoot");

        partial void OnIsStartWithWinChanged(bool oldValue, bool newValue)
        {
            StartupManager.SetStartWithWin(newValue);
        }
        #endregion

        #region IsViewAtBoot
        [ObservableProperty]
        private bool _isViewAtBoot = UserDataStore.GetValue<bool>("IsViewAtBoot");

        partial void OnIsViewAtBootChanged(bool oldValue, bool newValue)
        {
            UserDataStore.SetValue("IsViewAtBoot", newValue);
        }
        #endregion

        [ObservableProperty]
        private string _copyRight = AppInfoHelper.CopyRight;

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            AppVersion = $"CircleSearch - {GetAssemblyVersion()}";

            _isInitialized = true;
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }
    }
}
