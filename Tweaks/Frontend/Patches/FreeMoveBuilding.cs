using Game.Components.Building;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory]
class PatchFreeMoveBuilding
{
    internal static bool _enabled;

    // Called in Game.Views.Building.ViewBuildingArea.ConfirmResetBuild()
    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(BuildingAreaResourceChange),
        nameof(BuildingAreaResourceChange.RefreshResourceChangeOnPlan)
    )]
    static void BuildingAreaResourceChange_RefreshResourceChangeOnPlan_Prefix(ref int[] costArray)
    {
        if (costArray == null)
            return;

        for (var i = costArray.Length - 1; i >= 0; i--)
        {
            costArray[i] = 0;
        }
    }
}
