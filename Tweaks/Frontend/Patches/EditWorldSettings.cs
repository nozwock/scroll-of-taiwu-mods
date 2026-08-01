using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ViewLegacy), nameof(ViewLegacy.RequestData))]
    static IEnumerable<CodeInstruction> ViewLegacy_RequestData_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = instructions.ToList();
        var ctor = AccessTools.Constructor(
            typeof(AsyncMethodCallbackDelegate),
            [typeof(object), typeof(IntPtr)]
        );

        var updateLabelAfterRefresh = Transpilers.EmitDelegate(
            static (AsyncMethodCallbackDelegate action) =>
            {
                var self = (ViewLegacy)action.Target;
                action += (_, _) =>
                {
                    var isManuallyOpened = !self._inherit && !self._crossArchive;
                    if (isManuallyOpened)
                    {
                        // Or UI_Reset_World_Config_Title
                        self.confirm.GetComponentInChildren<TextMeshProUGUI>().text =
                            LanguageKey.GM_EditWorldCreationInfo_Name.Tr();
                    }
                };

                return action;
            }
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (
                codes[i].opcode == OpCodes.Newobj
                && codes[i].operand is ConstructorInfo ctorCode
                && ctorCode == ctor
            )
            {
                codes.Insert(i + 1, updateLabelAfterRefresh);
                break;
            }
        }

        return codes.AsEnumerable();
    }
}
