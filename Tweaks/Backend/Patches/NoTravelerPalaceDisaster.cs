using GameData.Domains.Map;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatch]
static class PatchNoTravelerPalaceDisaster
{
    // Make Immortal Mansion ability apply no side-effects (poison, etc.)
    // TeleportOnTravelerPalace
    //      MakeRandomTravelerPalaceDisaster
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapDomain), nameof(MapDomain.MakeRandomTravelerPalaceDisaster))]
    static bool MapDomain_MakeRandomTravelerPalaceDisaster_Prefix() => false;
}
