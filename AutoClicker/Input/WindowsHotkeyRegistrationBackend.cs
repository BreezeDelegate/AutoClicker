using System.Runtime.InteropServices;
using AutoClicker.Core.Hotkeys;
using AutoClicker.Utils;

namespace AutoClicker.Input;

internal sealed class WindowsHotkeyRegistrationBackend : IHotkeyRegistrationBackend
{
    private readonly nint _windowHandle;
    private int _lastError;

    public WindowsHotkeyRegistrationBackend(nint windowHandle) => _windowHandle = windowHandle;

    public int LastError => _lastError;

    public bool Register(int id, int modifier, int virtualKeyCode)
    {
        bool registered = Win32ApiUtils.RegisterHotkey(_windowHandle, id, modifier, virtualKeyCode);
        _lastError = registered ? 0 : Marshal.GetLastWin32Error();
        return registered;
    }

    public bool Unregister(int id)
    {
        bool unregistered = Win32ApiUtils.DeregisterHotkey(_windowHandle, id);
        _lastError = unregistered ? 0 : Marshal.GetLastWin32Error();
        return unregistered;
    }
}
