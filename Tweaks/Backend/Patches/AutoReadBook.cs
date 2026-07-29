using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GameData.Domains.Item;
using GameData.Domains.World;
using HarmonyLib;

namespace Tweaks.Patches;

// Auto-read all pages for any book added to inventory.
[HarmonyPatchCategory(nameof(PatchAutoReadBook))]
static class PatchAutoReadBook
{
    internal static bool _enabled;

    // Piggy-backing on Abyss Mode ApplyChallengeModeAutoReadBook for implementation.
    // ViewChallenge.OnClickButtonConfirm()
    //      ViewNewGame.DoStartNewGame()
    //          GameData.Domains.World.WorldDomain.CreateWorld()
    //              .SetChallengeModeData(ChallengeModeData data)
    // WorldDomain.IsChallengeModeEnabled()
    //      WorldDomain.ApplyChallengeModeAutoReadBook()
    //      WorldDomain.ApplyChallengeModeAutoReadBookAtAllExistBook()
    //      WorldDomain.ApplyChallengeModeMoreActionPointRecovery()
    //      WorldDomain.ApplyChallengeModeMoreActionPointMax()
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(WorldDomain), nameof(WorldDomain.ApplyChallengeModeAutoReadBook))]
    static IEnumerable<CodeInstruction> WorldDomain_ApplyChallengeModeAutoReadBook_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = new List<CodeInstruction>(instructions);

        var isChallengeModeEnabled = AccessTools.Method(
            typeof(WorldDomain),
            nameof(WorldDomain.IsChallengeModeEnabled)
        );
        var anyCompletePage = AccessTools.Method(
            typeof(SkillBook),
            nameof(SkillBook.AnyCompletePage)
        );
        var getPageIncompleteState = AccessTools.Method(
            typeof(SkillBookStateHelper),
            nameof(SkillBookStateHelper.GetPageIncompleteState)
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(getPageIncompleteState))
            {
                codes.InsertRange(i + 1, [new(OpCodes.Pop), new(OpCodes.Ldc_I4_0)]);
            }
            else if (codes[i].Calls(isChallengeModeEnabled) || codes[i].Calls(anyCompletePage))
            {
                codes.InsertRange(i + 1, [new(OpCodes.Pop), new(OpCodes.Ldc_I4_1)]);
            }
        }

        return codes.AsEnumerable();
    }
}
