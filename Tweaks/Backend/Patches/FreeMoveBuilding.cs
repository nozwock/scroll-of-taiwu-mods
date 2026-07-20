using System.Collections.Generic;
using System.Linq;
using GameData.Domains.Building;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatch]
static class PatchFreeMoveBuilding
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(BuildingDomain), nameof(BuildingDomain.ConfirmPlanBuilding))]
    static IEnumerable<CodeInstruction> BuildingDomain_ConfirmPlanBuilding(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = new List<CodeInstruction>(instructions);

        var target = AccessTools.Method(
            typeof(BuildingDomain),
            nameof(BuildingDomain.ConsumeResource)
        );

        for (var i = 0; i < codes.Count; i++)
        {
            // Patching BuildingBlockItem..cotr didn't work for some reason
            if (codes[i].Calls(target))
            {
                // 4 argument load instructions
                codes.RemoveRange(i - 4, 5);
                break;
            }
        }

        return codes.AsEnumerable();
    }
}
