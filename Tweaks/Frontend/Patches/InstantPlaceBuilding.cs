using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using Game.Views.Building;
using GameData.Utilities;
using HarmonyLib;

namespace Tweaks.Patches;


// ViewBuildingArea.StartPlacingBuilding
//      ViewBuildingArea.ConfirmBuild
//          BuildingDomain.GmCmd_BuildImmediately (if InstantBuild)
//          BuildingDomain.Build (else) (Tutorial is handled only in this branch)
//
// StartPlacingBuilding doesn't need to be patched due to patched BuildingDomain.Build
[HarmonyPatchCategory]
static class PatchInstantPlaceBuilding
{
    internal static bool _enabled;

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ViewBuildingOverview), nameof(ViewBuildingOverview.CanExecuteBuild))]
    static IEnumerable<CodeInstruction> ViewBuildingOverview_CanExecuteBuild_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = new List<CodeInstruction>(instructions);

        // this._operatorListCached.Exist()
        var target = SymbolExtensions.GetMethodInfo(
            () => ArrayExtensions.Exist((IReadOnlyList<int>)null!, null!)
        );

        var patched = false;
        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(target))
            {
                codes.InsertRange(i + 1, [new(OpCodes.Pop), new(OpCodes.Ldc_I4_1)]);
                patched = true;
                break;
            }
        }

        Debug.Assert(patched);

        return codes;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewBuildingOverview), nameof(ViewBuildingOverview.SetOperationLeftTime))]
    static bool ViewBuildingOverview_SetOperationLeftTime_Prefix(ViewBuildingOverview __instance)
    {
        __instance.timeCost.text = "0";
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewBuildingOverview), nameof(ViewBuildingOverview.UpdateConfirmButton))]
    [HarmonyPatch(typeof(ViewBuildingOverview), nameof(ViewBuildingOverview.OnEnable))]
    static void ViewBuildingOverview_UpdateElements_Postfix(ViewBuildingOverview __instance)
    {
        UpdateOverviewElements(__instance);
    }

    static void UpdateOverviewElements(ViewBuildingOverview overview, bool reset = false)
    {
        if (overview == null || overview.gameObject == null || overview._configData == null)
            return;

        var hide = !reset;

        if (hide)
        {
            if (overview.timeCostEstimate != null && overview.timeCostEstimate.activeSelf)
            {
                overview.timeCostEstimate.SetActive(false);
            }
            if (overview.operationInfo != null && overview.operationInfo.activeSelf)
            {
                overview.operationInfo.SetActive(false);
            }
        }
        else
        {
            var isLocked = overview.unlockTips != null && overview.unlockTips.activeSelf;
            if (!isLocked)
            {
                if (overview.timeCostEstimate != null && !overview.timeCostEstimate.activeSelf)
                {
                    overview.timeCostEstimate.SetActive(true);
                }
                if (overview.operationInfo != null && !overview.operationInfo.activeSelf)
                {
                    overview.operationInfo.SetActive(true);
                }
            }
        }
    }

    internal static void OnDisable()
    {
        var overview = UIElement.BuildingOverview.UiBase as ViewBuildingOverview;
        if (overview != null)
        {
            UpdateOverviewElements(overview, reset: true);
            if (overview._configData != null)
            {
                overview.UpdateOperatorInfo();
                overview.UpdateConfirmButton();
            }
        }
    }
}
