using GameData.Domains;
using GameData.Domains.Character;
using HarmonyLib;

namespace Tweaks.Patches;

// Last-resort retroactive removal for existing saves.
// Destructive. Changes are saved to archive and cannot be undone by toggling the patch off.
//
// Intentional transgender characters in the game:
// - Template 349 (童身老怪) ConfigData.Character.CreateItems5() (NpcFace_tongshenlaoguai) in GameData.Shared.dll
// - Story mirror character (EventHelper.CreateMirrorCharacter)
// - Fulong loyal servant (EventHelper.FulongServantSetTransgender)
// - Samsara and saved soul reincarnations
// - Custom Taiwu protagonist creation (OfflineCreateProtagonist)
//
// Note: Event characters like Fulong servants and Mirror characters use standard templates (CreatingType == 1) and
// cannot be distinguished from random NPCs in saved archives; they may be affected by this retroactive removal.
//
// Flipped portraits for _transgender characters after the state is set comes from:
// Character.OfflineCreateAttractionAndAvatar
//      AvatarManager.Instance.GetRandomAvatar
[HarmonyPatchCategory(nameof(PatchNoTransgender))]
static class PatchNoTransgender
{
    internal static bool _enabled;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), nameof(Character.Deserialize))]
    static void NoTransgender_Postfix(Character __instance) => RemoveTransgender(__instance);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.Serialize))]
    [HarmonyPatch(typeof(Character), nameof(Character.GetAvatar))]
    static void NoTransgender_Prefix(Character __instance) => RemoveTransgender(__instance);

    static bool ShouldRemoveTransgender(Character character)
    {
        if (!character._transgender)
            return false;

        if (DomainManager.Taiwu != null && character.IsTaiwu())
            return false;

        if (character.GetCreatingType() != 1)
            return false;

        // Intentional transgender character templates
        var charCfg = Config.Character.Instance.GetItemOrDefault(character.GetTemplateId());
        if (charCfg != null && charCfg.Transgender)
            return false;

        return true;
    }

    static void RemoveTransgender(Character character)
    {
        if (!_enabled || !ShouldRemoveTransgender(character))
            return;

        character._transgender = false;
        character._avatar?.ChangeGender(character._gender);
    }
}
