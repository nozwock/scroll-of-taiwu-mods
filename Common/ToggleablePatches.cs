using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common;

class ToggleablePatches(params Type[] types)
{
    readonly Dictionary<Type, bool> _patchedEnabledValues = [];
    readonly Dictionary<Type, FieldInfo> _patchEnabledFields = types
        .Select(t =>
            (
                t,
                field: t.GetField(
                    "_enabled",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
                )
            )
        )
        .Where(it => it.field != null && it.field.FieldType == typeof(bool))
        .ToDictionary(it => it.t, it => it.field!);

    public IEnumerable<(Type PatchType, bool Enabled, bool Changed)> EnumeratePatchStates()
    {
        foreach (var (type, enabledField) in _patchEnabledFields)
        {
            // `!` is there because otherwise it complains about unboxing nullable, but only in net8, i.e. Backend
            var enabled = (bool)enabledField.GetValue(null)!;
            var changed = enabled != false;
            if (_patchedEnabledValues.TryGetValue(type, out var enabledBefore))
                changed = enabledBefore != enabled;
            _patchedEnabledValues[type] = enabled;

            // We need `changed` here because Harmony is stupid and its patch/unpatch functions will crash when calling
            // Unpatch when the method wasn't patched even once and vice-versa.
            yield return (type, enabled, changed);
        }
    }
}
