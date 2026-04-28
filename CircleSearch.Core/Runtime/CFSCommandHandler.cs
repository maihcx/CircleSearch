namespace CircleSearch.Core.Runtime
{
    public static class CFSCommandHandler
    {
        public static void Handle(string name, string value)
        {
            switch (name)
            {
                case "main-event":
                    HandleTrayConfigChange(name, value);
                    break;
                case "tray-event":
                case "state":
                    HandleMainConfigChange(name, value);
                    break;
                case "core-svc-state":
                    HandleCoreState(value);
                    break;
            }
        }

        private static void HandleTrayConfigChange(string name, string value)
        {
            AppRuntime.cfsTray.Send(name, value);
        }

        private static void HandleMainConfigChange(string name, string value)
        {
            if (!AppRuntime.cfsMain.IsAppStarted())
            {
                AppRuntime.cfsMain.StartApp();
            }
            AppRuntime.cfsMain.Send(name, value);
        }

        private static void HandleCoreState(string value)
        {
            if (value == "shutdown")
            {
                if (AppRuntime.cfsMain.IsAppStarted()) {
                    AppRuntime.cfsMain.Send("state", value);
                }

                Program.OnClosed();
                Environment.Exit(0);
            }
        }
    }
}