using GameData.Domains.World;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchEditWorldSettings))]
static class PatchEditWorldSettings
{
    internal static bool _enabled;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(WorldDomain), nameof(WorldDomain.GetCanResetWorldSettings))]
    static bool WorldDomain_GetCanResetWorldSettings_Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}
