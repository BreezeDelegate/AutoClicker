namespace AutoClicker.Core.Hotkeys;

public readonly record struct HotkeyRegistrationBinding(int VirtualKeyCode, bool IncludeModifiers);

public sealed record HotkeyRegistrationSlot(
    string Operation,
    IReadOnlyList<int> Ids,
    HotkeyRegistrationBinding Binding);

public readonly record struct HotkeyRegistrationResult(
    bool Success,
    string? FailedOperation,
    int NativeError,
    bool RollbackSucceeded)
{
    public static HotkeyRegistrationResult Ok() => new(true, null, 0, true);
}

public interface IHotkeyRegistrationBackend
{
    int LastError { get; }
    bool Register(int id, int modifier, int virtualKeyCode);
    bool Unregister(int id);
}

public static class HotkeyRegistrationTransaction
{
    public static HotkeyRegistrationResult TryRegisterSlot(
        IHotkeyRegistrationBackend backend,
        HotkeyRegistrationSlot slot,
        IReadOnlyList<int> modifiers)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(modifiers);

        var pairs = Expand(slot, modifiers);
        var registeredIds = new List<int>();

        foreach (var pair in pairs)
        {
            if (!backend.Register(pair.Id, pair.Modifier, slot.Binding.VirtualKeyCode))
            {
                var error = backend.LastError;
                foreach (var id in registeredIds)
                    backend.Unregister(id);
                return new HotkeyRegistrationResult(false, slot.Operation, error, true);
            }
            registeredIds.Add(pair.Id);
        }

        return HotkeyRegistrationResult.Ok();
    }

    public static HotkeyRegistrationResult ReplaceAll(
        IHotkeyRegistrationBackend backend,
        IReadOnlyList<HotkeyRegistrationSlot> current,
        IReadOnlyList<HotkeyRegistrationSlot> desired,
        IReadOnlyList<int> modifiers)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(desired);
        ArgumentNullException.ThrowIfNull(modifiers);

        foreach (var slot in current)
            UnregisterSlot(backend, slot);

        foreach (var slot in desired)
        {
            var result = TryRegisterSlot(backend, slot, modifiers);
            if (result.Success)
                continue;

            var error = result.NativeError;
            foreach (var desiredSlot in desired)
                UnregisterSlot(backend, desiredSlot);

            var rollbackSucceeded = true;
            foreach (var oldSlot in current)
            {
                if (!TryRegisterSlot(backend, oldSlot, modifiers).Success)
                    rollbackSucceeded = false;
            }

            return new HotkeyRegistrationResult(false, slot.Operation, error, rollbackSucceeded);
        }

        return HotkeyRegistrationResult.Ok();
    }

    public static void UnregisterSlot(IHotkeyRegistrationBackend backend, HotkeyRegistrationSlot slot)
    {
        foreach (var id in slot.Ids)
            backend.Unregister(id);
    }

    private static IReadOnlyList<(int Id, int Modifier)> Expand(
        HotkeyRegistrationSlot slot,
        IReadOnlyList<int> modifiers)
    {
        if (slot.Ids.Count == 0 || modifiers.Count == 0)
            throw new ArgumentException("Hotkey IDs and modifiers cannot be empty.");

        var count = slot.Binding.IncludeModifiers ? Math.Min(slot.Ids.Count, modifiers.Count) : 1;
        var result = new (int Id, int Modifier)[count];
        for (var i = 0; i < count; i++)
            result[i] = (slot.Ids[i], modifiers[i]);
        return result;
    }
}
