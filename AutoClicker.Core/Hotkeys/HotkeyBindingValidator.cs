namespace AutoClicker.Core.Hotkeys;

public readonly record struct HotkeyBinding(string Operation, int VirtualKeyCode);

public readonly record struct HotkeyConflict(string FirstOperation, string SecondOperation, int VirtualKeyCode);

public static class HotkeyBindingValidator
{
    public static IReadOnlyList<HotkeyConflict> Validate(params HotkeyBinding[] bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        var conflicts = new List<HotkeyConflict>();

        for (var i = 0; i < bindings.Length; i++)
        {
            for (var j = i + 1; j < bindings.Length; j++)
            {
                if (bindings[i].VirtualKeyCode == bindings[j].VirtualKeyCode)
                    conflicts.Add(new HotkeyConflict(bindings[i].Operation, bindings[j].Operation, bindings[i].VirtualKeyCode));
            }
        }

        return conflicts;
    }
}
