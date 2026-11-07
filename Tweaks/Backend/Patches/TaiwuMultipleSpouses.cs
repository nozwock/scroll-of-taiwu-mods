using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Character.Ai;
using GameData.Domains.Character.Relation;
using GameData.Domains.TaiwuEvent.EventHelper;
using HarmonyLib;

namespace Tweaks.Patches;

// ref https://steamcommunity.com/sharedfiles/filedetails/?id=3750326625
// A similar mod exists, but takes a much worse approach:
// - It globally prefix-patches CharacterDomain.GetAliveSpouse to return -1 for Taiwu just because it wanted
//   AllowAddingHusbandOrWifeRelation to not take Taiwu into account. That global lie breaks child recognition,
//   adoption, and event checks.
// - In AllowAddingHusbandOrWifeRelation, it forces __result = true, bypassing blood relation, duplicate marriage, and
//   other checks.
//
// Why only these few methods are patched?
// Daily interactions (dialogue, affection, UI, divorce, death) query relations via HasRelation() or iterate
// HashSet<int> HusbandsAndWives.  The game does not call GetAliveSpouse for regular gameplay. All other callers of
// GetAliveSpouse are NPC-specific and do not apply to Taiwu.
[HarmonyPatchCategory(nameof(PatchTaiwuMultipleSpouses))]
static class PatchTaiwuMultipleSpouses
{
    internal static bool _enabled;

    // Check whether Taiwu can get married to an NPC.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(EventHelper), nameof(EventHelper.IsTargetCanGetMarried))]
    static IEnumerable<CodeInstruction> EventHelper_IsTargetCanGetMarried_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        MethodBase original
    )
    {
        Debug.Assert(original.GetParameters()[0].ParameterType == typeof(int));

        var codes = instructions.ToList();
        var getAliveSpouse = AccessTools.Method(typeof(CharacterDomain), nameof(CharacterDomain.GetAliveSpouse));

        var intercept = Transpilers.EmitDelegate(
            static (CharacterDomain domain, int charId, int targetCharId) =>
            {
                if (charId == DomainManager.Taiwu.GetTaiwuCharId())
                {
                    return domain.HasRelation(charId, targetCharId, RelationType.HusbandOrWife) ? 1 : -1;
                }
                return domain.GetAliveSpouse(charId);
            }
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(getAliveSpouse))
            {
                codes[i] = intercept;
                codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
                i++;
            }
        }

        return codes.AsEnumerable();
    }

    // Relationship creation validity and NPC-initiated proposal check.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(RelationTypeHelper), nameof(RelationTypeHelper.AllowAddingHusbandOrWifeRelation))]
    [HarmonyPatch(typeof(AiHelper.Relation), nameof(AiHelper.Relation.CanStartRelation_HusbandOrWife))]
    static IEnumerable<CodeInstruction> AllowAddingHusbandOrWifeRelation_CanStartRelation_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = instructions.ToList();
        var getAliveSpouse = AccessTools.Method(typeof(CharacterDomain), nameof(CharacterDomain.GetAliveSpouse));

        var intercept = Transpilers.EmitDelegate(
            static (CharacterDomain domain, int charId) =>
                charId == DomainManager.Taiwu.GetTaiwuCharId() ? -1 : domain.GetAliveSpouse(charId)
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(getAliveSpouse))
            {
                codes[i] = intercept;
            }
        }

        return codes.AsEnumerable();
    }

    // Ensure child of Taiwu and any wedded spouse is recognized.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(EventHelper), nameof(EventHelper.DefaultHandleNewbornChild))]
    static IEnumerable<CodeInstruction> EventHelper_DefaultHandleNewbornChild_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        MethodBase original
    )
    {
        Debug.Assert(original.GetParameters()[1].ParameterType == typeof(int));
        return TranspileChildbirthSpouseCheck(instructions, new(OpCodes.Ldarg_1));
    }

    // Prevent newborn child from being abandoned if father and mother are wedded spouses.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(EventHelper), nameof(EventHelper.CheckParentAbandonNewbornChild))]
    static IEnumerable<CodeInstruction> EventHelper_CheckParentAbandonNewbornChild_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        MethodBase original
    )
    {
        Debug.Assert(original.GetParameters()[0].ParameterType == typeof(int));
        return TranspileChildbirthSpouseCheck(instructions, new(OpCodes.Ldarg_0));
    }

    static IEnumerable<CodeInstruction> TranspileChildbirthSpouseCheck(
        IEnumerable<CodeInstruction> instructions,
        CodeInstruction loadMotherId
    )
    {
        var codes = instructions.ToList();
        var getAliveSpouse = AccessTools.Method(typeof(CharacterDomain), nameof(CharacterDomain.GetAliveSpouse));

        var intercept = Transpilers.EmitDelegate(
            static (CharacterDomain domain, int fatherCharId, int motherCharId) =>
                domain.HasRelation(fatherCharId, motherCharId, RelationType.HusbandOrWife)
                    ? motherCharId
                    : domain.GetAliveSpouse(fatherCharId)
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(getAliveSpouse))
            {
                codes[i] = intercept;
                codes.Insert(i, loadMotherId);
                i++;
            }
        }

        return codes.AsEnumerable();
    }

    // Remove -60% adultery penalty between Taiwu and any of Taiwu's wedded spouses, or when Taiwu proposes a single
    // NPC.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AiHelper.Relation), nameof(AiHelper.Relation.GetStartRelationSuccessRate_BoyOrGirlFriend))]
    static IEnumerable<CodeInstruction> AiHelper_Relation_GetStartRelationSuccessRate_BoyOrGirlFriend_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        MethodBase original
    )
    {
        Debug.Assert(original.GetParameters()[1].ParameterType == typeof(Character));

        var codes = instructions.ToList();
        var getAliveSpouse = AccessTools.Method(typeof(CharacterDomain), nameof(CharacterDomain.GetAliveSpouse));

        var intercept = Transpilers.EmitDelegate(
            static (CharacterDomain domain, int selfCharId, Character targetChar) =>
            {
                // selfcharId is assumed to be where the Taiwu char will be passed to due to .IsTaiwu() being used on
                // selfChar in original function
                if (selfCharId == DomainManager.Taiwu.GetTaiwuCharId())
                {
                    return targetChar.GetId();
                }
                return domain.GetAliveSpouse(selfCharId);
            }
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(getAliveSpouse))
            {
                codes[i] = intercept;
                codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_1));
            }
        }

        return codes.AsEnumerable();
    }
}
