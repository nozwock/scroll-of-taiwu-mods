using Game.Views.Adventure;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory]
static class PatchFreeMoveInAdventure
{
    internal static bool _enabled;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewAdventureRemake), nameof(ViewAdventureRemake.SetCostText))]
    static void ViewAdventureRemake_SetCostText_Prefix(ref int moveCost) => moveCost = -1; // Hide cost gameobject
}
