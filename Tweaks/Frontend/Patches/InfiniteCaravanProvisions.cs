using Game.Views.Buildings;
using GameData.Domains.Building;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory]
static class PatchInfiniteCaravanProvisions
{
    internal static bool _enabled;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TeaHorseCaravanData), nameof(TeaHorseCaravanData.GetReplenishmentCost))]
    static void GetReplenishmentCost_Postfix(ref int __result)
    {
        __result = 0;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewTeaHorseCaravan), nameof(ViewTeaHorseCaravan.RefreshText))]
    static void ViewTeaHorseCaravan_RefreshText_Postfix(ViewTeaHorseCaravan __instance)
    {
        var caravanData = __instance._teaHorseCaravanData;
        if (caravanData == null)
            return;

        var caravanState = caravanData.CaravanState;
        if (caravanState >= ViewTeaHorseCaravan.TeaHorseCaravanState.Ready
            && caravanState != ViewTeaHorseCaravan.TeaHorseCaravanState.ReadyGetItem)
        {
            __instance.weatherChangeReplenishmentText.text = "0";
            __instance.replenishmentText.text = LanguageKey.LK_TeaHorse_Replenishment.TrFormat(
                caravanData.CaravanReplenishment,
                TeaHorseCaravanData.CaravanReplenishmentInitValue,
                (short)0
            );
            __instance.replenishmentText.GetComponent<TMPTextSpriteHelper>().Parse();
        }
    }
}
