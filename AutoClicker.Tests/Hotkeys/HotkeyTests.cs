using AutoClicker.Core.Hotkeys;
using Xunit;

namespace AutoClicker.Tests.Hotkeys;

public sealed class HotkeyTests
{
    [Fact]
    public void ValidatorAcceptsDistinctBindings()
    {
        var conflicts = HotkeyBindingValidator.Validate(
            new HotkeyBinding("Start", 0x75),
            new HotkeyBinding("Stop", 0x76),
            new HotkeyBinding("Toggle", 0x77));

        Assert.Empty(conflicts);
    }

    [Theory]
    [InlineData("Start", "Stop", 0x75, 0x75, 0x77)]
    [InlineData("Start", "Toggle", 0x75, 0x76, 0x75)]
    [InlineData("Stop", "Toggle", 0x75, 0x76, 0x76)]
    public void ValidatorNamesConflictingOperations(string first, string second, int start, int stop, int toggle)
    {
        var conflict = Assert.Single(HotkeyBindingValidator.Validate(
            new HotkeyBinding("Start", start),
            new HotkeyBinding("Stop", stop),
            new HotkeyBinding("Toggle", toggle)));

        Assert.Equal(first, conflict.FirstOperation);
        Assert.Equal(second, conflict.SecondOperation);
    }

    [Fact]
    public void AtomicSlotRegistrationRemovesPartialRegistrationOnFailure()
    {
        var backend = new FakeBackend { FailOnRegistrationAttempt = 2, LastError = 1409 };
        var slot = Slot("Stop", 100, new HotkeyRegistrationBinding(0x76, IncludeModifiers: true));

        var result = HotkeyRegistrationTransaction.TryRegisterSlot(backend, slot, Modifiers);

        Assert.False(result.Success);
        Assert.Equal(1409, result.NativeError);
        Assert.Empty(backend.Active);
    }

    [Fact]
    public void ReplaceAllRestoresOldBindingsWhenNewRegistrationFails()
    {
        var backend = new FakeBackend();
        var current = new[]
        {
            Slot("Start", 100, new HotkeyRegistrationBinding(0x75, false)),
            Slot("Stop", 110, new HotkeyRegistrationBinding(0x76, false)),
            Slot("Toggle", 120, new HotkeyRegistrationBinding(0x77, false))
        };
        foreach (var slot in current)
            Assert.True(HotkeyRegistrationTransaction.TryRegisterSlot(backend, slot, Modifiers).Success);

        backend.ResetAttempts();
        backend.FailOnRegistrationAttempt = 2;
        backend.LastError = 1409;
        var desired = new[]
        {
            Slot("Start", 100, new HotkeyRegistrationBinding(0x78, false)),
            Slot("Stop", 110, new HotkeyRegistrationBinding(0x79, false)),
            Slot("Toggle", 120, new HotkeyRegistrationBinding(0x7A, false))
        };

        var result = HotkeyRegistrationTransaction.ReplaceAll(backend, current, desired, Modifiers);

        Assert.False(result.Success);
        Assert.True(result.RollbackSucceeded);
        Assert.Equal("Stop", result.FailedOperation);
        Assert.Contains((100, 0, 0x75), backend.Active);
        Assert.Contains((110, 0, 0x76), backend.Active);
        Assert.Contains((120, 0, 0x77), backend.Active);
        Assert.DoesNotContain(backend.Active, x => x.VirtualKeyCode is 0x78 or 0x79 or 0x7A);
    }

    private static readonly int[] Modifiers = { 0, 1, 2, 4 };

    private static HotkeyRegistrationSlot Slot(string name, int firstId, HotkeyRegistrationBinding binding) =>
        new(name, new[] { firstId, firstId + 1, firstId + 2, firstId + 3 }, binding);

    private sealed class FakeBackend : IHotkeyRegistrationBackend
    {
        private int _attempts;
        public int? FailOnRegistrationAttempt { get; set; }
        public int LastError { get; set; }
        public HashSet<(int Id, int Modifier, int VirtualKeyCode)> Active { get; } = new();

        public bool Register(int id, int modifier, int virtualKeyCode)
        {
            _attempts++;
            if (FailOnRegistrationAttempt == _attempts)
                return false;
            return Active.Add((id, modifier, virtualKeyCode));
        }

        public bool Unregister(int id)
        {
            Active.RemoveWhere(x => x.Id == id);
            return true;
        }

        public void ResetAttempts()
        {
            _attempts = 0;
            FailOnRegistrationAttempt = null;
        }
    }
}
