using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GameData.Common;
using GameData.Domains.Item;
using GameData.Domains.Taiwu;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchMaxCricketDurability))]
static class PatchMaxCricketDurability
{
    internal static bool _enabled;

    // ViewCricketBetting
    // ViewCricketCombat.DoSettlement()
    //      ItemDomain.SettlementCricketWager() - There's also SetCurrDurability call in here but it's covered by
    //      Cricket.SetCurrDurability patch
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.GetCricketCombatTaiwuDisplayData))]
    static IEnumerable<CodeInstruction> TaiwuDomain_GetCricketCombatTaiwuDisplayData_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = new List<CodeInstruction>(instructions);

        var tryGetCricket = AccessTools.Method(
            typeof(ItemDomain),
            nameof(ItemDomain.TryGetElement_Crickets)
        );

        var resetDurability = Transpilers.EmitDelegate(
            static (bool ok, Cricket cricket) =>
            {
                if (ok)
                {
                    cricket.SetCurrDurability(
                        cricket.GetMaxDurability(),
                        DataContextManager.GetCurrentThreadDataContext()
                    );
                }

                return ok;
            }
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(tryGetCricket))
            {
                var localCricket = codes[i - 1].operand;
                codes.InsertRange(i + 1, [new(OpCodes.Ldloc_S, localCricket), resetDurability]);
                break;
            }
        }

        return codes.AsEnumerable();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Cricket), nameof(Cricket.SetCurrDurability))]
    static void Cricket_SetCurrDurability_Prefix(Cricket __instance, ref short currDurability)
    {
        currDurability = __instance.GetMaxDurability();
    }
}
