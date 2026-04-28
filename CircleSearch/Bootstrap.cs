namespace CircleSearch
{
    public static class Bootstrap
    {
        private static Thread? _pipeThread;
        private static Mutex? _mutex;
        private static string UniqueAppId = @"Global\CircleSearch.SingleInstance.App";
        private static bool _isPrimaryInstance = false;
        private static SplashScreen SplashScreen;

        public static bool IsViewAtBoot { get; set; }
        public static bool IsEndService { get; set; }

        public static void OnBeforeStartup()
        {
            SharedMem.AppSettings = LauncherSettings.Load();

            SharedMem.Launcher = new OverlayLauncherService();

            #region Mutex checker
            try
            {
                _mutex = CreateMutexWithSecurity(UniqueAppId);
                _isPrimaryInstance = _mutex.WaitOne(0, false);
            }
            catch
            {
                _mutex = new Mutex(true, UniqueAppId, out _isPrimaryInstance);
            }

            if (!_isPrimaryInstance)
            {
                try
                {
                    using var client = new NamedPipeClientStream(".", UniqueAppId, PipeDirection.Out);
                    client.Connect(1000);
                    using var writer = new StreamWriter(client) { AutoFlush = true };
                    writer.WriteLine("SHOW");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to connect to pipe: {ex.Message}");
                }

                Environment.Exit(0);
                return;
            }
            #endregion

            #region SplashScreen Show
            SplashScreen = new SplashScreen("Assets/app-256.png");
            SplashScreen.Show(false, true);
            #endregion

            #region Core initialization
            ConfluxService cfsCircleSearchCore = new();
            cfsCircleSearchCore.CreateNoWindow = true;
            cfsCircleSearchCore.Register("CircleSearch.Core.exe", "CircleSearch.MainToCore", "CircleSearch.CoreToMain");

            IsViewAtBoot = cfsCircleSearchCore.IsAppStarted();

            if (UserDataStore.GetValue<bool>("IsViewAtBoot"))
            {
                IsViewAtBoot = true;
            }

            cfsCircleSearchCore.StartApp();
            _ = cfsCircleSearchCore.StartServiceAsync();

            ConfluxManager.cfsCircleSearchCore = cfsCircleSearchCore;

            cfsCircleSearchCore.OnMessageReceived += (name, value) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (name == "state")
                    {
                        switch (value)
                        {
                            case "start":
                                WindowHelper.FocusMainWindow();
                                break;
                            case "shutdown":
                                IsEndService = true;
                                Application.Current.Shutdown();
                                break;
                        }
                    }
                    else if (name == "tray-event")
                    {
                        WindowHelper.FocusMainWindow();
                        switch (value)
                        {
                            case "OnGoHome":
                                NavigationHandle.NavigationService.Navigate(typeof(HomePage));
                                break;

                            case "OnGoConfig":
                                NavigationHandle.NavigationService.Navigate(typeof(ConfigPage));
                                break;

                            case "OnGoSettings":
                                NavigationHandle.NavigationService.Navigate(typeof(SettingsPage));
                                break;
                        }
                    }
                });
            };
            #endregion

            #region Single App Reader
            _pipeThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        var pipeSecurity = new PipeSecurity();
                        pipeSecurity.AddAccessRule(new PipeAccessRule(
                            new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                            PipeAccessRights.ReadWrite,
                            AccessControlType.Allow
                        ));

                        using var server = NamedPipeServerStreamAcl.Create(
                            UniqueAppId,
                            PipeDirection.In,
                            1,
                            PipeTransmissionMode.Byte,
                            PipeOptions.None,
                            0,
                            0,
                            pipeSecurity
                        );

                        server.WaitForConnection();

                        using var reader = new StreamReader(server);
                        string? line = reader.ReadLine();

                        if (line == "SHOW")
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                var mainWindow = Application.Current?.MainWindow;
                                if (mainWindow != null)
                                {
                                    WindowHelper.FocusMainWindow();
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Thread.Sleep(100);
                    }
                }
            });
            _pipeThread.IsBackground = true;
            _pipeThread.Start();
            #endregion
        }

        public static void OnStartup()
        {
            StartupManager.RefreshStartWithWin();

            SharedMem.Launcher.SendHotkey(SharedMem.AppSettings);
            SharedMem.Launcher.SendConfig(SharedMem.AppSettings);

            SplashScreen.Close(new TimeSpan(0, 0, 0, 0, 0));

            #region Handle for VAB Off
            if (!IsViewAtBoot)
            {
                App.Current.Shutdown();
            }
            #endregion
        }

        public static void OnExit()
        {
            if (_mutex != null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch { }
                _mutex.Dispose();
            }
        }

        /// <summary>
        /// Create Mutex for all users with full control permission.
        /// </summary>
        private static Mutex CreateMutexWithSecurity(string name)
        {
            var allowEveryoneRule = new MutexAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                MutexRights.FullControl,
                AccessControlType.Allow
            );

            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);

            bool createdNew;

            var mutex = new Mutex(false, name, out createdNew);


            if (createdNew)
            {
                mutex.SetAccessControl(securitySettings);
            }

            return mutex;
        }
    }
}
