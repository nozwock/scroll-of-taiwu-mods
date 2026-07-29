using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Game.Views.Building;
using GameData.Domains.Building;
using HarmonyLib;

namespace Tweaks.Patches;

// FIXME: Isn't exactly instant. Sets the demolition duration to infinity, with a close button which actually removes
// the building? Need to look more into this.
[HarmonyPatchCategory]
static class PatchInstantRemoveBuilding
{
    internal static bool _enabled;

    // BuildingDomain
    //      .Remove()
    //          .SetVillageBuildWorkAndBlockData()
    //      .OfflineUpdateOperation()
    //
    // If later, the patch needs to be conditional: Wrap the call to GmCmd_RemoveBuildingImmediately in a emitted
    // delegate where the check for whether the patch should be enabled or not can take place.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ViewBuildingArea), nameof(ViewBuildingArea.ConfirmMultiplyRemove))]
    static IEnumerable<CodeInstruction> ViewBuildingArea_ConfirmMultiplyRemove_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = instructions.ToList();

        var target = AccessTools.Method(
            typeof(BuildingDomainMethod.Call),
            nameof(BuildingDomainMethod.Call.Remove)
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(target))
            {
                // Don't like replacing original instruction but oh well...
                codes[i].operand = AccessTools.Method(
                    typeof(BuildingDomainMethod.Call),
                    nameof(BuildingDomainMethod.Call.GmCmd_RemoveBuildingImmediately)
                );
                codes.Insert(i++, new(OpCodes.Pop)); // Takes care of `int[] workers` on the evaluation stack
                break;
            }
        }

        return codes.AsEnumerable();
    }
}
