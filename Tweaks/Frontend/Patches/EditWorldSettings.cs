using System.Reflection;
using Common.Extensions;
using Game.Views.LegacyPassing;
using GameData.GameDataBridge;
using GameData.Utilities;
using HarmonyLib;
using TMPro;

namespace Tweaks.Patches;

// The confirm button ViewLegacy.confirm ("Taiwu Inheritance") is what'll actually save the changes.
// Text key is LK_HotKeyGroup_MainInterfaceFunction_TaiwuLegacy.
[HarmonyPatchCategory]
static class PatchEditWorldSettings
{
    internal static bool _enabled;

    // ViewLegacy
    //      NewGameSubPageWorldDetailPanel .worldDetailPanel
    //          .RefreshInteractable()
    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(GlobalOperations),
        nameof(GlobalOperations.CanResetWorldSettings),
        MethodType.Getter
    )]
    static bool GlobalOperations_get_CanResetWorldSettings_Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }

    // Updating the text for ViewLegacy.confirm in ViewLegacy.OnInit() doesn't work since Element.ShowAfterRefresh()
    // inside ViewLegacy.RequestData() refreshes the UI with translated texts.
    [HarmonyPatch]
    static class ViewLegacy_RequestData_delegate_Postfix
    {
        static MethodBase TargetMethod() =>
            typeof(ViewLegacy).GetLocalMethod(
                $"<{nameof(ViewLegacy.RequestData)}>",
                [typeof(int), typeof(RawDataPool)],
                isRootMethod: true
            );

        static void Postfix(ViewLegacy __instance)
        {
            var self = __instance;

            var isManuallyOpened = !self._inherit && !self._crossArchive;
            if (isManuallyOpened)
            {
                // Or UI_Reset_World_Config_Title
                self.confirm.GetComponentInChildren<TextMeshProUGUI>().text =
                    LanguageKey.GM_EditWorldCreationInfo_Name.Tr();
            }
        }
    }
}
