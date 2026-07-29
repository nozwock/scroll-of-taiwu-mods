using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GameData.Domains;
using GameData.Domains.Character;
using HarmonyLib;

namespace Tweaks.Patches;

// Prevent rape of Taiwu's companions and relatives.
// XXX Maybe even extend it to the Taiwu villagers.
// TODO: Make this all configurable.
//
// There's also MakeCharacterHaveSex and GmCmd_MakeCharacterHaveSex but they don't seem to be used...
[HarmonyPatchCategory(nameof(PatchNoTaiwuRape))]
static class PatchNoTaiwuRape
{
    internal static bool _enabled;

    // I see only one call to this function and that's with allowRape=false, but just to be safe we'll still patch it.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.OfflineExecuteFixedAction_MakeLove_Mutual))]
    static void Character_OfflineExecuteFixedAction_MakeLove_Mutual_Prefix(
        int targetCharId,
        ref bool allowRape
    )
    {
        if (allowRape)
        {
            allowRape = !BlockRape(targetCharId);
        }
    }

    // The "paranoia" (or skill issue) patch. If this is unexpectedly being called with isRape, we can't prevent the
    // event being recorded in LifeRecord but atleast nothing would have happened here lol
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.OfflineMakeLove))]
    static bool Character_OfflineMakeLove_Prefix(
        Character father,
        Character mother,
        bool isRape,
        ref bool __result
    )
    {
        if (isRape && BlockRape(father._id, mother._id))
        {
            __result = false; // isPregnant
            return false;
        }

        return true;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CharacterDomain), nameof(CharacterDomain.HandleRapeAction))]
    static IEnumerable<CodeInstruction> CharacterDomain_HandleRapeAction_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = instructions.ToList();

        var getFertility = AccessTools.Method(typeof(Character), nameof(Character.GetFertility));
        var interceptFertility = Transpilers.EmitDelegate(
            static (short fertility, Character targetChar) =>
            {
                if (BlockRape(targetChar._id))
                {
                    fertility = 0; // Fails the rape action due to fertility > 50 check
                }

                return fertility;
            }
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(getFertility))
            {
                var loadArgTargetChar = new CodeInstruction(OpCodes.Ldarg_3);
                codes.InsertRange(i + 1, [loadArgTargetChar, interceptFertility]);
                break;
            }
        }

        return codes.AsEnumerable();
    }

    static bool BlockRape(params int[] charIds)
    {
        var taiwuRelated = GetTaiwuRelated();
        return charIds.Any(charId =>
            DomainManager.Taiwu.IsInGroup(charId) || taiwuRelated.Contains(charId)
        );
    }

    static HashSet<int> GetTaiwuRelated()
    {
        var taiwuRelated = new HashSet<int>();

        DomainManager
            .Character.GetRelatedCharacters(DomainManager.Taiwu.GetTaiwuCharId())
            .GetAllPrioritizedCharIds(taiwuRelated);

        return taiwuRelated;
    }
}
