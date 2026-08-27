using System;
using System.Runtime.InteropServices;

namespace AutoClicker.Utils
{
    public static class Win32ApiUtils
    {
        internal const uint InputMouse = 0;

        [StructLayout(LayoutKind.Sequential)]
        internal struct Input
        {
            public uint Type;
            public InputUnion Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)]
            public MouseInput Mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MouseInput
        {
            public int Dx;
            public int Dy;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public UIntPtr ExtraInfo;
        }

        [DllImport("user32.dll", EntryPoint = "SetCursorPos", SetLastError = true)]
        internal static extern bool SetCursorPosition(int x, int y);

        [DllImport("user32.dll", EntryPoint = "SendInput", SetLastError = true)]
        internal static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

        [DllImport("user32.dll", EntryPoint = "RegisterHotKey", SetLastError = true)]
        internal static extern bool RegisterHotkey(nint hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", EntryPoint = "UnregisterHotKey", SetLastError = true)]
        internal static extern bool DeregisterHotkey(nint hWnd, int id);
    }
}
