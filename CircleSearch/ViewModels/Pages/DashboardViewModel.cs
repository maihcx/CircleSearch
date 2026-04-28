using CircleSearch.Models;
using CircleSearch.Services;
using System.Collections.ObjectModel;

namespace CircleSearch.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly LauncherSettings _settings;
        private readonly OverlayLauncherService _launcher;

        [ObservableProperty] private string _hotkeyDisplay = "";
        [ObservableProperty] private bool _ctrlKey;
        [ObservableProperty] private bool _winKey;
        [ObservableProperty] private bool _altKey;
        [ObservableProperty] private bool _shiftKey;
        [ObservableProperty] private string _selectedKey = "Z";
        [ObservableProperty] private int _selectedSearchEngine;
        [ObservableProperty] private string _selectedOcrLanguage = "eng";
        [ObservableProperty] private bool _startWithWindows;
        [ObservableProperty] private bool _minimizeToTray;
        [ObservableProperty] private double _overlayOpacity;
        [ObservableProperty] private string _accentColor = "#4FC3F7";
        [ObservableProperty] private bool _isDaemonRunning;
        [ObservableProperty] private string _statusMessage = "Đang khởi động...";

        public ObservableCollection<string> AvailableKeys { get; } = new(new[]
        {
        "A","B","C","D","E","F","G","H","I","J","K","L","M",
        "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
        "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
        "Space","Tab","OemTilde"
    });

    public ObservableCollection<SearchEngineItem> SearchEngines { get; } = new(new[]
    {
        new SearchEngineItem("Google",     0),
        new SearchEngineItem("Bing",       1),
        new SearchEngineItem("DuckDuckGo", 2),
        new SearchEngineItem("Yandex",     3),
        new SearchEngineItem("Baidu",      4),
    });

    public ObservableCollection<OcrLanguageItem> OcrLanguages { get; } = new(new[]
    {
        new OcrLanguageItem("English",           "eng"),
        new OcrLanguageItem("Tiếng Việt",           "vie"),
        new OcrLanguageItem("English + Tiếng Việt",     "eng+vie"),
    });

        public DashboardViewModel()
        {
            _settings = SharedMem.AppSettings;
            _launcher = SharedMem.Launcher;
            LoadFromSettings();
            IsDaemonRunning = _launcher.IsDaemonRunning;
            StatusMessage   = IsDaemonRunning
                ? $"✓ Đang chạy — nhấn {HotkeyDisplay} để kích hoạt"
                : "⚠ Overlay daemon chưa khởi động";
        }

        private void LoadFromSettings()
        {
            CtrlKey              = _settings.Hotkey.Ctrl;
            WinKey               = _settings.Hotkey.Win;
            AltKey               = _settings.Hotkey.Alt;
            ShiftKey             = _settings.Hotkey.Shift;
            SelectedKey          = _settings.Hotkey.Key;
            SelectedSearchEngine = (int)_settings.SearchEngine;
            SelectedOcrLanguage  = _settings.OcrLanguage;
            StartWithWindows     = _settings.StartWithWindows;
            MinimizeToTray       = _settings.MinimizeToTray;
            OverlayOpacity       = _settings.OverlayOpacity;
            AccentColor          = _settings.OverlayAccentColor;
            UpdateHotkeyDisplay();
        }

        private void UpdateHotkeyDisplay()
        {
            var parts = new List<string>();
            if (CtrlKey) parts.Add("Ctrl");
            if (WinKey) parts.Add("Win");
            if (AltKey) parts.Add("Alt");
            if (ShiftKey) parts.Add("Shift");
            parts.Add(SelectedKey);
            HotkeyDisplay = string.Join(" + ", parts);
        }

        partial void OnCtrlKeyChanged(bool _) => UpdateHotkeyDisplay();
        partial void OnWinKeyChanged(bool _) => UpdateHotkeyDisplay();
        partial void OnAltKeyChanged(bool _) => UpdateHotkeyDisplay();
        partial void OnShiftKeyChanged(bool _) => UpdateHotkeyDisplay();
        partial void OnSelectedKeyChanged(string _) => UpdateHotkeyDisplay();

        [RelayCommand]
        private void SaveSettings()
        {
            _settings.Hotkey.Ctrl  = CtrlKey;
            _settings.Hotkey.Win   = WinKey;
            _settings.Hotkey.Alt   = AltKey;
            _settings.Hotkey.Shift = ShiftKey;
            _settings.Hotkey.Key   = SelectedKey;
            _settings.SearchEngine      = (Models.SearchEngines)SelectedSearchEngine;
            _settings.OcrLanguage       = SelectedOcrLanguage;
            _settings.MinimizeToTray    = MinimizeToTray;
            _settings.OverlayOpacity    = OverlayOpacity;
            _settings.OverlayAccentColor = AccentColor;
            _settings.Save();

            _launcher.SendHotkey(_settings);
            _launcher.SendConfig(_settings);
            IsDaemonRunning = true;

            StatusMessage = $"✓ Đã lưu — nhấn {HotkeyDisplay} để kích hoạt";
        }

        [RelayCommand]
        private void TestOverlay()
        {
            _launcher.LaunchOnce(_settings);
            StatusMessage = "Đã mở overlay thử nghiệm...";
        }

        private static void SetStartup(bool enable)
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (enable)
                key.SetValue("CircleSearch",
                    System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName);
            else
                key.DeleteValue("CircleSearch", false);

            key.Close();
        }
    }

    public record SearchEngineItem(string Name, int Value);
    public record OcrLanguageItem(string Name, string Code);
}
