using Config;
using GameData.Common;
using GameData.Domains.Building;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchMaxLevelPlaceBuilding))]
static class PatchMaxLevelPlaceBuilding
{
    internal static bool _enabled;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BuildingDomain), nameof(BuildingDomain.Build))]
    static void BuildingDomain_Build_Postfix(
        BuildingDomain __instance,
        DataContext context,
        BuildingBlockKey blockKey,
        short buildingTemplateId,
        (short, BuildingBlockData) __result
    )
    {
        if (PatchInstantPlaceBuilding._enabled)
            return;

        var element_BuildingBlocks = __result.Item2;
        var maxLevel = BuildingBlock.Instance[buildingTemplateId].MaxLevel;
        element_BuildingBlocks.Level = maxLevel;
        for (var i = 0; i < maxLevel; i++)
        {
            element_BuildingBlocks.UnlockLevelSlot(i);
        }
        __instance.SetElement_BuildingBlocks(blockKey, element_BuildingBlocks, context);
    }
}
