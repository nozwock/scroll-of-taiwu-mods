using GameData.Common;
using GameData.Domains;
using GameData.Domains.Building;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchInfiniteCaravanProvisions))]
static class PatchInfiniteCaravanProvisions
{
    internal static bool _enabled;

    internal static void TryRecoverCaravanProvisions(DataContext? context = null)
    {
        var data = DomainManager.Building.GetTeaHorseCaravanData();
        if (!IsCaravanActive(data))
            return;

        context ??= DataContextManager.GetCurrentThreadDataContext();
        if (context == null)
            return;

        data.CaravanReplenishment = TeaHorseCaravanData.CaravanReplenishmentInitValue;
        data.LackReplenishmentTurn = 0;
        DomainManager.Building.SetTeaHorseCaravanData(data, context);
    }

    static bool IsCaravanActive(TeaHorseCaravanData? data)
    {
        return data != null
            && data.CaravanState >= BuildingDomain.TeaHorseCaravanState.Ready
            && data.CaravanState != BuildingDomain.TeaHorseCaravanState.ReadyGetItem;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BuildingDomain), nameof(BuildingDomain.UpdateTeaHorseCaravan))]
    static void UpdateTeaHorseCaravan_Prefix(BuildingDomain __instance)
    {
        var data = __instance.GetTeaHorseCaravanData();
        if (!IsCaravanActive(data))
            return;

        data.CaravanReplenishment = TeaHorseCaravanData.CaravanReplenishmentInitValue;
        data.LackReplenishmentTurn = 0;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BuildingDomain), nameof(BuildingDomain.UpdateTeaHorseCaravan))]
    static void UpdateTeaHorseCaravan_Postfix(DataContext context)
    {
        TryRecoverCaravanProvisions(context);
    }
}
