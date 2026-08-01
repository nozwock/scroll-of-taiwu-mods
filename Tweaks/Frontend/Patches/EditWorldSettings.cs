using Game.Views.LegacyPassing;
using GameData.GameDataBridge;
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
    // ViewLegacy.RequestData()
    //      .Refresh()
    //      Element.ShowAfterRefresh() // There was no need to run code after ShowAfterRefresh() -_-
    // No need for transpilers, getting delegate method with heuristics, etc.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewLegacy), nameof(ViewLegacy.Refresh))]
    static void ViewLegacy_Refresh_Postfix(ViewLegacy __instance)
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
