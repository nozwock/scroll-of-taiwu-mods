using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GameData.Domains.Character;
using GameData.Domains.Taiwu;
using HarmonyLib;

namespace Tweaks.Patches;

// Disable anomalous cricket pregnancy
[HarmonyPatch]
static class PatchNoCricketPregnancy
{
    // Related:
    // Character.OfflineUpdatePregnantState()
    //      CharacterDomain.ParallelCreateNewbornChildren()
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CharacterDomain), nameof(CharacterDomain.CreatePregnantState))]
    static IEnumerable<CodeInstruction> CharacterDomain_CreatePregnantState_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = new List<CodeInstruction>(instructions);

        var target = AccessTools.Method(
            typeof(TaiwuDomain),
            nameof(TaiwuDomain.GetCricketLuckPoint)
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(target))
            {
                codes.InsertRange(i + 1, [new(OpCodes.Pop), new(OpCodes.Ldc_I4_0)]);
            }
        }

        return codes.AsEnumerable();
    }
}
