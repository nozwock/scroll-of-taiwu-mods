using GameData.Domains.Taiwu;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchFreeCricketWishing))]
static class PatchFreeCricketWishing
{
    internal static bool _enabled;

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

    // Retroactively fix persisted negative value
    // GetCricketLuckPoint is used for frontend, etc. GetCricketCollectionDisplayData()
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.GetCricketLuckPoint))]
    static void TaiwuDomain_GetCricketLuckPoint_Prefix(TaiwuDomain __instance, ref int __result)
    {
        if (__result < 0)
        {
            __instance._cricketLuckPoint = 0;
            __result = 0;
        }
    }
}
