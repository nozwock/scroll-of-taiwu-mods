using GameData.Domains.Taiwu;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatch]
static class PatchBonusMaxTeammate
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.GetTaiwuGroupMaxCount))]
    static void TaiwuDomain_GetTaiwuGroupMaxCount_Prefix(ref int __result)
    {
        __result += Plugin.bonusMaxTeammate;
    }
}
