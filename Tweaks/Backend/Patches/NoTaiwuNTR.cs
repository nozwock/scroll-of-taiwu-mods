using System.Linq;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Character.Ai;
using GameData.Domains.Character.Relation;
using HarmonyLib;

namespace Tweaks.Patches;

// ref https://steamcommunity.com/sharedfiles/filedetails/?id=3749698139
// The above at the moment of writing only checked for Taiwu -> Target RelationType.Adored which I guess is fine since
// even if HusbandOrWife relation is present, Adored would still exist.
// Plus, I wanted some more functionality, hence this patch.
[HarmonyPatchCategory(nameof(PatchNoTaiwuNTR))]
static class PatchNoTaiwuNTR
{
    internal static bool _enabled;

    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(AiHelper.Relation),
        nameof(AiHelper.Relation.GetStartRelationSuccessRate_SexRelationBaseRate)
    )]
    static bool AiHelper_Relation_GetStartRelationSuccessRate_SexRelationBaseRate_Prefix(
        Character selfChar,
        Character targetChar,
        ref int __result
    )
    {
        if (BlockRelation(selfChar, targetChar))
        {
            __result = int.MinValue;
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AiHelper.Relation), nameof(AiHelper.Relation.GetStartOrEndRelationChance))]
    static bool AiHelper_Relation_GetStartOrEndRelationChance_Prefix(
        Character selfChar,
        Character targetChar,
        ref int __result
    )
    {
        if (BlockRelation(selfChar, targetChar))
        {
            __result = 0;
            return false;
        }

        return true;
    }

    // In some cases the invocation of this function is gated behind GetStartRelationSuccessRate_SexRelationBaseRate,
    // other times not. So just to be safe.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyAddRelation_Adore))]
    static bool Character_ApplyAddRelation_Adore_Prefix(Character selfChar, Character targetChar) =>
        !BlockRelation(selfChar, targetChar);

    static bool BlockRelation(Character selfChar, Character targetChar)
    {
        var taiwu = DomainManager.Taiwu.GetTaiwu();
        if (taiwu._id == selfChar._id || taiwu._id == targetChar._id)
        {
            return false;
        }

        // Adored is "Lover"
        var selfIsTaiwuLover = DomainManager.Character.HasRelation(
            taiwu._id,
            selfChar._id,
            RelationType.Adored | RelationType.HusbandOrWife
        );
        var targetIsTaiwuLover = DomainManager.Character.HasRelation(
            taiwu._id,
            targetChar._id,
            RelationType.Adored | RelationType.HusbandOrWife
        );
        if (selfIsTaiwuLover || targetIsTaiwuLover)
        {
            // If loved BY the Taiwu
            // Doesn't care for if the self/target is in relation with other(s) or each other already
            return true;
        }

        // TODO: This needs to be gated behind an option since it's much more broad
        if (IsRomancableCompanion(taiwu, selfChar) || IsRomancableCompanion(taiwu, targetChar))
        {
            // This could help keep companions virign for Jade-Maiden's skill that requires it, etc.
            return true;
        }

        return false;

        static bool IsRomancableCompanion(Character taiwu, Character character) =>
            // XXX I would consider taiwu._bisexual too but I don't know if it's always true or something in the case of
            // Taiwu
            taiwu._gender != character._gender
            && DomainManager.Taiwu.IsInGroup(character._id)
            && IsRomancable(taiwu, character);

        // Whether the targetChar is already in a relationship with the selfChar or is single.
        static bool IsRomancable(Character selfChar, Character targetChar)
        {
            if (targetChar.HasVirginity())
            {
                // Shortcut; currently, NPCs don't have a way to gain back virginity unlike the Taiwu with the Taoist T4 skill.
                // If someone modifies this state outside of the game, that's not my problem :P
                return true;
            }

            var targetRelated = DomainManager.Character.GetRelatedCharacters(targetChar._id);

            // XXX Maybe an option to allow if the partners only includes past Taiwus? The function would then not be
            // generic to Character.
            var targetPartners = targetRelated.Adored.GetCollection();
            targetPartners.UnionWith(targetRelated.HusbandsAndWives.GetCollection());
            if (targetPartners.Count > 1)
            {
                return false;
            }
            if (targetPartners.Count == 1 && targetPartners.First() == selfChar._id)
            {
                return true;
            }

            // StepChildren is accounted for by the above partner check
            var targetChildren = targetRelated.BloodChildren.GetCollection();
            if (targetChildren.Count > 0)
            {
                // FIXME: Doesn't take into account the children with no father (magic pregnancy - monk's skill)
                var bothChildren = DomainManager
                    .Character.GetRelatedCharacters(selfChar._id)
                    .BloodChildren.GetCollection();
                bothChildren.IntersectWith(targetChildren);
                if (bothChildren.Count != targetChildren.Count)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
