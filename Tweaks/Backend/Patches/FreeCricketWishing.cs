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

    // Fix _cricketLuckPoint being allowed to be set to negative value
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.SetCricketLuckPoint))]
    static void TaiwuDomain_SetCricketLuckPoint_Prefix(ref int value)
    {
        if (value < 0)
            value = 0;
    }
}
