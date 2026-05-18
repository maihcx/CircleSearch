using Microsoft.Extensions.Hosting;

namespace CircleSearch.Core
{
    public class Bootstrap
    {
        private readonly IHostApplicationLifetime lifetime;

        public Bootstrap(
            IHostApplicationLifetime lifetime)
        {
            this.lifetime = lifetime;
        }

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

            _ = cfsMain.StartServiceAsync();
            #endregion

            #region Initialize ConfluxService for Tray Process
            ConfluxService cfsTray = new();

            cfsTray.Register("CircleSearch Tray.exe", "CircleSearch.CoreToTray", "CircleSearch.TrayToCore");
            AppRuntime.cfsTray = cfsTray;

            cfsTray.OnMessageReceiving += CFSIncomingHandler.Handle;

            cfsTray.OnMessageReceived += CFSCommandHandler.Handle;

            cfsTray.CreateNoWindow = true;

            cfsTray.StartApp();

            _ = cfsTray.StartServiceAsync();
            #endregion
        }

        public void OnStopped()
        {
            AppRuntime.hotkeyService?.Dispose();
            _ = AppRuntime.cfsMain?.StopServiceAsync();
            AppRuntime.cfsMain = null;

            _ = AppRuntime.cfsTray?.StopServiceAsync();
            AppRuntime.cfsTray = null;
        }

        public void Shutdown()
        {
            lifetime.StopApplication();
        }
    }
}
