using GameData.Domains.Character;
using HarmonyLib;

namespace Tweaks.Patches;

// Destructive. Changes are persisted and transgender and avatar gender state cannot be restored by disabling the patch.
[HarmonyPatchCategory(nameof(PatchNoTransgender))]
static class PatchNoTransgender
{
    internal static bool _enabled;

    // Patches for all different constructors/initializers
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.OfflineSetGenderInfo))]
    static void Character_OfflineSetGenderInfo_Prefix(ref bool transgender) => transgender = false;

    // This is when creating a new character I think?
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), nameof(Character.OfflineCreateProtagonist))]
    static void Character_OfflineCreateProtagonist_Postfix(Character __instance)
    {
        var self = __instance;
        // Prioritizing inscribed character's portrait for gender stat
        if (self._transgender)
        {
            self._transgender = false;
            self._gender = self._avatar.Gender;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), MethodType.Constructor, [typeof(short)])]
    [HarmonyPatch(typeof(Character), nameof(Character.OfflineInheritCrossArchiveCharacter))]
    static void SimpleNoTransgender_Postfix(Character __instance) =>
        __instance._transgender = false;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), nameof(Character.Deserialize))]
    static void NoTransgender_Postfix(Character __instance) => RemoveTransgender(__instance);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.Serialize))]
    [HarmonyPatch(typeof(Character), nameof(Character.GetAvatar))]
    [HarmonyPatch(typeof(Character), nameof(Character.OfflineCreateAttractionAndAvatar))]
    static void NoTransgender_Prefix(Character __instance) => RemoveTransgender(__instance);

    static void RemoveTransgender(Character character)
    {
        if (character._transgender)
        {
            character._transgender = false;
            character._avatar.ChangeGender(character._gender);
        }
    }
}
