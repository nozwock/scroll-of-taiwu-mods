using GameData.Domains.Extra;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchInfiniteActionPoint))]
static class PatchInfiniteActionPoint
{
    internal static bool _enabled;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ExtraDomain), nameof(ExtraDomain.ConsumeActionPoint))]
    static void ExtraDomain_ConsumeActionPoint_Prefix(ref int value)
    {
        value = 0;
    }
}
