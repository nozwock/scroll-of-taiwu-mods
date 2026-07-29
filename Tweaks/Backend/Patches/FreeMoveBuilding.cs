using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GameData.Domains.Building;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchFreeMoveBuilding))]
static class PatchFreeMoveBuilding
{
    internal static bool _enabled;

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BuildingDomain), nameof(BuildingDomain.ConfirmPlanBuilding))]
    static IEnumerable<CodeInstruction> BuildingDomain_ConfirmPlanBuilding(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator il
    )
    {
        var codes = new List<CodeInstruction>(instructions);

        var target = AccessTools.Method(
            typeof(BuildingDomain),
            nameof(BuildingDomain.ConsumeResource)
        );

        // ref https://gist.github.com/JavidPack/454477b67db8b017cb101371a8c49a1c#harmony-patch
        var skipTarget = il.DefineLabel();

        for (var i = 0; i < codes.Count; i++)
        {
            // Patching BuildingBlockItem..ctor didn't work for some reason
            if (
                codes[i].IsLdarg()
                && codes[i + 1].IsLdarg()
                && codes[i + 2].IsLdloc()
                && codes[i + 3].IsLdloc()
                && codes[i + 4].Calls(target)
            )
            {
                codes[i + 5].labels.Add(skipTarget);
                codes.Insert(i++, new(OpCodes.Br_S, skipTarget));
                break;
            }
        }

        return codes.AsEnumerable();
    }
}
