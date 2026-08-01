using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
class HarmonyPatchCategory : Attribute { }

/// <summary>
/// The frontend's (Mono Unity game) 0Harmony.dll seems to be an older version that doesn't have HarmonyPatchCategory,
/// which prevents us from doing patching and unpatching based on groups. So here it is, HarmonyPatchCategory at home.
/// </summary>
static class HarmonyExtensions
{
    record class AttachedData(Dictionary<Type, Harmony> Categories);

    static readonly ConditionalWeakTable<Harmony, AttachedData> _attachedData = new();

    public static void PatchAllUncategorized(this Harmony self)
    {
        AccessTools
            .GetTypesFromAssembly(Assembly.GetCallingAssembly())
            .DoIf(t => !HasPatchCategory(t), t => self.CreateClassProcessor(t).Patch());
    }

    /// <summary>
    /// Frontend's Harmony requires the <paramref name="type"/> to have the <see cref="HarmonyPatchCategory"/>
    /// attribute.
    /// </summary>
    public static void PatchCategory(this Harmony self, Type type)
    {
        var data = self.GetAttachedData();
        if (
            data.Categories.ContainsKey(type)
            // We don't want to allow patching any nested class that doesn't have the attribute explicitly
            || !type.IsDefined(typeof(HarmonyPatchCategory), inherit: true)
        )
            return;

        var harmony = new Harmony($"{self.Id}.{type.FullName}");
        // allowUnannotatedType - don't require HarmonyPatch attribute on class
        harmony.CreateClassProcessor(type, allowUnannotatedType: true).Patch();
        // These nested types are excluded in PatchAllUncategorized
        foreach (var t in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            harmony.CreateClassProcessor(t).Patch(); // Patch all nested Harmony methods
        }

        data.Categories.Add(type, harmony);
    }

    public static void UnpatchCategory(this Harmony self, Type type)
    {
        var data = self.GetAttachedData();
        if (!data.Categories.TryGetValue(type, out var harmony))
            return;

        harmony.UnpatchSelf();
        data.Categories.Remove(type);
    }

    public static void UnpatchAllCategories(this Harmony self)
    {
        var data = self.GetAttachedData();
        foreach (var (id, harmony) in data.Categories.ToList())
        {
            harmony.UnpatchSelf();
            data.Categories.Remove(id);
        }
    }

    public static void UnpatchSelf(this Harmony self)
    {
        self.UnpatchSelf();
        self.UnpatchAllCategories();
    }

    public static IEnumerable<MethodBase> GetPatchedMethods(this Harmony self) =>
        self.GetAttachedData()
            .Categories.Values.SelectMany(harmony => harmony.GetPatchedMethods())
            .Concat(self?.GetPatchedMethods());

    static AttachedData GetAttachedData(this Harmony self)
    {
        if (!_attachedData.TryGetValue(self, out var data))
        {
            data = new AttachedData([]);
            _attachedData.Add(self, data);
        }

        return data;
    }

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
