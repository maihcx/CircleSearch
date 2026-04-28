namespace CircleSearch.Core.Runtime
{
    public static class CFSIncomingHandler
    {
        private static readonly Dictionary<string, string> currentMsgReceiving = new();

        public static void Handle(string name, string value)
        {
            currentMsgReceiving.TryGetValue(name, out string currMsgReceiving);
            if (currMsgReceiving == value)
                return;

            currentMsgReceiving[name] = value;

            switch (name)
            {
                case "hotkey-change":
                    HandleHotkeyChange(value);
                    break;
                case "overlaycfg-change":
                    HandleOverlayConfigChange(value);
                    break;
                case "start-ocr":
                    AppRuntime.StartOverlay();
                    currentMsgReceiving.Remove(name);
                    break;
            }
        }

        private static void HandleOverlayConfigChange(string value)
        {
            AppRuntime.OverlayConfig = value;
        }

        private static void HandleHotkeyChange(string value)
        {
            string[] args = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool ctrl = args.Contains("--ctrl");
            bool win = args.Contains("--win");
            bool alt = args.Contains("--alt");
            bool shift = args.Contains("--shift");

            uint vk = 0x5A;
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "--vk" && uint.TryParse(args[i + 1], out var v)) vk = v;

            AppRuntime.hotkeyService.Unregister();
            AppRuntime.hotkeyService.Register(ctrl, win, alt, shift, vk);
        }
    }
}
