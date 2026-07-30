using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory]
static class PatchMoreActionPoint
{
    internal static bool _enabled;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TimeManager), nameof(TimeManager.ActionPointMax), MethodType.Getter)]
    static bool TimeManager_get_ActionPointMax_Prefix(ref int __result)
    {
        __result = GlobalConfig.Instance.MoreActionPointLimitPerMonth;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TimeManager), nameof(TimeManager.ActionPointRecovery), MethodType.Getter)]
    static bool TimeManager_get_ActionPointRecovery_Prefix(ref int __result)
    {
        __result = GlobalConfig.Instance.MoreActionPointRecoveryPerMonth;
        return false;
    }
}
