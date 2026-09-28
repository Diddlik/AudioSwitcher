using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AudioSwitcher.Services;

public sealed class GlobalHotkeyService : IDisposable
{
    private const uint WmHotkey = 0x0312;
    private const uint WmAppRegister = 0x8001;
    private const uint WmQuit = 0x0012;

    private readonly ConcurrentQueue<RegistrationRequest> _requests = new();
    private readonly LocalizationService _text;
    private readonly Dictionary<int, string> _registrations = [];
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new();
    private uint _threadId;
    private bool _disposed;

    public GlobalHotkeyService(LocalizationService text)
    {
        _text = text;
        _thread = new Thread(MessageLoop)
        {
            IsBackground = true,
            Name = "AudioSwitcher global hotkeys",
        };
        _thread.Start();
        _ready.Wait();
    }

    public event EventHandler<string>? Triggered;

    public bool Register(IReadOnlyDictionary<string, string> shortcuts, out string error)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var request = new RegistrationRequest(shortcuts);
        _requests.Enqueue(request);
        if (!PostThreadMessage(_threadId, WmAppRegister, UIntPtr.Zero, IntPtr.Zero))
        {
            error = new Win32Exception().Message;
            return false;
        }

        if (!request.Completed.Wait(TimeSpan.FromSeconds(5)))
        {
            error = _text["ShortcutRegistrationTimeout"];
            return false;
        }

        error = request.Error;
        return string.IsNullOrEmpty(error);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        PostThreadMessage(_threadId, WmQuit, UIntPtr.Zero, IntPtr.Zero);
        _thread.Join(TimeSpan.FromSeconds(2));
        _ready.Dispose();
    }

    private void MessageLoop()
    {
        _threadId = GetCurrentThreadId();
        PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
        _ready.Set();

        while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
        {
            if (message.Message == WmHotkey && _registrations.TryGetValue((int)message.WParam, out var action))
            {
                Triggered?.Invoke(this, action);
            }
            else if (message.Message == WmAppRegister)
            {
                while (_requests.TryDequeue(out var request))
                {
                    ApplyRegistrations(request);
                }
            }
        }

        ClearRegistrations();
    }

    private void ApplyRegistrations(RegistrationRequest request)
    {
        ClearRegistrations();
        var id = 1;

        foreach (var shortcut in request.Shortcuts)
        {
            if (!HotkeyGesture.TryParse(shortcut.Value, out var gesture, out var parseError, _text))
            {
                request.Error = $"{shortcut.Value}: {parseError}";
                ClearRegistrations();
                request.Completed.Set();
                return;
            }

            if (!RegisterHotKey(IntPtr.Zero, id, (uint)gesture.Modifiers, gesture.VirtualKey))
            {
                request.Error = _text.Format("ShortcutAlreadyInUse", shortcut.Value);
                ClearRegistrations();
                request.Completed.Set();
                return;
            }

            _registrations[id++] = shortcut.Key;
        }

        request.Completed.Set();
    }

    private void ClearRegistrations()
    {
        foreach (var id in _registrations.Keys)
        {
            UnregisterHotKey(IntPtr.Zero, id);
        }

        _registrations.Clear();
    }

    private sealed class RegistrationRequest(IReadOnlyDictionary<string, string> shortcuts)
    {
        public IReadOnlyDictionary<string, string> Shortcuts { get; } = shortcuts;
        public ManualResetEventSlim Completed { get; } = new();
        public string Error { get; set; } = string.Empty;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public IntPtr HWnd;
        public uint Message;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int PointX;
        public int PointY;
        public uint Private;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr window, int id);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out NativeMessage message, IntPtr window, uint minimum, uint maximum);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(out NativeMessage message, IntPtr window, uint minimum, uint maximum, uint remove);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessage(uint threadId, uint message, UIntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
