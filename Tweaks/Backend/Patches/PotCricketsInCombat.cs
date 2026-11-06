using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.Taiwu;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchPotCricketsInCombat))]
static class PatchPotCricketsInCombat
{
    internal static bool _enabled;

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.GetCricketCombatTaiwuDisplayData))]
    static IEnumerable<CodeInstruction> TaiwuDomain_GetCricketCombatTaiwuDisplayData_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = new List<CodeInstruction>(instructions);

        var getInventory = AccessTools.Method(
            typeof(Character),
            nameof(Character.GetInventory)
        );
        var itemsField = AccessTools.Field(
            typeof(Inventory),
            nameof(Inventory.Items)
        );
        var getKeys = AccessTools.PropertyGetter(
            typeof(Dictionary<ItemKey, int>),
            nameof(Dictionary<,>.Keys)
        );

        var getItemsWithVillageCrickets = Transpilers.EmitDelegate(
            static (Dictionary<ItemKey, int> inventoryItems) =>
            {
                var collection = DomainManager.Extra.GetCricketCollectionDataList();
                if (collection == null || collection.Count == 0)
                    return inventoryItems;

                var items = new Dictionary<ItemKey, int>(inventoryItems);
                foreach (var slot in collection)
                {
                    if (slot.Cricket.IsValid() && slot.CricketJar.IsValid())
                    {
                        items.TryAdd(slot.Cricket, 1);
                    }
                }
                return items;
            }
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (
                codes[i].Calls(getKeys)
                && i >= 2
                && codes[i - 2].Calls(getInventory)
                && codes[i - 1].LoadsField(itemsField)
            )
            {
                codes.Insert(i, getItemsWithVillageCrickets);
                i++;
                break;
            }
        }

        return codes.AsEnumerable();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.RequestValidCricketPlan))]
    static IEnumerable<CodeInstruction> TaiwuDomain_RequestValidCricketPlan_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = new List<CodeInstruction>(instructions);

        var containsMethod = AccessTools.Method(
            typeof(Inventory),
            nameof(Inventory.Contains),
            [typeof(int)]
        );

        var checkContainsOrInVillagePot = Transpilers.EmitDelegate(
            static (Inventory inventory, int num) =>
            {
                if (inventory.Contains(num))
                    return true;

                var collection = DomainManager.Extra.GetCricketCollectionDataList();
                if (collection == null)
                    return false;

                foreach (var slot in collection)
                {
                    if (
                        slot.Cricket.IsValid()
                        && slot.CricketJar.IsValid()
                        && slot.Cricket.Id == num
                    )
                    {
                        return true;
                    }
                }

                return false;
            }
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(containsMethod))
            {
                codes[i] = checkContainsOrInVillagePot;
                break;
            }
        }

        return codes.AsEnumerable();
    }

    // Ensure wins by village pot crickets (owned by a Building) are recognized as Taiwu-owned
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ItemDomain), nameof(ItemDomain.SetCricketRecord))]
    static IEnumerable<CodeInstruction> ItemDomain_SetCricketRecord_Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        var codes = new List<CodeInstruction>(instructions);

        var tryGetCricket = AccessTools.Method(
            typeof(ItemDomain),
            nameof(ItemDomain.TryGetElement_Crickets)
        );
        var getTaiwuCharId = AccessTools.Method(
            typeof(TaiwuDomain),
            nameof(TaiwuDomain.GetTaiwuCharId)
        );

        object? cricketVar = null;
        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(tryGetCricket) && i > 0)
            {
                cricketVar = codes[i - 1].operand;
                break;
            }
        }

        Debug.Assert(cricketVar != null);

        var checkIsTaiwuOrVillageCricket = Transpilers.EmitDelegate(
            static (Cricket element, bool currentFlag) =>
                currentFlag
                || (element?.Owner is { OwnerType: ItemOwnerType.Building, OwnerId: 44 })
        );

        for (var i = 0; i < codes.Count; i++)
        {
            if (!codes[i].Calls(getTaiwuCharId))
            {
                continue;
            }

            for (var j = i + 1; j < codes.Count; j++)
            {
                if (!codes[j].IsStloc())
                {
                    continue;
                }

                var stlocFlagInst = codes[j];
                var ldlocFlagInst = stlocFlagInst.opcode switch
                {
                    var op when op == OpCodes.Stloc_0 => new CodeInstruction(
                        OpCodes.Ldloc_0
                    ),
                    var op when op == OpCodes.Stloc_1 => new CodeInstruction(
                        OpCodes.Ldloc_1
                    ),
                    var op when op == OpCodes.Stloc_2 => new CodeInstruction(
                        OpCodes.Ldloc_2
                    ),
                    var op when op == OpCodes.Stloc_3 => new CodeInstruction(
                        OpCodes.Ldloc_3
                    ),
                    var op when op == OpCodes.Stloc_S => new CodeInstruction(
                        OpCodes.Ldloc_S,
                        stlocFlagInst.operand
                    ),
                    _ => new CodeInstruction(OpCodes.Ldloc, stlocFlagInst.operand),
                };

                var ldlocCricketInst = new CodeInstruction(OpCodes.Ldloc_S, cricketVar);

                codes.InsertRange(
                    j + 1,
                    [
                        ldlocCricketInst,
                        ldlocFlagInst,
                        checkIsTaiwuOrVillageCricket,
                        new(stlocFlagInst.opcode, stlocFlagInst.operand),
                    ]
                );
                return codes.AsEnumerable();
            }
        }

        return codes.AsEnumerable();
    }
}
