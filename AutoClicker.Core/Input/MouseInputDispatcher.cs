using System.ComponentModel;
using System.Drawing;

namespace AutoClicker.Core.Input;

public sealed class MouseInputDispatcher : IMouseInput
{
    private readonly IMouseNativeApi _native;

    public MouseInputDispatcher(IMouseNativeApi native) =>
        _native = native ?? throw new ArgumentNullException(nameof(native));

    public void Click(ClickButton button, ClickAction action, Point? fixedPosition)
    {
        if (fixedPosition is Point position && !_native.SetCursorPosition(position))
            throw new Win32Exception(_native.LastError, "Failed to set the mouse cursor position.");

        var packets = MouseInputBuilder.Build(button, action);
        var sent = _native.Send(packets);
        if (sent != packets.Count)
            throw new Win32Exception(_native.LastError, $"SendInput sent {sent} of {packets.Count} mouse packets.");
    }
}
