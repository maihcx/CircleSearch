namespace CircleSearch.Tray.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigableView<MainWindowViewModels>
    {
        public MainWindowViewModels ViewModel { get; }
        public ApplicationThemeManagerService ThemeManagerService { get; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainWindowViewModels();
            DataContext = this;
            AppRuntime.MainWindow = this;

            ThemeManagerService = new ApplicationThemeManagerService(this);
            AppRuntime.ThemeManagerService = ThemeManagerService;
            ThemeManagerService.InitCornerRadius();
            ThemeManagerService.Watch();

            TrayIcon.BalloonTipClick += TrayIcon_BalloonTipClick;
        }

        private void TrayIcon_BalloonTipClick([System.Diagnostics.CodeAnalysis.NotNull] Wpf.Ui.Tray.Controls.NotifyIcon sender, RoutedEventArgs e)
        {
            AppRuntime.CoreService?.Send("tray-event", "OnGoSettings--UPDATE");
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            ApplicationThemeManager.Apply(ThemeManagerService.GetSysApplicationTheme(), ThemeManagerService.GetBackdropType(), true);

            this.Hide();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void NotifyIcon_LeftClick(Wpf.Ui.Tray.Controls.NotifyIcon sender, RoutedEventArgs e)
        {
            AppRuntime.CoreService?.StartApp();
            AppRuntime.CoreService?.Send("state", "start");
        }

        public void ShowUpdateBalloon(string version)
        {
            string title = LocalizationHelper.GetLang("update_available_title");
            string body = $"CircleSearch {version} {LocalizationHelper.GetLang("update_balloon_body")}";

            TrayIcon.ShowBalloonTip(5000, title, body, Wpf.Ui.Tray.Controls.ToolTipIcon.Warning);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (AppRuntime.CoreService!.IsAppStarted())
            {
                AppRuntime.CoreService.Send("core-svc-state", "shutdown");
            }
            base.OnClosing(e);
        }
    }
}
