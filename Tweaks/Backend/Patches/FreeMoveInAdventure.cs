using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GameData.Adventure;
using GameData.Domains.Adventure;
using GameData.Domains.Extra;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatch]
static class PatchFreeMoveInAdventure
{
    // Event/Adventure ("encounter") tiles/cells costing no energy
    // Frontend patch: ViewAdventureRemake.SetCostText
    //
    // Game.Views.Adventure.ViewAdventurePrepare // "Encounter" (event) start popup dialog
    //      ViewAdventureRemake
    //          calls AdventureRuntime.GetMoveCost() // For tile/cell move cost
    // AdvanceDaysInMonth()
    // ConsumeActionPointInAdventure()
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AdventureRuntime), nameof(AdventureRuntime.GetMoveCost))]
    static bool AdventureRuntime_GetMoveCost_Prefix(ref int __result)
    {
        __result = 0;
        return false;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(AdventureRuntime), nameof(AdventureRuntime.GetMoveCost))]
    static int AdventureRuntime_GetMoveCost_Original(
        AdventureRuntime __instance,
        AdventureBlockIndex pos
    ) => throw new NotImplementedException();

    // Important to let the MoveInAdventure call in the original GetMoveCost since it also updates the map's state every
    // 10 action points cost, like progressing the NPCs' action, moving them, etc.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AdventureDomain), nameof(AdventureDomain.MoveInAdventure))]
    static IEnumerable<CodeInstruction> AdventureDomain_MoveInAdventure_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = new List<CodeInstruction>(instructions);

        var getMoveCost_Original = AccessTools.Method(
            typeof(PatchFreeMoveInAdventure),
            nameof(AdventureRuntime_GetMoveCost_Original)
        );
        var getMoveCost = AccessTools.Method(
            typeof(AdventureRuntime),
            nameof(AdventureRuntime.GetMoveCost)
        );
        var isActionPointEnough = AccessTools.Method(
            typeof(ExtraDomain),
            nameof(ExtraDomain.IsActionPointEnough)
        );
        var consumeActionPoint = AccessTools.Method(
            typeof(ExtraDomain),
            nameof(ExtraDomain.ConsumeActionPoint)
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(getMoveCost))
            {
                // Reusing existing CodeInstruction to avoid having to carry-over other states like labels, etc.
                codes[i].opcode = OpCodes.Call;
                codes[i].operand = getMoveCost_Original;
            }
            else if (codes[i].Calls(isActionPointEnough))
            {
                // replace IsActionPointEnough return value with true
                codes.InsertRange(i + 1, [new(OpCodes.Pop), new(OpCodes.Ldc_I4_1)]);
            }
            else if (codes[i].Calls(consumeActionPoint))
            {
                // ConsumeActionPoint(context, 0);
                codes.InsertRange(i, [new(OpCodes.Pop), new(OpCodes.Ldc_I4_0)]);
                i += 2; // Prevent infinite loop and "memory leak" T_T. Yes, I had the glory of experiencing it.
            }
        }

        return codes.AsEnumerable();
    }
}
