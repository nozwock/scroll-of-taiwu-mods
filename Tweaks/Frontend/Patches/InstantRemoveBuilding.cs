using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Game.Views.Building;
using Game.Views.Building.BuildingAreaQuickActionMenu.LeftButton;
using GameData.Domains;
using GameData.Domains.Building;
using GameData.GameDataBridge;
using GameData.Serializer;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory]
static class PatchInstantRemoveBuilding
{
    internal static bool _enabled;

    // BuildingDomain
    //      .Remove()
    //          .SetVillageBuildWorkAndBlockData()
    //      .OfflineUpdateOperation()
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ViewBuildingArea), nameof(ViewBuildingArea.ConfirmMultiplyRemove))]
    static IEnumerable<CodeInstruction> ViewBuildingArea_ConfirmMultiplyRemove_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = instructions.ToList();

        var target = AccessTools.Method(
            typeof(BuildingDomainMethod.Call),
            nameof(BuildingDomainMethod.Call.Remove)
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(target))
            {
                // Don't like replacing original instruction but oh well...
                codes[i].operand = AccessTools.Method(
                    typeof(BuildingDomainMethod.Call),
                    nameof(BuildingDomainMethod.Call.GmCmd_RemoveBuildingImmediately)
                );
                codes.Insert(i++, new(OpCodes.Pop)); // Takes care of `int[] workers` on the evaluation stack
                break;
            }
        }

        return codes.AsEnumerable();
    }

    // Bypass idle worker requirement when selecting buildings for multi-demolition.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewBuildingArea), nameof(ViewBuildingArea.SetBlockCannotRemove))]
    static void ViewBuildingArea_SetBlockCannotRemove_Prefix(ref bool workerAvailable)
    {
        workerAvailable = true;
    }

    // Enable quick action menu demolition without requiring available idle workers.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(QuickRemoveProcessor), nameof(QuickRemoveProcessor.CanInteract))]
    static bool QuickRemoveProcessor_CanInteract_Prefix(
        QuickRemoveProcessor __instance,
        ref bool __result
    )
    {
        __result = __instance.IsVisible();
        return false;
    }

    // ViewBuildingArea.OnNotifyGameData only handles Method 36 (GmCmd_BuildImmediately), 47 (Build), and 49
    // (SetStopOperation), silently dropping GmCmd_RemoveBuildingImmediately.
    // Catch GmCmd_RemoveBuildingImmediately to update visual blocks and roads immediately when backend removal
    // finishes.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewBuildingArea), nameof(ViewBuildingArea.OnNotifyGameData))]
    static void ViewBuildingArea_OnNotifyGameData_Prefix(
        ViewBuildingArea __instance,
        List<NotificationWrapper> notifications
    )
    {
        foreach (var wrapper in notifications)
        {
            var notification = wrapper.Notification;
            if (
                notification.Type == NotificationType.MethodReturn
                && notification.DomainId == DomainHelper.DomainIds.Building
                && notification.MethodId == BuildingDomainHelper.MethodIds.GmCmd_RemoveBuildingImmediately
            )
            {
                (short, BuildingBlockData) buildingData = (0, new());
                Serializer.Deserialize(wrapper.DataPool, notification.ValueOffset, ref buildingData);
                __instance.UpdateBuildingData(
                    new(__instance._areaId, __instance._blockId, buildingData.Item1),
                    buildingData.Item2
                );
                __instance.UpdateRoad();
            }
        }
    }
}
