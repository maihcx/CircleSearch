using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace CircleSearch.Core.Services;

public sealed class GlobalHotkeyService : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern sbyte GetMessage(out MSG msg, IntPtr hWnd, uint min, uint max);

    private const int WM_HOTKEY = 0x0312;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CTRL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private readonly int _id;
    private Thread? _messageThread;
    private volatile bool _running;
    private readonly object _lock = new();

    private bool _registered;
    private uint _vk;
    private uint _mods;

    public event Action? HotkeyPressed;

    public GlobalHotkeyService(int id = 9001)
    {
        _id = id;
    }

    public bool Register(bool ctrl, bool win, bool alt, bool shift, uint vk)
    {
        lock (_lock)
        {
            if (_registered)
                throw new InvalidOperationException("Hotkey already registered");

            _mods = MOD_NOREPEAT;
            if (ctrl) _mods |= MOD_CTRL;
            if (win) _mods |= MOD_WIN;
            if (alt) _mods |= MOD_ALT;
            if (shift) _mods |= MOD_SHIFT;

            _vk = vk;

            // start message loop thread
            _running = true;
            _messageThread = new Thread(MessageLoop)
            {
                IsBackground = true,
                Name = "HotkeyMessageLoop"
            };
            _messageThread.Start();

            // đợi thread init xong và register
            SpinWait.SpinUntil(() => _registered || !_running, 1000);

            return _registered;
        }
    }

    public void Unregister()
    {
        lock (_lock)
        {
            if (!_registered)
                return;

            _running = false;

            // gửi message giả để break GetMessage
            PostThreadMessage(_messageThreadId, 0x0012 /* WM_QUIT */, IntPtr.Zero, IntPtr.Zero);

            _messageThread?.Join();

            UnregisterHotKey(IntPtr.Zero, _id);
            _registered = false;
        }
    }

    private uint _messageThreadId;

    private void MessageLoop()
    {
        _messageThreadId = GetCurrentThreadId();

        if (!RegisterHotKey(IntPtr.Zero, _id, _mods, _vk))
        {
            _registered = false;
            _running = false;
            return;
        }

        _registered = true;

        while (_running && GetMessage(out MSG msg, IntPtr.Zero, 0, 0) != 0)
        {
            if (msg.message == WM_HOTKEY && msg.wParam.ToInt32() == _id)
            {
                try
                {
                    HotkeyPressed?.Invoke();
                }
                catch
                {
                    // tránh crash thread
                }
            }
        }
    }

    public void Dispose()
    {
        Unregister();
    }

    #region Win32

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int pt_x;
        public int pt_y;
    }

    #endregion
}