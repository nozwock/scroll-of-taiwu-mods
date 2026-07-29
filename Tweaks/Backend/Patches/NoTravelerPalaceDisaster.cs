using GameData.Domains.Map;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchNoTravelerPalaceDisaster))]
static class PatchNoTravelerPalaceDisaster
{
    internal static bool _enabled;

    // Make Immortal Mansion ability apply no side-effects (poison, etc.)
    // TeleportOnTravelerPalace
    //      MakeRandomTravelerPalaceDisaster
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapDomain), nameof(MapDomain.MakeRandomTravelerPalaceDisaster))]
    static bool MapDomain_MakeRandomTravelerPalaceDisaster_Prefix() => false;
}
