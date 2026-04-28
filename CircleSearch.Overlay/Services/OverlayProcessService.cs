using System.Diagnostics;
using System.IO;

namespace CircleSearch.Overlay.Services;

public sealed class OverlayProcessService : IDisposable
{
    private Process? _current;
    private readonly string _exePath;
    private readonly object _lock = new();

    public OverlayProcessService()
    {
        _exePath = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule!.FileName!;
    }

    public void OpenOverlay(OverlayConfig config)
    {
        lock (_lock)
        {
            KillCurrent();

            var args = BuildArgs(config);

            _current = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName        = _exePath,
                    Arguments       = args,
                    UseShellExecute = false,

                    CreateNoWindow  = true,
                },
                EnableRaisingEvents = true,
            };

            _current.Exited += (_, _) =>
            {
                lock (_lock)
                {
                    _current?.Dispose();
                    _current = null;
                }
            };

            _current.Start();
        }
    }

    public void KillCurrent()
    {
        lock (_lock)
        {
            if (_current is { HasExited: false })
            {
                try { _current.Kill(); } catch { }
            }
            _current?.Dispose();
            _current = null;
        }
    }

    private static string BuildArgs(OverlayConfig config)
    {
        return $"--overlay " +
               $"--engine {config.SearchEngine} " +
               $"--ocrlang {config.OcrLanguage} " +
               $"--opacity {config.Opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
               $"--accent {config.AccentColor}";
    }

    public void Dispose() => KillCurrent();
}