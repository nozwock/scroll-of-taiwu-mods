using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GameData.Domains.Item;
using GameData.Domains.Taiwu;
using HarmonyLib;

namespace Tweaks.Patches;

// Normal clothing doesn't get inherited by the successor when the Taiwu dies. Makes sense I guess, but here's a patch
// to get around it anyways :P
//
// SetTaiwuDying()
//     isTaiwuDying
//         AddTaiwuDeath()
//         EventHelper.TriggerLegacyPassingEvent()
//             // The below are invoked in Event/EventLib/Taiwu_EventPackage_TaiwuLegacyPassing.dll
//             .StartPassingLegacy()
//             .FinishPassingLegacy()
//             .StartUltimateSelectCharacterForDirectSamsaraMother()
//             .GameOver()
// TaiwuDomain.ConfirmChosenSuccessor()
//     .TransferTaiwuData()
//         ClothingItem.KeepOnPassing -_- Should've started from here...
[HarmonyPatchCategory(nameof(PatchAlwaysPassOnClothing))]
static class PatchAlwaysPassOnClothing
{
    internal static bool _enabled;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Clothing), nameof(Clothing.GetKeepOnPassing))]
    static bool Clothing_GetKeepOnPassing_Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }

    // Patching the ClothingItem..ctor to set KeepOnPassing to true didn't work.
    // I'm going to assume the ClothingItem are being instantiated before the mods are loaded.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.TransferTaiwuData))]
    static IEnumerable<CodeInstruction> TaiwuDomain_TransferTaiwuData_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = instructions.ToList();

        var target = AccessTools.Field(
            typeof(Config.ClothingItem),
            nameof(Config.ClothingItem.KeepOnPassing)
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].LoadsField(target))
            {
                codes.InsertRange(i + 1, [new(OpCodes.Pop), new(OpCodes.Ldc_I4_1)]);
                i += 2;
            }
        }

        return codes.AsEnumerable();
    }
}
