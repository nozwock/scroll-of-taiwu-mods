using System;
using System.Collections.Generic;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Taiwu;
using GameData.Utilities;
using Newtonsoft.Json;
using Tweaks.Common;

namespace Tweaks.Patches;

public static class PatchShareSkillPlans
{
    static CombatSkillPlan BuildCombatSkillPlan(SkillPlanExportData exportData)
    {
        var plan = new CombatSkillPlan();

        if (exportData == null)
            return plan;

        if (exportData.GenericGridAllocation != null)
        {
            var max = Math.Min(plan.GenericGridAllocation.Length, exportData.GenericGridAllocation.Length);
            for (var i = 0; i < max; i++)
            {
                plan.GenericGridAllocation[i] = (byte)Math.Max(0, exportData.GenericGridAllocation[i]);
            }
        }

        static void PopulateList(short[] target, List<SkillPlanItem>? source)
        {
            if (source == null)
                return;

            var idx = 0;
            foreach (var item in source)
            {
                if (idx >= target.Length)
                    break;

                if (item.Id >= 0)
                {
                    target[idx++] = item.Id;
                }
            }
        }

        PopulateList(plan.NeigongList, exportData.Neigong);
        PopulateList(plan.AttackSkillList, exportData.Attack);
        PopulateList(plan.AgilitySkillList, exportData.Agility);
        PopulateList(plan.DefenseSkillList, exportData.Defense);
        PopulateList(plan.AssistanceSkillList, exportData.Assistance);

        return plan;
    }

    // ref TaiwuDomain.CopyCombatSkillPlan
    // CharacterDomain.DuplicateCurrentCombatSkillPlan
    public static bool ImportSkillPlan(DataContext ctx, int charId, string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return false;

        SkillPlanExportData? exportData;
        try
        {
            exportData = JsonConvert.DeserializeObject<SkillPlanExportData>(jsonContent);
        }
        catch (Exception ex)
        {
            AdaptableLog.Error($"Failed to parse skill plan JSON: {ex}");
            return false;
        }

        if (exportData == null)
            return false;

        var isTaiwu = charId == DomainManager.Taiwu.GetTaiwuCharId();
        var planCount = isTaiwu
            ? DomainManager.Extra.GetUnlockedCombatSkillPlanCount()
            : DomainManager.Extra.TryGetCharacterCombatSkillConfiguration(charId)?.PlanCount ?? 1;

        if (planCount >= 9)
        {
            AdaptableLog.Warning($"Cannot import skill plan: plan count for character {charId} is already {planCount}/9.");
            return false;
        }

        // Append new combat skill plan
        DomainManager.Character.AppendCombatSkillPlan(ctx, charId);

        var character = DomainManager.Character.GetElement_Objects(charId);
        var charCombatSkills = DomainManager.CombatSkill.GetCharCombatSkills(charId);

        int curPlanId;
        CharacterCombatSkillConfiguration? config = null;

        if (isTaiwu)
        {
            curPlanId = DomainManager.Taiwu.GetCurrCombatSkillPlanId();
        }
        else
        {
            config = DomainManager.Extra.TryGetCharacterCombatSkillConfiguration(charId)
                ?? new CharacterCombatSkillConfiguration(character);
            curPlanId = config.CurrentPlanId;
        }

        var plan = BuildCombatSkillPlan(exportData);

        // Collect and apply mastered (compacted) skills
        var masteredShortList = ShortList.Create();
        void CollectMastered(List<SkillPlanItem>? items)
        {
            if (items == null)
                return;

            foreach (var item in items)
            {
                if (item.Compacted
                    && item.Id >= 0
                    && charCombatSkills.TryGetValue(item.Id, out var skill)
                    && !skill.GetRevoked()
                    && !masteredShortList.Items.Contains(item.Id))
                {
                    masteredShortList.Items.Add(item.Id);
                }
            }
        }

        CollectMastered(exportData.Neigong);
        CollectMastered(exportData.Attack);
        CollectMastered(exportData.Agility);
        CollectMastered(exportData.Defense);
        CollectMastered(exportData.Assistance);

        DomainManager.Extra.SetCharacterMasteredCombatSkills(ctx, charId, masteredShortList);
        if (isTaiwu)
        {
            DomainManager.Extra.SetElement_MasteredCombatSkillPlans(curPlanId, masteredShortList, ctx);
        }
        else
        {
            config?.CombatSkillMasterPlans[curPlanId] = masteredShortList;
        }

        // Apply plan
        DomainManager.Character.ApplyCombatSkillPlan(ctx, character, plan);

        if (isTaiwu)
        {
            DomainManager.Taiwu.SetElement_CombatSkillPlans(curPlanId, plan, ctx);
            DomainManager.Taiwu.SetGenericGridAllocation(ctx, plan.GenericGridAllocation);
            DomainManager.Taiwu.UpdateCombatSkillPlan(ctx, curPlanId);

            if (exportData.SkillOrder != null)
            {
                // Skips passive skills (0 = Neigong, 4 = Assistance)
                // ref: ExtraDomain.CheckCombatSkillOrderPlan
                //      ViewCharacterMenuEquipCombatSkill.ResetSkillOrder
                var equippedSkills = character.GetCombatSkillEquipment();
                var validEquippedSkills = new HashSet<short>();
                for (sbyte category = 1; category <= 3; category++)
                {
                    foreach (var id in equippedSkills[category])
                    {
                        if (id >= 0)
                        {
                            validEquippedSkills.Add(id);
                        }
                    }
                }

                // 18 main slots and 36 extra slots, with -1 representing empty slots.
                // ref: ViewCharacterMenuEquipCombatSkill.UpdateSortCombatSkillPanel
                //      ViewCharacterMenuEquipCombatSkill.ResetSkillOrder
                //      ExtraDomain.SetCombatSkillOrderPlan
                //      ExtraDomain.CheckCombatSkillOrderPlan
                var orderList = new List<short>(54);
                var placedSkills = new HashSet<short>();
                for (var slot = 0; slot < 54; slot++)
                {
                    if (slot < exportData.SkillOrder.Count)
                    {
                        var id = exportData.SkillOrder[slot];
                        if (id >= 0 && validEquippedSkills.Contains(id) && placedSkills.Add(id))
                        {
                            orderList.Add(id);
                            continue;
                        }
                    }
                    orderList.Add(-1);
                }

                // If any equipped Attack/Agility/Defense skills were missing from the preset's order, place them into
                // the earliest available empty (-1) slots, similar to ExtraDomain.CheckCombatSkillOrderPlan.
                for (sbyte category = 1; category <= 3; category++)
                {
                    foreach (var id in equippedSkills[category])
                    {
                        if (id >= 0 && validEquippedSkills.Contains(id) && placedSkills.Add(id))
                        {
                            var emptyIdx = orderList.IndexOf(-1);
                            if (emptyIdx >= 0)
                            {
                                orderList[emptyIdx] = id;
                            }
                        }
                    }
                }

                var orderShortList = ShortList.Create();
                orderShortList.Items.AddRange(orderList);
                DomainManager.Extra.SetCombatSkillOrderPlan(ctx, orderShortList);
            }
        }
        else if (config != null)
        {
            config.CombatSkillEquipPlans[curPlanId] = plan;
            DomainManager.Extra.SetCharacterCombatSkillConfiguration(ctx, charId, config);
        }

        return true;
    }
}
