using GameData.Domains.Item;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchMaxItemDurability))]
static class PatchMaxItemDurability
{
    internal static bool _enabled;

    // CraftTool and Accessory because they're not repairable
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CraftTool), nameof(CraftTool.SetCurrDurability))]
    [HarmonyPatch(typeof(Accessory), nameof(Accessory.SetCurrDurability))]
    static void Partial_ItemBase_SetCurrDurability_Prefix(
        object __instance,
        ref short currDurability
    )
    {
        var item = (ItemBase)__instance;
        currDurability = item.GetMaxDurability();
    }
}
