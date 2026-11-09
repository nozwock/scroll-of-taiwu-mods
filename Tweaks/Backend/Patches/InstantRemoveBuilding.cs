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

        // Return building core item.
        // Not part of usual demolition process. Here as a cheat.
        if (configData.BuildingCoreItem != -1)
        {
            DomainManager.Taiwu.ReturnBuildingCoreItem(context, configData);
        }

        __instance.BuildingRemoveVillagerWork(context, blockKey);
        __instance.BuildingRemoveResident(context, blockData, blockKey);

        // ref: BuildingDomain.OfflineUpdateOperation
        var modification = new ParallelBuildingModification
        {
            BlockKey = blockKey,
            BlockData = blockData,
            RemoveMakeItemData = true,
            RemoveEventBookData = true,
            RemoveResidence = true,
            FreeShopManager = true,
            RemoveCollectResourceType = __instance._CollectBuildingResourceType.ContainsKey(blockKey),
            ResetAllChildrenBlocks = true,
            BuildingOperationComplete = true,
        };

        // Refund materials according to vanilla remove formula
        sbyte level = __instance.BuildingBlockLevel(blockKey);
        for (sbyte b = 0; b < 8; b++)
        {
            int resourceReturn = SharedMethods.GetResourceReturnOfRemoveBuilding(
                configData,
                level,
                b,
                blockData
            );
            modification.SetResource(b, resourceReturn);
        }

        __instance.ComplementUpdateBuilding(context, modification);
        __instance.OnBuildingRemoved(context, blockKey);

        // Note: We explicitly do not send a monthly notification because this is an instant action rather than a
        // month-end event.
        // In vanilla it's done via:
        // DomainManager.World.GetMonthlyNotificationCollection().AddBuildingDemolitionCompleted

        __result = (blockKey.BuildingBlockIndex, __instance.GetElement_BuildingBlocks(blockKey));
        return false;
    }
}
