using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using GameData.Domains.Character;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchNoRandomTransgender))]
static class PatchNoRandomTransgender
{
    internal static bool _enabled;

    // Targets the 3% random chance assignment:
    //      else if (random.CheckPercentProb(3))
    //      {
    //          this._transgender = true;
    //      }
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Character), nameof(Character.OfflineCreateIntelligentCharacter))]
    [HarmonyPatch(typeof(Character), nameof(Character.OfflineCreateTemporaryIntelligentCharacter))]
    static IEnumerable<CodeInstruction> OfflineCreate_BypassTransgender_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator il,
        MethodBase original
    )
    {
        var codes = new List<CodeInstruction>(instructions);
        var field = AccessTools.Field(typeof(Character), nameof(Character._transgender));

        var patched = false;
        for (var i = 2; i < codes.Count - 1; i++)
        {
            if (
                codes[i].StoresField(field)
                && codes[i - 1].LoadsConstant(1)
                && codes[i - 2].IsLdarg(0)
            )
            {
                var skipTarget = il.DefineLabel();
                codes[i + 1].labels.Add(skipTarget);

                var jump = new CodeInstruction(OpCodes.Br_S, skipTarget);
                jump.MoveLabelsFrom(codes[i - 2]);
                codes.Insert(i - 2, jump);
                patched = true;
                break;
            }
        }

        Debug.Assert(
            patched,
            $"Failed to find 'this._transgender = true' assignment in {original.DeclaringType?.FullName}.{original.Name}"
        );

        return codes;
    }
}
