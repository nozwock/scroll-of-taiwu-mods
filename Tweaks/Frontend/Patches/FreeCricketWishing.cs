using Game.Views.Building;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory]
static class PatchFreeCricketWishing
{
    internal static bool _enabled;

    // ViewCricketWishing
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewCricketCollection), nameof(ViewCricketCollection.OnInit))]
    static void ViewCricketWishing_OnInit_Prefix()
    {
        GlobalConfig.Instance.CricketWishingCostLuckPoint = 0;
    }
}
