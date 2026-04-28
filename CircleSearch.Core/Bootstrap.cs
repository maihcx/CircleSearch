namespace CircleSearch.Core
{
    public class Bootstrap
    {
        public void OnStarted()
        {
            AppRuntime.hotkeyService = new GlobalHotkeyService();

            AppRuntime.hotkeyService.HotkeyPressed += () =>
            {
                AppRuntime.StartOverlay();
            };

            #region Initialize ConfluxService for Main Process
            ConfluxService cfsMain = new();

            cfsMain.Register("CircleSearch.exe", "CircleSearch.CoreToMain", "CircleSearch.MainToCore");

            AppRuntime.cfsMain = cfsMain;

            cfsMain.OnMessageReceiving += CFSIncomingHandler.Handle;

            cfsMain.OnMessageReceived += CFSCommandHandler.Handle;

            cfsMain.StartServiceAsync();
            #endregion

            #region Initialize ConfluxService for Tray Process
            ConfluxService cfsTray = new();

            cfsTray.Register("CircleSearch.Tray.exe", "CircleSearch.CoreToTray", "CircleSearch.TrayToCore");
            AppRuntime.cfsTray = cfsTray;

            cfsTray.OnMessageReceiving += CFSIncomingHandler.Handle;

            cfsTray.OnMessageReceived += CFSCommandHandler.Handle;

            cfsTray.CreateNoWindow = true;
            cfsTray.StartApp();
            cfsTray.StartService();
            #endregion
        }

        public void OnStopped()
        {
            AppRuntime.hotkeyService.Dispose();
        }
    }
}
