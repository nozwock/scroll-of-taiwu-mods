using GameData.Domains.Taiwu;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatch]
static class PatchBonusMaxTeammate
{
    internal static int _bonusMaxTeammate = 20;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.GetTaiwuGroupMaxCount))]
    static void TaiwuDomain_GetTaiwuGroupMaxCount_Prefix(ref int __result)
    {
        __result += _bonusMaxTeammate;
    }
}
