using GameData.Domains.Character;
using GameData.Domains.TaiwuEvent.EventHelper;
using HarmonyLib;

// Wug King -> Prime Gu
// Makes it so, using the sect function also removes any Prime Gu
[HarmonyPatchCategory(nameof(PatchCleanseWugKing))]
static class PatchCleanseWugKing
{
    internal static bool _enabled;

    // EventHelper.ApplyEffect_WuShengMiYu - The sect function of 5 Immortals Sect
    //      EventHelper.ChangeWugKingDurations
    [HarmonyPrefix]
    [HarmonyPatch(typeof(EventHelper), nameof(EventHelper.ChangeWugKingDurations))]
    static void EventHelper_ChangeWugKingDurations_Prefix(ref short deltaDuration)
    {
        deltaDuration = short.MinValue;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.ChangeFeatureByWugKing))]
    static bool Character_ChangeFeatureByWugKing_Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}
