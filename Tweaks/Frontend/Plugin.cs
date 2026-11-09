using System;
using System.Linq;
using Common;
using Config;
using Game.Components.SortAndFilter.CharacterLocationDisplayData;
using Game.Views.Building.BuildingManage;
using Game.Views.EventWindow;
using GameData.Domains.Item;
using GameData.Domains.Mod;
using GameData.Domains.Taiwu;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;
using Tweaks.Patches;
using UnityEngine;

namespace Tweaks;

[HarmonyPatch]
[PluginConfig("Tweaks", "nozwock", "0.1.2")]
public class Plugin : TaiwuRemakePlugin
{
    internal static Plugin? Instance
    {
        get => _instance?.TryGetTarget(out var it) == true ? it : null;
        set => _instance = value is null ? null : new(value);
    }

    static WeakReference<Plugin>? _instance;
    static readonly ToggleablePatches _toggleablePatches = ToggleablePatches.GetAllWithAttribute(
        typeof(HarmonyPatchCategory)
    );

    GameObject? _go;
    Harmony? _harmony;

    public override void Initialize()
    {
        AdaptableLog.Info($"{GetGuid()}: Init Frontend");

        Instance = this;

        try
        {
            AdaptableLog.Info($"{GetGuid()}: Applying Harmony patches");

            _harmony = new($"{ModIdStr}.Frontend");
            _harmony.PatchAllUncategorized();

            foreach (var m in HarmonyExtensions.GetPatchedMethods(_harmony))
            {
                AdaptableLog.Info($"{GetGuid()}: Patched {m.DeclaringType?.FullName}.{m.Name}");
            }
        }
        catch (Exception ex)
        {
            AdaptableLog.Error($"{GetGuid()}: {ex}");
        }

        _go = new($"{GetGuid()}.Frontend");
        UnityEngine.Object.DontDestroyOnLoad(_go);
        _go.AddComponent<PluginMono>();
    }

    public override void Dispose()
    {
        PatchInstantPlaceBuilding.OnDisable();
        PatchInstantRemoveBuilding.OnDisable();
        PatchShareSkillPlans.OnDisable();

        if (_harmony != null)
            HarmonyExtensions.UnpatchSelf(_harmony);
        _harmony = null;

        UnityEngine.Object.Destroy(_go);
        _go = null;
    }

    public override void OnModSettingUpdate()
    {
        ModManager.GetSetting(
            ModIdStr,
            "BackgroundTraitLimit",
            ref PatchBackgroundTraitLimit._backgroundTraitLimit
        );

        ModManager.GetSetting(ModIdStr, "FreeCricketWishing", ref PatchFreeCricketWishing._enabled);
        ModManager.GetSetting(ModIdStr, "FreeMoveBuilding", ref PatchFreeMoveBuilding._enabled);
        ModManager.GetSetting(
            ModIdStr,
            "FreeMoveInAdventure",
            ref PatchFreeMoveInAdventure._enabled
        );
        ModManager.GetSetting(
            ModIdStr,
            "InstantPlaceBuilding",
            ref PatchInstantPlaceBuilding._enabled
        );
        ModManager.GetSetting(
            ModIdStr,
            "InstantRemoveBuilding",
            ref PatchInstantRemoveBuilding._enabled
        );
        ModManager.GetSetting(
            ModIdStr,
            "LeftAlignedEventText",
            ref PatchLeftAlignedEventText._enabled
        );
        ModManager.GetSetting(
            ModIdStr,
            "AlwaysVisiblePELevel",
            ref PatchAlwaysVisiblePELevel._enabled
        );
        ModManager.GetSetting(ModIdStr, "MoreActionPoint", ref PatchMoreActionPoint._enabled);
        ModManager.GetSetting(ModIdStr, "EditWorldSettings", ref PatchEditWorldSettings._enabled);
        ModManager.GetSetting(ModIdStr, "ShareSkillPlans", ref PatchShareSkillPlans._enabled);

        if (UIElement.EventWindow.UiBase is ViewEventWindow eventWindow)
        {
            eventWindow.eventContent.alignment = PatchLeftAlignedEventText._enabled
                ? TMPro.TextAlignmentOptions.Left
                : TMPro.TextAlignmentOptions.Center;
        }

        if (_harmony != null)
            InitializeCategoryPatches(_harmony);
    }

    void InitializeCategoryPatches(Harmony harmony)
    {
        foreach (
            var (PatchType, Enabled, _) in _toggleablePatches
                .EnumeratePatchStates()
                .Where(it => it.Changed)
        )
        {
            AdaptableLog.Info($"{GetGuid()}: Toggle Patch: enable={Enabled} ({PatchType.Name})");

            try
            {
                if (Enabled)
                {
                    harmony.PatchCategory(PatchType);
                }
                else
                {
                    harmony.UnpatchCategory(PatchType);

                    if (PatchType == typeof(PatchInstantRemoveBuilding))
                    {
                        PatchInstantRemoveBuilding.OnDisable();
                    }
                    else if (PatchType == typeof(PatchShareSkillPlans))
                    {
                        PatchShareSkillPlans.OnDisable();
                    }
                    else if (PatchType == typeof(PatchInstantPlaceBuilding))
                    {
                        PatchInstantPlaceBuilding.OnDisable();
                    }
                }
            }
            catch (Exception ex)
            {
                AdaptableLog.Error(
                    $"{GetGuid()}: Failed to toggle patch {PatchType.Name}={Enabled}: {ex}"
                );
            }
        }
    }

    // Exposed to be called from UnityExplorer's console, for now
    // https://steamcommunity.com/sharedfiles/filedetails/?id=3748518411
    public static bool TaiwuLearnSectArts(int sectId) =>
        Enum.IsDefined(typeof(ELocationSectId), sectId)
        && TaiwuLearnSectArts((ELocationSectId)sectId);

    public static bool TaiwuLearnSectArts(ELocationSectId sectId)
    {
        var isInGameWorld = SingletonObject.getInstance<BasicGameData>().TaiwuCharId > 0;
        if (!isInGameWorld || Instance?.ModIdStr is not { } modId)
            return false;

        var mod = new SerializableModData();
        // ELocationSectId (Starting with 1)
        mod.Set("SectId", (int)sectId + 1);

        ModDomainMethod.Call.CallModMethodWithParam(modId, nameof(TaiwuLearnSectArts), mod);

        return true;
    }

    public static bool EnsureWesternTreasures()
    {
        var isInGameWorld = SingletonObject.getInstance<BasicGameData>().TaiwuCharId > 0;
        if (!isInGameWorld || Instance?.ModIdStr is not { } modIdStr)
            return false;

        ModDomainMethod.Call.CallModMethod(modIdStr, nameof(EnsureWesternTreasures));

        return true;
    }

    public static bool GetGiftForJiXi(int amount)
    {
        var isInGameWorld = SingletonObject.getInstance<BasicGameData>().TaiwuCharId > 0;
        if (!isInGameWorld)
            return false;

        amount = amount < 1 ? 1 : amount;

        // JiXi Eerie Mystery II Gift Item
        var ancientJadeFox = new ItemKey(12, 0, 373, 285767);
        TaiwuDomainMethod.Call.WarehouseAdd(ancientJadeFox, amount);
        TaiwuDomainMethod.Call.TransferItem(
            -1,
            (sbyte)ItemSourceType.Warehouse,
            (sbyte)ItemSourceType.Inventory,
            ancientJadeFox,
            amount
        );

        return true;
    }

    public static bool EnsureMinimumQualifications(short value)
    {
        var isInGameWorld = SingletonObject.getInstance<BasicGameData>().TaiwuCharId > 0;
        if (!isInGameWorld || Instance?.ModIdStr is not { } modIdStr)
            return false;

        var mod = new SerializableModData();
        mod.Set("Value", value);

        ModDomainMethod.Call.CallModMethodWithParam(
            modIdStr,
            nameof(EnsureMinimumQualifications),
            mod
        );

        return true;
    }
}

class PluginMono : MonoBehaviour
{
    void Update()
    {
        var id = Plugin.Instance?.ModIdStr ?? "";

        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                AdaptableLog.Info($"{Plugin.Instance?.GetGuid()}: Calling ResetQiRatio");
                ModDomainMethod.Call.CallModMethod(id, "ResetQiRatio");
            }
            else if (Input.GetKeyDown(KeyCode.F10))
            {
                AdaptableLog.Info($"{Plugin.Instance?.GetGuid()}: Calling ResetDurability");
                ModDomainMethod.Call.CallModMethod(id, "ResetDurability");
            }
        }
    }
}
