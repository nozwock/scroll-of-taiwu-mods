using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Game.Views.Building;
using Game.Views.Building.BuildingAreaQuickActionMenu.LeftButton;
using Game.Views.Building.BuildingManage;
using GameData.Domains;
using GameData.Domains.Building;
using GameData.GameDataBridge;
using GameData.Serializer;
using GameData.Utilities;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tweaks.Patches;


// BuildingDomain
//      .Remove()
//          .SetVillageBuildWorkAndBlockData()
//      .OfflineUpdateOperation()
//
// Don't need to patch the following for the actual removal logic since we already patch the method
// `BuildingDomain.Remove` they call:
//      ViewBuildingArea.ConfirmMultiplyRemove
//      BuildingManageSubPageNewRemove.OnClickStartRemove
//      QuickRemoveProcessor.OnClick
[HarmonyPatchCategory]
static class PatchInstantRemoveBuilding
{
    internal static bool _enabled;

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
    // (SetStopOperation), silently dropping Remove and GmCmd_RemoveBuildingImmediately.
    // Catch them to update visual blocks and roads immediately when backend removal finishes.
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
                && (notification.MethodId == BuildingDomainHelper.MethodIds.Remove
                    || notification.MethodId == BuildingDomainHelper.MethodIds.GmCmd_RemoveBuildingImmediately)
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

    [HarmonyReversePatch]
    [HarmonyPatch(
        typeof(BuildingManageSubPageNewRemove),
        nameof(BuildingManageSubPageNewRemove.OnClickStartRemove)
    )]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void OriginalOnClickStartRemove(BuildingManageSubPageNewRemove __instance)
    {
        throw new NotImplementedException();
    }

    // Prompt for confirmation before instant demolition to prevent accidental miss-clicks.
    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(BuildingManageSubPageNewRemove),
        nameof(BuildingManageSubPageNewRemove.OnClickStartRemove)
    )]
    static bool BuildingManageSubPageNewRemove_OnClickStartRemove_Prefix(
        BuildingManageSubPageNewRemove __instance
    )
    {
        string buildingName = __instance.ConfigData != null ? __instance.ConfigData.Name : "";
        string title = LanguageKey.LK_Common_Attention.Tr();
        string content = string.IsNullOrEmpty(buildingName)
            ? LanguageKey.LK_Building_ConfirmOperate.TrFormat(LanguageKey.LK_Building_Demolish.Tr())
            : LanguageKey.LK_Building_ConfirmOperate.TrFormat(
                $"{LanguageKey.LK_Building_Demolish.Tr()}: {buildingName}"
            );

        CommonUtils.ShowConfirmDialog(title, content, () => OriginalOnClickStartRemove(__instance));
        return false;
    }

    // Enable "Demolish" button in building details subpage without requiring assigned workers and hide unnecessary UI
    // elements for instant demolition.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BuildingManageSubPageNewRemove), nameof(BuildingManageSubPageNewRemove.UpdateButtons))]
    static void BuildingManageSubPageNewRemove_UpdateButtons_Postfix(
        BuildingManageSubPageNewRemove __instance
    )
    {
        if (__instance.IsRemove && __instance.BlockData.OperationType == -1)
        {
            if (__instance.IsBuildingManagementUnlocked)
            {
                __instance.confirmBtn.interactable = true;
                TMP_Text componentInChildren = __instance.confirmBtn.GetComponentInChildren<TMP_Text>();
                componentInChildren?.color = "#fff5dd".HexStringToColor();
            }
        }

        UpdateSubPageElements(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BuildingManageSubPageNewRemove), nameof(BuildingManageSubPageNewRemove.OnEnable))]
    static void BuildingManageSubPageNewRemove_OnEnable_Postfix(
        BuildingManageSubPageNewRemove __instance
    )
    {
        UpdateSubPageElements(__instance);
    }

    static void UpdateSubPageElements(BuildingManageSubPageNewRemove subPage, bool reset = false)
    {
        if (subPage == null || subPage.gameObject == null)
            return;

        var personArea = subPage.transform.Find("Viewport/Content/PersonArea")?.gameObject;

        var hideUi = !reset
            && subPage.confirmBtn != null
            && subPage.confirmBtn.gameObject.activeSelf;

        var changed = false;
        if (hideUi)
        {
            if (subPage.progressBg != null && subPage.progressBg.activeSelf)
            {
                subPage.progressBg.SetActive(false);
                changed = true;
            }
            if (subPage.progressTitle?.gameObject?.transform.parent?.gameObject is { } titleRoot
                && titleRoot.activeSelf)
            {
                titleRoot.SetActive(false);
                changed = true;
            }
            if (personArea != null && personArea.activeSelf)
            {
                personArea.SetActive(false);
                changed = true;
            }
        }
        else
        {
            if (subPage.progressBg != null && !subPage.progressBg.activeSelf)
            {
                subPage.progressBg.SetActive(true);
                changed = true;
            }
            if (subPage.progressTitle?.gameObject?.transform.parent?.gameObject is { } titleRoot
                && !titleRoot.activeSelf)
            {
                titleRoot.SetActive(true);
                changed = true;
            }
            if (personArea != null && !personArea.activeSelf)
            {
                personArea.SetActive(true);
                changed = true;
            }
        }

        if (changed)
        {
            var content = subPage.transform.Find("Viewport/Content") as RectTransform;
            if (content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }
    }

    internal static void OnDisable()
    {
        var buildingManage = UIElement.BuildingManage.UiBase as ViewBuildingManage;
        if (buildingManage == null)
            return;

        var subPages = buildingManage.GetComponentsInChildren<BuildingManageSubPageNewRemove>(true);
        foreach (var subPage in subPages)
        {
            UpdateSubPageElements(subPage, reset: true);
        }
    }
}
