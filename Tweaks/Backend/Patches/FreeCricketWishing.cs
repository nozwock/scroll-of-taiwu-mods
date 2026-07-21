using GameData.Domains.Taiwu;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatch]
static class PatchFreeCricketWishing
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.CricketRoomWishingCricket))]
    static void TaiwuDomain_CricketRoomWishingCricket_Prefix()
    {
        GlobalConfig.Instance.CricketWishingCostLuckPoint = 0;
    }
}
