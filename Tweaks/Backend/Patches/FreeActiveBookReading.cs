using System.Collections.Generic;
using System.Reflection.Emit;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Extra;
using GameData.Domains.Taiwu;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchFreeActiveBookReading))]
static class PatchFreeActiveBookReading
{
    internal static bool _enabled;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.ActiveReadOnce))]
    static void TaiwuDomain_ActiveReadOnce_Prefix()
    {
        // Modifying GlobalConfig in .ctor doesn't seem to be working
        GlobalConfig.Instance.ActiveReadingTimeCost = 0;
        GlobalConfig.Instance.ActiveReadingAttributeCost = 0;
    }

    // This is also used on month change
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.MakeReadingProgressActuallyWork))]
    static void TaiwuDomain_MakeReadingProgressActuallyWork_Prefix(
        DataContext context,
        ref int activeReadProgressAffectedEfficiency
    )
    {
        activeReadProgressAffectedEfficiency = 10000;
        DomainManager.Extra.SetActiveReadingProgress(0, context);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.ActiveReadOnce))]
    static IEnumerable<CodeInstruction> TaiwuDomain_ActiveReadOnce_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var target = AccessTools.Method(
            typeof(ExtraDomain),
            nameof(ExtraDomain.GetActiveReadingProgress)
        );

        foreach (var code in instructions)
        {
            yield return code;

            if (code.Calls(target))
            {
                yield return new CodeInstruction(OpCodes.Pop);
                yield return new CodeInstruction(OpCodes.Ldc_I4_S, 9); // +1 will be 10
            }
        }
    }
}
