using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using FrameWork.UISystem.UIElements;
using Game.Views;
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

    static string LoadoutsDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "MartialLoadouts"));

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

    record RenameCfgData(bool IgnoreValidation = false);
    static readonly ConditionalWeakTable<RenameCfg, RenameCfgData> _renameCfgCwt = new();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewRename), nameof(ViewRename.TrySubmit))]
    static bool ViewRename_TrySubmit_Prefix(
        ViewRename __instance,
        RenameCfg ____renameCfg,
        TMP_InputField ___inputField
    )
    {
        if (!_enabled)
            return true;

        if (_renameCfgCwt.TryGetValue(____renameCfg, out var data) && data.IgnoreValidation)
        {
            ____renameCfg.Submit?.Invoke(___inputField.text.Trim());
            __instance.QuickHide();
            return false;
        }

        return true;
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
            var charId = self.CharacterMenu?.CurCharacterId ?? -1;
            var isTaiwu = self.CharacterMenu != null && self.CharacterMenu.CurrentCharacterIsTaiwu;
            var charName = self.CharacterMenu?.DisplayCharacters
                .Where(c => c != null && c.CharacterId == charId)
                .Select(c => NameCenter.GetMonasticTitleOrDisplayName(c, isTaiwu))
                .FirstOrDefault()
                ?? string.Empty;

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
                CharId = charId,
                CharName = charName,
                GenericGridAllocation = displayData.GenericGridAllocation != null
                    ? Array.ConvertAll(displayData.GenericGridAllocation, b => (int)b)
                    : null,
                Neigong = GetCategoryItems(0),
                Attack = GetCategoryItems(1),
                Agility = GetCategoryItems(2),
                Defense = GetCategoryItems(3),
                Assistance = GetCategoryItems(4)
            };

            var safeCharName = charName;
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                safeCharName = safeCharName.Replace(c, '_');
            }

            var defaultFileName = $"Preset_{curPlanId + 1}-{DateTime.Now:yyyyMMdd_HHmmss}";
            if (!string.IsNullOrWhiteSpace(safeCharName))
            {
                defaultFileName = $"{safeCharName}-{defaultFileName}";
            }

            // Not using FileDialog.SelectSaveFilePath since that leads to game crash with pipe disconnection errors, at
            // least on Linux when running the game through Proton.
            var renameCfg = new RenameCfg
            {
                Title = "Export Preset",
                Description = "Enter a file name for the exported preset:",
                Default = defaultFileName,
                EmptyDesc = "Enter file name...",
                CharCount = 255,
                IsHideDescription = false,
                Submit = inputName =>
                {
                    try
                    {
                        var fileName = string.IsNullOrWhiteSpace(inputName) ? defaultFileName : inputName.Trim();
                        foreach (var c in Path.GetInvalidFileNameChars())
                        {
                            fileName = fileName.Replace(c, '_');
                        }

                        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        {
                            fileName += ".json";
                        }

                        var loadoutsDir = LoadoutsDir;
                        if (!Directory.Exists(loadoutsDir))
                        {
                            Directory.CreateDirectory(loadoutsDir);
                        }

                        var savePath = Path.Combine(loadoutsDir, fileName);
                        var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
                        File.WriteAllText(savePath, json);

                        AdaptableLog.Info($"Exported skill plan to {savePath}");
                        CommonUtils.ShowDialog("Export Preset", $"Preset exported to:\n./{Path.GetFileName(loadoutsDir)}/{Path.GetFileName(savePath)}");
                    }
                    catch (Exception ex)
                    {
                        AdaptableLog.Error($"Error writing exported skill plan: {ex}");
                        CommonUtils.ShowDialog("Export Failed", $"Failed to save preset:\n{ex.Message}");
                    }
                }
            };
            _renameCfgCwt.Add(renameCfg, new(IgnoreValidation: true));
            renameCfg.Show();
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
            string loadoutsDir = LoadoutsDir;
            if (!Directory.Exists(loadoutsDir))
            {
                Directory.CreateDirectory(loadoutsDir);
            }

            string rawPath = LocalDialog.GetUnitySelectFileName(JsonFilter, loadoutsDir);
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
