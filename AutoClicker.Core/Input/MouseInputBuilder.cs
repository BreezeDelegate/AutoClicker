namespace AutoClicker.Core.Input;

[Flags]
public enum MouseInputFlags : uint
{
    LeftDown = 0x0002,
    LeftUp = 0x0004,
    RightDown = 0x0008,
    RightUp = 0x0010,
    MiddleDown = 0x0020,
    MiddleUp = 0x0040,
    XDown = 0x0080,
    XUp = 0x0100
}

public readonly record struct MouseInputPacket(MouseInputFlags Flags, uint MouseData);

public static class MouseInputBuilder
{
    public static IReadOnlyList<MouseInputPacket> Build(ClickButton button, ClickAction action)
    {
        var pair = button switch
        {
            ClickButton.Left => Pair(MouseInputFlags.LeftDown, MouseInputFlags.LeftUp, 0),
            ClickButton.Right => Pair(MouseInputFlags.RightDown, MouseInputFlags.RightUp, 0),
            ClickButton.Middle => Pair(MouseInputFlags.MiddleDown, MouseInputFlags.MiddleUp, 0),
            ClickButton.X1 => Pair(MouseInputFlags.XDown, MouseInputFlags.XUp, 1),
            ClickButton.X2 => Pair(MouseInputFlags.XDown, MouseInputFlags.XUp, 2),
            _ => throw new ArgumentOutOfRangeException(nameof(button))
        };

        return action switch
        {
            ClickAction.Single => pair,
            ClickAction.Double => new[] { pair[0], pair[1], pair[0], pair[1] },
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
    }

    private static MouseInputPacket[] Pair(MouseInputFlags down, MouseInputFlags up, uint data) =>
        new[] { new MouseInputPacket(down, data), new MouseInputPacket(up, data) };
}
