using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Runtime.InteropServices;
using AutoClicker.Core.Input;
using AutoClicker.Utils;

namespace AutoClicker.Input;

internal sealed class WindowsMouseNativeApi : IMouseNativeApi
{
    private int _lastError;

    public int LastError => _lastError;

    public bool SetCursorPosition(Point position)
    {
        var result = Win32ApiUtils.SetCursorPosition(position.X, position.Y);
        _lastError = result ? 0 : Marshal.GetLastWin32Error();
        return result;
    }

    public uint Send(IReadOnlyList<MouseInputPacket> packets)
    {
        var inputs = packets.Select(packet => new Win32ApiUtils.Input
        {
            Type = Win32ApiUtils.InputMouse,
            Data = new Win32ApiUtils.InputUnion
            {
                Mouse = new Win32ApiUtils.MouseInput
                {
                    MouseData = packet.MouseData,
                    Flags = (uint)packet.Flags
                }
            }
        }).ToArray();

        var sent = Win32ApiUtils.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Win32ApiUtils.Input>());
        _lastError = sent == inputs.Length ? 0 : Marshal.GetLastWin32Error();
        return sent;
    }
}
