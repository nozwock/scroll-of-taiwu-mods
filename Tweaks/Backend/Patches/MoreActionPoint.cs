using GameData.Domains.World;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchMoreActionPoint))]
static class PatchMoreActionPoint
{
    internal static bool _enabled;

    // WorldDomain
    //      .ActionPointMax
    //          .IsChallengeModeEnabled()
    //      .ActionPointRecovery
    //      .IsChallengeModeEnabled()
    //          ChallengeModeData.IsEnabled()
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WorldDomain), nameof(WorldDomain.ApplyChallengeModeMoreActionPointMax))]
    static bool WorldDomain_ApplyChallengeModeMoreActionPointMax_Prefix(ref int __result)
    {
        __result = GlobalConfig.Instance.MoreActionPointLimitPerMonth;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(WorldDomain),
        nameof(WorldDomain.ApplyChallengeModeMoreActionPointRecovery)
    )]
    static bool WorldDomain_ApplyChallengeModeMoreActionPointRecovery_Prefix(ref int __result)
    {
        __result = GlobalConfig.Instance.MoreActionPointRecoveryPerMonth;
        return false;
    }
}
