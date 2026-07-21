using Game.Views.Cricket;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatch]
static class PatchFreeCricketWishing
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewCricketWishing), nameof(ViewCricketWishing.OnInit))]
    static void ViewCricketWishing_OnInit_Prefix()
    {
        GlobalConfig.Instance.CricketWishingCostLuckPoint = 0;
    }
}
