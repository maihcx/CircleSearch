namespace CircleSearch.Core.Utils
{
    public static class AppRuntime
    {
        public static ConfluxService? cfsMain { get; set; }

        public static ConfluxService? cfsTray { get; set; }

        public static Bootstrap? bootstrap { get; set; }

        public static GlobalHotkeyService? hotkeyService;
        public static string OverlayConfig = string.Empty;

        public static void StartOverlay()
        {
            ConfluxService overlayConflux = new ConfluxService();
            overlayConflux.Register("CircleSearch Overlay.exe", "CircleSearchCoreToOverlay");
            overlayConflux.StartApp(OverlayConfig);
        }
    }
}
