using System;
using System.Collections.Generic;
using System.IO;
using FrameWork.UISystem.UIElements;
using Game.Views.CharacterMenu;
using GameData.Domains.Mod;
using GameData.Utilities;
using HarmonyLib;
using Newtonsoft.Json;
using TMPro;
using Tweaks.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Tweaks.Patches;

[HarmonyPatchCategory]
static class PatchShareSkillPlans
{
    internal static bool _enabled;

    const string JsonFilter = "JSON Files(*.json)\0*.json\0";

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewCharacterMenuEquipCombatSkill), "Awake")]
    static void Awake_Postfix(ViewCharacterMenuEquipCombatSkill __instance)
    {
        InitButtons(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewCharacterMenuEquipCombatSkill), nameof(ViewCharacterMenuEquipCombatSkill.Refresh))]
    static void Refresh_Postfix(ViewCharacterMenuEquipCombatSkill __instance)
    {
        InitButtons(__instance);
        UpdateButtonsState(__instance);
    }

    static void InitButtons(ViewCharacterMenuEquipCombatSkill self)
    {
        static void SetupButton(GameObject btnGo, string label, TextMeshProUGUI? fontSource, Action onClick)
        {
            // Hide cloned icon images
            foreach (var img in btnGo.GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject != btnGo)
                {
                    img.gameObject.SetActive(false);
                }
            }

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(btnGo.transform, false);
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            if (fontSource != null)
            {
                tmp.font = fontSource.font;
                tmp.fontSharedMaterial = fontSource.fontSharedMaterial;
            }
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.text = label;
            tmp.raycastTarget = false;

            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(Mathf.Max(btnRect.sizeDelta.x, 64f), btnRect.sizeDelta.y);

            var cButton = btnGo.GetComponent<CButton>();
            cButton?.ClearAndAddListener(onClick);
        }

        if (self.copyPlanButton == null)
            return;

        var parent = self.copyPlanButton.transform.parent;
        if (parent == null)
            return;

        var exportTransform = parent.Find("ExportPlanButton");
        var importTransform = parent.Find("ImportPlanButton");

        if (exportTransform == null)
        {
            var exportGo = UnityEngine.Object.Instantiate(self.copyPlanButton.gameObject, parent);
            exportGo.name = "ExportPlanButton";
            SetupButton(exportGo, "Export", self.availableGenericGridCountText, () => OnExportClicked(self));
            exportTransform = exportGo.transform;
        }

        if (importTransform == null)
        {
            var importGo = UnityEngine.Object.Instantiate(self.copyPlanButton.gameObject, parent);
            importGo.name = "ImportPlanButton";
            SetupButton(importGo, "Import", self.availableGenericGridCountText, () => OnImportClicked(self));
            importTransform = importGo.transform;
        }

        exportTransform.gameObject.SetActive(_enabled);
        importTransform.gameObject.SetActive(_enabled);

        if (self.deletePlanButton != null)
        {
            var delIndex = self.deletePlanButton.transform.GetSiblingIndex();
            exportTransform.SetSiblingIndex(delIndex + 1);
            importTransform.SetSiblingIndex(delIndex + 2);
        }
    }

    static void UpdateButtonsState(ViewCharacterMenuEquipCombatSkill self)
    {
        var parent = self.copyPlanButton?.transform?.parent;
        if (parent == null)
            return;

        var exportTransform = parent.Find("ExportPlanButton");
        var importTransform = parent.Find("ImportPlanButton");
        if (exportTransform == null || importTransform == null)
            return;

        exportTransform.gameObject.SetActive(_enabled);
        importTransform.gameObject.SetActive(_enabled);

        if (!_enabled)
            return;

        var canOperate = self.CanOperate;
        var planCount = self._equipCombatSkillDisplayData?.PlanCount ?? 1;

        var exportBtn = exportTransform.GetComponent<CButton>();
        exportBtn?.interactable = canOperate;

        var importBtn = importTransform.GetComponent<CButton>();
        importBtn?.interactable = canOperate && planCount < 9;

        var importTooltip = importTransform.GetComponent<TooltipInvoker>();
        if (importTooltip != null)
        {
            importTooltip.PresetParam =
            [
                planCount < 9 ? "Import Preset" : LanguageKey.LK_CombatSkill_Equip_PlanLimitMax.Tr()
            ];
            importTooltip.Refresh();
        }

        var exportTooltip = exportTransform.GetComponent<TooltipInvoker>();
        if (exportTooltip != null)
        {
            exportTooltip.PresetParam = ["Export Preset"];
            exportTooltip.Refresh();
        }
    }

    static void OnExportClicked(ViewCharacterMenuEquipCombatSkill self)
    {
        try
        {
            var displayData = self._equipCombatSkillDisplayData;
            if (displayData == null)
                return;

            var curPlanId = displayData.CurrentPlanId;
            List<SkillPlanItem> GetCategoryItems(sbyte equipType)
            {
                var list = self.GetEquippedSkills(equipType);
                var result = new List<SkillPlanItem>();
                if (list == null)
                {
                    return result;
                }

                foreach (var id in list)
                {
                    if (id >= 0)
                    {
                        string name = Config.CombatSkill.Instance.GetItemOrDefault(id)?.Name ?? string.Empty;
                        bool compacted = self.EquippedSkillDataDict.TryGetValue(id, out var data) && data.Mastered;
                        result.Add(new SkillPlanItem { Id = id, Name = name, Compacted = compacted });
                    }
                }
                return result;
            }

            var exportData = new SkillPlanExportData
            {
                Version = 1,
                Name = $"Preset {curPlanId + 1}",
                Date = DateTime.Now.ToString("s"),
                GenericGridAllocation = displayData.GenericGridAllocation != null
                    ? Array.ConvertAll(displayData.GenericGridAllocation, b => (int)b)
                    : null,
                Neigong = GetCategoryItems(0),
                Attack = GetCategoryItems(1),
                Agility = GetCategoryItems(2),
                Defense = GetCategoryItems(3),
                Assistance = GetCategoryItems(4)
            };

            string archiveDirPath = GameApp.GetArchiveDirPath();
            if (!Directory.Exists(archiveDirPath))
            {
                Directory.CreateDirectory(archiveDirPath);
            }

            string rawPath = LocalDialog.SelectSaveFilePath(JsonFilter, archiveDirPath);
            if (string.IsNullOrEmpty(rawPath))
                return;

            string savePath = rawPath.Split('\0')[0];
            if (string.IsNullOrWhiteSpace(savePath))
                return;

            if (!savePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                savePath += ".json";
            }

            var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
            File.WriteAllText(savePath, json);

            AdaptableLog.Info($"Exported skill plan to {savePath}");
        }
        catch (Exception ex)
        {
            AdaptableLog.Error($"Error exporting skill plan: {ex}");
        }
    }

    static void OnImportClicked(ViewCharacterMenuEquipCombatSkill self)
    {
        if (self._equipCombatSkillDisplayData?.PlanCount >= 9)
            return;

        try
        {
            string archiveDirPath = GameApp.GetArchiveDirPath();
            if (!Directory.Exists(archiveDirPath))
            {
                Directory.CreateDirectory(archiveDirPath);
            }

            string rawPath = LocalDialog.GetUnitySelectFileName(JsonFilter, archiveDirPath);
            if (string.IsNullOrEmpty(rawPath))
                return;

            string loadPath = rawPath.Split('\0')[0];
            if (!File.Exists(loadPath))
                return;

            var json = File.ReadAllText(loadPath);
            if (string.IsNullOrWhiteSpace(json))
                return;

            if (self == null || !self.isActiveAndEnabled)
                return;

            var modData = new SerializableModData();
            modData.Set("CharId", self.CharacterMenu.CurCharacterId);
            modData.Set("Json", json);

            var modIdStr = Plugin.Instance?.ModIdStr
                ?? throw new NullReferenceException(nameof(Plugin.Instance.ModIdStr));

            ModDomainMethod.Call.CallModMethodWithParam(modIdStr, "ShareSkillPlans", modData);
            self.RequestData();
        }
        catch (Exception ex)
        {
            AdaptableLog.Error($"Error importing skill plan: {ex}");
        }
    }
}
