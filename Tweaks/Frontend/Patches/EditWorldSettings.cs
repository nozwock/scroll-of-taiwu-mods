using GameData.GameDataBridge;
using HarmonyLib;

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
}
