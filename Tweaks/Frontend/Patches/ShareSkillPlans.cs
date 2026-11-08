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

    static Sprite? _exportSprite;
    static Sprite? _importSprite;

    static Sprite GetExportSprite() => _exportSprite ??= LoadEmbeddedSprite("export.png");
    static Sprite GetImportSprite() => _importSprite ??= LoadEmbeddedSprite("import.png");

    static Sprite LoadEmbeddedSprite(string resourceFileName)
    {
        var asm = typeof(PatchShareSkillPlans).Assembly;
        var resName = asm.GetManifestResourceNames()
            .FirstOrDefault(it => it.EndsWith(resourceFileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException($"Embedded resource '{resourceFileName}' not found in {asm.FullName}");

        using var stream = asm.GetManifestResourceStream(resName);
        using var outStream = new MemoryStream();
        stream.CopyTo(outStream);

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(outStream.ToArray());
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    static void InitButtons(ViewCharacterMenuEquipCombatSkill self)
    {
        static void SetupButton(GameObject btnGo, Sprite iconSprite, Action onClick)
        {
            var childImages = btnGo.GetComponentsInChildren<Image>(true);
            Image? iconImg = null;
            foreach (var img in childImages)
            {
                if (img.gameObject != btnGo)
                {
                    iconImg = img;
                    break;
                }
            }

            if (iconImg != null)
            {
                iconImg.sprite = iconSprite;
                iconImg.overrideSprite = iconSprite;
                iconImg.preserveAspect = true;
                iconImg.color = Color.white;
                iconImg.gameObject.SetActive(true);
            }
            else
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(btnGo.transform, false);
                iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            var iconRect = iconImg.rectTransform;
            iconRect.anchorMin = new Vector2(0.2f, 0.2f);
            iconRect.anchorMax = new Vector2(0.8f, 0.8f);
            iconRect.sizeDelta = Vector2.zero;
            iconRect.anchoredPosition = Vector2.zero;

            foreach (var img in childImages)
            {
                if (img.gameObject != btnGo && img != iconImg)
                {
                    img.gameObject.SetActive(false);
                }
            }

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
            SetupButton(exportGo, GetExportSprite(), () => OnExportClicked(self));
            exportTransform = exportGo.transform;
        }

        if (importTransform == null)
        {
            var importGo = UnityEngine.Object.Instantiate(self.copyPlanButton.gameObject, parent);
            importGo.name = "ImportPlanButton";
            SetupButton(importGo, GetImportSprite(), () => OnImportClicked(self));
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

    internal static void OnDisable()
    {
        var view = UIElement.CharacterMenuEquipCombatSkill.UiBase as ViewCharacterMenuEquipCombatSkill;
        if (view == null)
            return;

        var parent = view.copyPlanButton?.transform?.parent;
        if (parent == null)
            return;

        parent.Find("ExportPlanButton")?.gameObject.SetActive(false);
        parent.Find("ImportPlanButton")?.gameObject.SetActive(false);
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

            // Only Taiwu has a configurable combat skill order plan (changeCombatSkillSortButton).
            List<short>? skillOrder = null;
            if (isTaiwu)
            {
                if (self._combatSkillOrderPlan != null && self._combatSkillOrderPlan.Count > 0)
                {
                    skillOrder = [.. self._combatSkillOrderPlan];
                }

                if (skillOrder != null)
                {
                    while (skillOrder.Count < 54)
                    {
                        skillOrder.Add(-1);
                    }
                    if (skillOrder.Count > 54)
                    {
                        skillOrder = [.. skillOrder.Take(54)];
                    }
                }
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
                Assistance = GetCategoryItems(4),
                SkillOrder = skillOrder
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

            // Check for preset skills that are missing from character
            SkillPlanExportData? preset;
            try
            {
                preset = JsonConvert.DeserializeObject<SkillPlanExportData>(json);
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"Error deserializing preset: {ex}");
                CommonUtils.ShowDialog("Import Failed", "Failed to parse preset file.");
                return;
            }

            if (preset == null)
                return;

            var knownSkills = self.SkillDatas
                .Where(skill => skill != null && !skill.Revoked && skill.TemplateId >= 0)
                .Select(skill => skill.TemplateId)
                .ToHashSet();

            var missingSkills = new List<string>();
            var seenMissingIds = new HashSet<short>();
            void CheckMissingInCategory(List<SkillPlanItem>? items)
            {
                if (items == null)
                    return;

                foreach (var item in items)
                {
                    if (item != null && item.Id >= 0 && !knownSkills.Contains(item.Id) && seenMissingIds.Add(item.Id))
                    {
                        string? skillName = Config.CombatSkill.Instance.GetItemOrDefault(item.Id)?.Name;
                        if (string.IsNullOrWhiteSpace(skillName))
                        {
                            skillName = !string.IsNullOrWhiteSpace(item.Name) ? item.Name : $"ID {item.Id}";
                        }
                        missingSkills.Add(skillName!);
                    }
                }
            }

            CheckMissingInCategory(preset.Neigong);
            CheckMissingInCategory(preset.Attack);
            CheckMissingInCategory(preset.Agility);
            CheckMissingInCategory(preset.Defense);
            CheckMissingInCategory(preset.Assistance);

            if (missingSkills.Count > 0)
            {
                var missingList = string.Join("\n", missingSkills.Take(5));
                if (missingSkills.Count > 5)
                {
                    missingList += $"\n... +{missingSkills.Count - 5} others";
                }

                CommonUtils.ShowDialog(
                    "Import Failed",
                    $"Preset could not be imported properly due to the following missing martial arts from the character:\n\n{missingList}"
                );
            }

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
