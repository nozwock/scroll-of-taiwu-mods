using Game.Views.NewGame;
using HarmonyLib;

[HarmonyPatch]
static class PatchBackgroundTraitLimit
{
    internal static int _backgroundTraitLimit = 0;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NewGameSubPageFeature), nameof(NewGameSubPageFeature.OnEnable))]
    static void NewGameSubPageFeature_OnEnable_Postfix(NewGameSubPageFeature __instance)
    {
        var self = __instance;
        if (_backgroundTraitLimit > 0)
            self.MaxPoints = _backgroundTraitLimit;
    }
}
