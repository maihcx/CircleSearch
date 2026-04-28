namespace CircleSearch.Overlay.Utils
{
    public class OverlayConfig
    {
        public int SearchEngine { get; set; } = 0;
        public string OcrLanguage { get; set; } = "vie";
        public double Opacity { get; set; } = 0.85;
        public string AccentColor { get; set; } = "#4FC3F7";

        public static OverlayConfig ParseArgs(string[] args)
        {
            var cfg = new OverlayConfig();
            for (int i = 0; i < args.Length - 1; i++)
            {
                switch (args[i])
                {
                    case "--engine":
                        if (int.TryParse(args[i + 1], out var eng)) cfg.SearchEngine = eng; break;
                    case "--ocrlang":
                        cfg.OcrLanguage = args[i + 1]; break;
                    case "--opacity":
                        if (double.TryParse(args[i + 1],
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var op))
                            cfg.Opacity = op > 0 ? op : 0.85;
                        break;
                    case "--accent":
                        cfg.AccentColor = args[i + 1]; break;
                }
            }
            return cfg;
        }

        public string GetSearchUrl(string query)
        {
            var encoded = Uri.EscapeDataString(query);
            return SearchEngine switch
            {
                1 => $"https://www.bing.com/search?q={encoded}",
                2 => $"https://duckduckgo.com/?q={encoded}",
                3 => $"https://yandex.com/search/?text={encoded}",
                4 => $"https://www.baidu.com/s?wd={encoded}",
                _ => $"https://www.google.com/search?q={encoded}"
            };
        }
    }
}
