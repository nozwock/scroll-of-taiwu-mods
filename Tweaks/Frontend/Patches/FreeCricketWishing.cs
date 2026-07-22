using Game.Views.Building;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatch]
static class PatchFreeCricketWishing
{
    // ViewCricketWishing
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewCricketCollection), nameof(ViewCricketCollection.OnInit))]
    static void ViewCricketWishing_OnInit_Prefix()
    {
        GlobalConfig.Instance.CricketWishingCostLuckPoint = 0;
    }
}
