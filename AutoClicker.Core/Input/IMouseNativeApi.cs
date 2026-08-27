using System.Drawing;

namespace AutoClicker.Core.Input;

public interface IMouseNativeApi
{
    int LastError { get; }
    bool SetCursorPosition(Point position);
    uint Send(IReadOnlyList<MouseInputPacket> packets);
}
