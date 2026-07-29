using Game.Views.EventWindow;
using HarmonyLib;

[HarmonyPatchCategory]
static class PatchLeftAlignedEventText
{
    internal static bool _enabled;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewEventWindow), nameof(ViewEventWindow.Awake))]
    static void ViewEventWindow_Awake_Postfix(ViewEventWindow __instance)
    {
        var self = __instance;
        // It is center by default. At least it is so at the moment of writing.
        self.eventContent.alignment = TMPro.TextAlignmentOptions.Left;
    }
}
