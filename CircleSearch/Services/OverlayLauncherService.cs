namespace CircleSearch.Services;

public class OverlayLauncherService
{
    public void SendConfig(LauncherSettings settings)
    {
        ConfluxManager.cfsCircleSearchCore.Send("hotkey-change", BuildHotkeyArgs(settings));
    }

    public void SendHotkey(LauncherSettings settings)
    {
        ConfluxManager.cfsCircleSearchCore.Send("overlaycfg-change", BuildBaseArgs(settings));
    }

    public void LaunchOnce(LauncherSettings settings)
    {
        ConfluxManager.cfsCircleSearchCore.Send("start-ocr", "--no-cfg");
    }

    private string BuildHotkeyArgs(LauncherSettings s)
    {
        var vk = KeyInterop.VirtualKeyFromKey(
            (Key)Enum.Parse(typeof(Key), s.Hotkey.Key, true));
        return
            (s.Hotkey.Ctrl  ? " --ctrl"  : "") +
            (s.Hotkey.Win   ? " --win"   : "") +
            (s.Hotkey.Alt   ? " --alt"   : "") +
            (s.Hotkey.Shift ? " --shift" : "") +
            $" --vk {vk}";
    }

    private static string BuildBaseArgs(LauncherSettings s) =>
        $"--engine {(int)s.SearchEngine} " +
        $"--ocrlang \"{s.OcrLanguage}\" " +
        $"--opacity {s.OverlayOpacity.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
        $"--accent \"{s.OverlayAccentColor}\"";

    public bool IsDaemonRunning => true;
}
