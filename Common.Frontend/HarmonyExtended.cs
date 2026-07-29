using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HarmonyLib;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
class HarmonyPatchCategory : Attribute { }

/// <summary>
/// The frontend's (Mono Unity game) 0Harmony.dll seems to be an older version that doesn't have HarmonyPatchCategory,
/// which prevents us from doing patching and unpatching based on groups. So here it is, HarmonyPatchCategory at home.
/// </summary>
record class HarmonyExtended(string Id)
{
    readonly Harmony _uncategorized = new(Id);
    readonly Dictionary<Type, Harmony> _categories = [];

    public void PatchAllUncategorized()
    {
        AccessTools
            .GetTypesFromAssembly(Assembly.GetCallingAssembly())
            .DoIf(t => !HasPatchCategory(t), t => _uncategorized.CreateClassProcessor(t).Patch());
    }

    public void UnpatchSelf()
    {
        _uncategorized.UnpatchSelf();

        foreach (var (id, harmony) in _categories.ToList())
        {
            harmony.UnpatchSelf();
            _categories.Remove(id);
        }
    }

    public void PatchCategory(Type type)
    {
        if (_categories.ContainsKey(type) || !HasPatchCategory(type))
            return;

        var harmony = new Harmony($"{Id}.{type.FullName}");
        harmony.CreateClassProcessor(type).Patch();

        _categories.Add(type, harmony);
    }

    public void UnpatchCategory(Type type)
    {
        if (!_categories.TryGetValue(type, out var harmony))
            return;

        harmony.UnpatchSelf();
        _categories.Remove(type);
    }

    public IEnumerable<MethodBase> GetPatchedMethods() =>
        _categories
            .Values.SelectMany(harmony => harmony.GetPatchedMethods())
            .Concat(_uncategorized?.GetPatchedMethods());

    static bool HasPatchCategory(Type type)
    {
        for (var t = type; t != null; t = t.DeclaringType)
        {
            if (t.IsDefined(typeof(HarmonyPatchCategory), inherit: true))
                return true;
        }

        return false;
    }
}
