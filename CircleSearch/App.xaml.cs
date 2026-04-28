 namespace CircleSearch
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static LauncherSettings Settings { get; private set; } = null!;
        public static OverlayLauncherService LauncherService { get; private set; } = null!;

        private string logFile = Path.Combine(AppInfoHelper.GetAppPath(), "crash.log");

        private bool _isViewAtBoot;

        public App()
        {
            RenderOptions.ProcessRenderMode = RenderMode.Default;

            Bootstrap.OnBeforeStartup();
            _isViewAtBoot = Bootstrap.IsViewAtBoot;

            TranslationSource.Instance.CurrentCulture = LanguageBase.GetSetupLanguage();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        public void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            File.AppendAllText(logFile, $"[{DateTime.Now}] UnhandledException: {ex}\n");
        }

        public void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            File.AppendAllText(logFile, $"[{DateTime.Now}] UnobservedTaskException: {e.Exception}\n");
            e.SetObserved();
        }

        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory)); })
            .ConfigureServices((context, services) =>
            {
                services.AddNavigationViewPageProvider();

                services.AddHostedService<ApplicationHostService>();

                // Theme manipulation
                services.AddSingleton<IThemeService, ThemeService>();

                // TaskBar manipulation
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // Service containing navigation, same as INavigationWindow... but without window
                services.AddSingleton<INavigationService, NavigationService>();

                // Main window with navigation
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                NavigationHandle.SetupPageViewModelPairs(services, "CircleSearch.Views.Pages", "CircleSearch.ViewModels.Pages");
                NavigationHandle.SetupPageViewModelPairs(services, "CircleSearch.Views.PagesBottom", "CircleSearch.ViewModels.PagesBottom");
                NavigationHandle.SetupPageViewModelPairs(services, "CircleSearch.Views.Pages.SystemConfigPages", "CircleSearch.ViewModels.Pages.SystemConfigViewModels");
            }).Build();

        /// <summary>
        /// Gets services.
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            if (_isViewAtBoot)
            {
                _host.StartAsync();
            }

            Bootstrap.OnStartup();
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            if (_isViewAtBoot)
            {
                _host.StopAsync().Wait();
            }

            Bootstrap.OnExit();

            if (_isViewAtBoot)
            {
                _host.Dispose();
            }
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }

        /// <summary>
        /// Gets registered service.
        /// </summary>
        /// <typeparam name="T">Type of the service to get.</typeparam>
        /// <returns>Instance of the service or <see langword="null"/>.</returns>
        public static T GetRequiredService<T>()
            where T : class
        {
            return _host.Services.GetRequiredService<T>();
        }
    }
}
