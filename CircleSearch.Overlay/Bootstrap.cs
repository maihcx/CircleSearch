namespace CircleSearch.Overlay
{
    public class Bootstrap
    {
        App Instance;

        private OverlayProcessService? overlayService;

        public Bootstrap(string[] args, App Instance)
        {
            this.Instance = Instance;
            AppRuntime.overlayConfig = OverlayConfig.ParseArgs(args);
        }

        public void OnStarted()
        {
            StartOverlayMode();
        }

        public void OnStopped()
        {
            overlayService?.Dispose();
        }

        private void StartOverlayMode()
        {
            Instance.ShutdownMode = ShutdownMode.OnLastWindowClose;
            var window = new OverlayWindow();
            window.Closed += (_, _) => Instance.Shutdown();
            window.Show();
            window.Activate();
            window.Focus();
        }
    }
}
