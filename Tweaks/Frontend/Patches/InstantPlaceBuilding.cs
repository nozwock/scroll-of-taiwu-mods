using Config;
using Game.Views.Building;
using HarmonyLib;

[HarmonyPatch]
static class PatchInstantPlaceBuilding
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewBuildingArea), nameof(ViewBuildingArea.StartPlacingBuilding))]
    static void ViewBuildingArea_StartPlacingBuilding_Prefix(
        BuildingBlockItem item,
        ref sbyte level,
        ref bool instantBuild
    )
    {
        level = item.MaxLevel;
        instantBuild = true; // Doesn't cost resources either
        // These values are then used in ViewBuildingArea.ConfirmBuild
    }
}
