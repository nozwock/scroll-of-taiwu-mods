using Config;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Building;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchInstantRemoveBuilding))]
static class PatchInstantRemoveBuilding
{
    internal static bool _enabled;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BuildingDomain), nameof(BuildingDomain.Remove))]
    static bool BuildingDomain_Remove_Prefix(
        BuildingDomain __instance,
        DataContext context,
        BuildingBlockKey blockKey,
        ref (short, BuildingBlockData) __result
    )
    {
        BuildingBlockData blockData = __instance.GetElement_BuildingBlocks(blockKey);
        BuildingBlockItem configData = BuildingBlock.Instance[blockData.TemplateId];

        if (
            (configData.Type != EBuildingBlockType.Building && !BuildingBlockData.IsResource(configData.Type))
            || blockData.OperationType != -1
            || configData.Class == EBuildingBlockClass.Static
            || configData.OperationTotalProgress[1] < 0
        )
        {
            __result = (blockKey.BuildingBlockIndex, blockData);
            return false;
        }

        // ref: BuildingDomain.OfflineUpdateOperation
        // Refund materials according to vanilla remove formula
        sbyte level = __instance.BuildingBlockLevel(blockKey);
        var taiwu = DomainManager.Taiwu.GetTaiwu();
        for (sbyte b = 0; b < 8; b++)
        {
            int resourceReturn = SharedMethods.GetResourceReturnOfRemoveBuilding(configData, level, b, blockData);
            if (resourceReturn > 0)
            {
                taiwu.ChangeResource(context, b, resourceReturn);
            }
        }

        // Return building core item.
        // Not part of usual demolition process. Here as a cheat.
        if (configData.BuildingCoreItem != -1)
        {
            DomainManager.Taiwu.ReturnBuildingCoreItem(context, configData);
        }

        __instance.BuildingRemoveVillagerWork(context, blockKey);

        __instance.BuildingRemoveResident(context, blockData, blockKey);

        // Reference for many of the following calls: BuildingDomain.ComplementUpdateBuilding
        __instance.ClearBuildingBlockEarningsData(context, blockKey, blockData.TemplateId == 222);
        DomainManager.Extra.TryRemoveAutoWorkBlockIndexList(context, blockKey.BuildingBlockIndex);
        DomainManager.Extra.TryRemoveAutoSoldBlockIndexList(context, blockKey.BuildingBlockIndex);
        if (__instance.TryGetElement_MakeItemDataDict(blockKey, out var _))
        {
            __instance.RemoveElement_MakeItemDataDict(blockKey, context);
        }
        __instance._shopEventCollections?.Remove(blockKey);
        if (__instance._CollectBuildingResourceType.ContainsKey(blockKey))
        {
            __instance.RemoveElement_CollectBuildingResourceType(blockKey, context);
        }

        __instance.ResetAllChildrenBlocks(context, blockKey, 0, -1);
        __instance.SetBuildingCustomName(context, blockKey, null);

        __instance.OnBuildingRemoved(context, blockKey);

        __instance.UpdateTaiwuVillageBuildingEffect();

        __result = (blockKey.BuildingBlockIndex, __instance.GetElement_BuildingBlocks(blockKey));
        return false;
    }
}
