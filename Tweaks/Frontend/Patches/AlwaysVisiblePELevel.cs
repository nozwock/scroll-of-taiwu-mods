using HarmonyLib;
using UICommon.Character;

namespace Tweaks.Patches;

[HarmonyPatchCategory]
static class PatchAlwaysVisiblePELevel
{
    internal static bool _enabled;

    // ViewEventWindow
    //      EventWindowCharacter
    //          .ConsummateLevelIcon
    //          .GetRightForbiddenConsummateLevel()
    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(EventWindowCharacter),
        nameof(EventWindowCharacter.GetRightForbiddenConsummateLevel)
    )]
    static void EventWindowCharacter_GetRightForbiddenConsummateLevel_Prefix(
        EventWindowCharacter __instance
    )
    {
        __instance.Data?.ExtraData.RightForbiddenConsummateLevel = false;
    }

    // Also used in:
    // ViewCombatBegin
    //      ConsummateIcon
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharacterConsummateLevel), nameof(CharacterConsummateLevel.FillElement))]
    static void CharacterConsummateLevel_FillElement_Prefix(CharacterConsummateLevel __instance)
    {
        var self = __instance;
        self.Item.CreatingType = 1;
    }
}
