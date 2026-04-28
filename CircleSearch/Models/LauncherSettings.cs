using System.Text.Json;

namespace CircleSearch.Models;

public class LauncherSettings
{
    public HotkeyConfig  Hotkey            { get; set; } = new();
    public SearchEngines SearchEngine      { get; set; } = SearchEngines.Google;
    public string        OcrLanguage       { get; set; } = "eng";
    public bool          StartWithWindows  { get; set; } = false;
    public bool          MinimizeToTray    { get; set; } = true;
    public double        OverlayOpacity    { get; set; } = 0.85;
    public string        OverlayAccentColor{ get; set; } = "#4FC3F7";

    public static LauncherSettings Load()
    {
        try
        {
            return JsonSerializer.Deserialize<LauncherSettings>(UserDataStore.GetValue<string>("ocr-cfg")) ?? new LauncherSettings();
        }
        catch { }
        return new LauncherSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            UserDataStore.SetValue("ocr-cfg", json);
        }
        catch { }
    }
}

public class HotkeyConfig
{
    public bool   Ctrl  { get; set; } = true;
    public bool   Win   { get; set; } = true;
    public bool   Alt   { get; set; } = false;
    public bool   Shift { get; set; } = false;
    public string Key   { get; set; } = "Z";

    public string ToDisplayString()
    {
        var parts = new List<string>();
        if (Ctrl)  parts.Add("Ctrl");
        if (Win)   parts.Add("Win");
        if (Alt)   parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        parts.Add(Key);
        return string.Join(" + ", parts);
    }
}

public enum SearchEngines
{
    Google, Bing, DuckDuckGo, Yandex, Baidu
}
