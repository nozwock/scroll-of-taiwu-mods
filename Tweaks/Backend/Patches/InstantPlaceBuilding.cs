using System.Collections.Generic;
using System.Reflection.Emit;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Building;
using GameData.Domains.Map;
using GameData.Domains.Organization;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchInstantPlaceBuilding))]
static class PatchInstantPlaceBuilding
{
    internal static bool _enabled;

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BuildingDomain), nameof(BuildingDomain.Build))]
    static IEnumerable<CodeInstruction> BuildingDomain_Build_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator il
    )
    {
        var codes = new List<CodeInstruction>(instructions);

        var methodSetVillageBuildWork = AccessTools.Method(
            typeof(BuildingDomain),
            nameof(BuildingDomain.SetVillageBuildWork)
        );
        var fieldOperationType = AccessTools.Field(
            typeof(BuildingBlockData),
            nameof(BuildingBlockData.OperationType)
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(methodSetVillageBuildWork))
            {
                var skipTarget = il.DefineLabel();
                codes[i + 1].labels.Add(skipTarget);

                var br = new CodeInstruction(OpCodes.Br_S, skipTarget);
                br.MoveLabelsFrom(codes[i]);
                CodeInstruction[] insertedCodes = [
                    new(OpCodes.Pop),
                    new(OpCodes.Pop),
                    new(OpCodes.Pop),
                    new(OpCodes.Pop),
                    br
                ];
                codes.InsertRange(i, insertedCodes);
                i += insertedCodes.Length;
            }
            else if (codes[i].StoresField(fieldOperationType))
            {
                codes.InsertRange(i, [new(OpCodes.Pop), new(OpCodes.Ldc_I4_M1)]);
                i += 2;
            }
        }

        return codes;
    }

    // ref: BuildingDomain.CompleteBuilding
    //      BuildingDomain.OfflineUpdateOperation
    //      BuildingDomain.ComplementUpdateBuilding
    //      BuildingDomain.TriggerBuildingCompleteEvents
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
        var element_BuildingBlocks = __result.Item2;
        element_BuildingBlocks.UnlockLevelSlot(0);

        __instance.CompleteBuilding(context, blockKey, element_BuildingBlocks);

        Settlement? settlement = DomainManager.Organization.GetSettlementByLocation(
            new Location(blockKey.AreaId, blockKey.BlockId)
        );
        short settlementId = settlement?.GetId() ?? DomainManager.Taiwu.GetTaiwuVillageSettlementId();
        DomainManager.World
            .GetMonthlyNotificationCollection()
            .AddBuildingConstructionCompleted(settlementId, buildingTemplateId);

        __instance._newCompleteOperationBuildings.Add(blockKey);

        var modification = new ParallelBuildingModification
        {
            BlockKey = blockKey,
            BlockData = element_BuildingBlocks,
            AddBuilding = true,
            BuildingOperationComplete = true,
        };
        __instance.ComplementUpdateBuilding(context, modification);

        __instance.TriggerBuildingCompleteEvents(context);
    }
}
