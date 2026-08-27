using System.ComponentModel;
using System.Drawing;
using AutoClicker.Core;
using AutoClicker.Core.Input;
using Xunit;

namespace AutoClicker.Tests.Input;

public sealed class MouseInputTests
{
    [Theory]
    [InlineData(ClickButton.Left, MouseInputFlags.LeftDown, MouseInputFlags.LeftUp, 0u)]
    [InlineData(ClickButton.Right, MouseInputFlags.RightDown, MouseInputFlags.RightUp, 0u)]
    [InlineData(ClickButton.Middle, MouseInputFlags.MiddleDown, MouseInputFlags.MiddleUp, 0u)]
    [InlineData(ClickButton.X1, MouseInputFlags.XDown, MouseInputFlags.XUp, 1u)]
    [InlineData(ClickButton.X2, MouseInputFlags.XDown, MouseInputFlags.XUp, 2u)]
    public void SingleClickMapsToExactDownUpPackets(ClickButton button, MouseInputFlags down, MouseInputFlags up, uint data)
    {
        var packets = MouseInputBuilder.Build(button, ClickAction.Single);

        Assert.Collection(packets,
            p => { Assert.Equal(down, p.Flags); Assert.Equal(data, p.MouseData); },
            p => { Assert.Equal(up, p.Flags); Assert.Equal(data, p.MouseData); });
    }

    [Fact]
    public void DoubleClickRepeatsTheExactPairTwice()
    {
        var packets = MouseInputBuilder.Build(ClickButton.Left, ClickAction.Double);

        Assert.Equal(4, packets.Count);
        Assert.Equal(packets[0], packets[2]);
        Assert.Equal(packets[1], packets[3]);
    }

    [Fact]
    public void DispatcherMovesCursorOnlyForFixedPositionAndSendsAllPackets()
    {
        var native = new FakeNativeMouseApi();
        var input = new MouseInputDispatcher(native);
        var target = new Point(120, 340);

        input.Click(ClickButton.Right, ClickAction.Double, target);

        Assert.Equal(target, native.LastPosition);
        Assert.Equal(4, native.LastPackets.Count);
    }

    [Fact]
    public void DispatcherDoesNotMoveCursorForCurrentPosition()
    {
        var native = new FakeNativeMouseApi();
        var input = new MouseInputDispatcher(native);

        input.Click(ClickButton.Left, ClickAction.Single, null);

        Assert.Null(native.LastPosition);
        Assert.Equal(2, native.LastPackets.Count);
    }

    [Fact]
    public void CursorFailureIsSurfaced()
    {
        var native = new FakeNativeMouseApi { CursorResult = false, LastError = 5 };
        var input = new MouseInputDispatcher(native);

        var error = Assert.Throws<Win32Exception>(() => input.Click(ClickButton.Left, ClickAction.Single, new Point(1, 2)));

        Assert.Equal(5, error.NativeErrorCode);
        Assert.Empty(native.LastPackets);
    }

    [Fact]
    public void PartialSendInputIsSurfaced()
    {
        var native = new FakeNativeMouseApi { ForcedSentCount = 1, LastError = 87 };
        var input = new MouseInputDispatcher(native);

        var error = Assert.Throws<Win32Exception>(() => input.Click(ClickButton.Left, ClickAction.Single, null));

        Assert.Equal(87, error.NativeErrorCode);
    }

    private sealed class FakeNativeMouseApi : IMouseNativeApi
    {
        public bool CursorResult { get; set; } = true;
        public uint? ForcedSentCount { get; set; }
        public int LastError { get; set; }
        public Point? LastPosition { get; private set; }
        public IReadOnlyList<MouseInputPacket> LastPackets { get; private set; } = Array.Empty<MouseInputPacket>();

        public bool SetCursorPosition(Point position)
        {
            LastPosition = position;
            return CursorResult;
        }

        public uint Send(IReadOnlyList<MouseInputPacket> packets)
        {
            LastPackets = packets.ToArray();
            return ForcedSentCount ?? (uint)packets.Count;
        }
    }
}
