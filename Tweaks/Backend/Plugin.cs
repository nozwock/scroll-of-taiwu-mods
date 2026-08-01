using System;
using System.Linq;
using System.Reflection;
using Common;
using GameData.Domains;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;
using Tweaks.Patches;

namespace Tweaks;

[PluginConfig("Tweaks", "nozwock", "0.1.1")]
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

    Harmony? _harmony;

    public override void Initialize()
    {
        AdaptableLog.Info($"{GetGuid()}: Init Backend");

        Instance = this;

        try
        {
            AdaptableLog.Info($"{GetGuid()}: Applying Harmony patches");

            _harmony = new($"{ModIdStr}.Backend");
            _harmony.PatchAllUncategorized(Assembly.GetExecutingAssembly());

            foreach (var m in _harmony.GetPatchedMethods())
            {
                AdaptableLog.Info($"{GetGuid()}: Patched {m.DeclaringType?.FullName}.{m.Name}");
            }
        }
        catch (Exception ex)
        {
            AdaptableLog.Error($"{GetGuid()}: {ex}");
        }

        InitializeBridgeMethods();
    }

    public override void Dispose()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
    }

    public override void OnModSettingUpdate()
    {
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "BonusMaxTeammate",
            ref PatchBonusMaxTeammate._bonusMaxTeammate
        );

        DomainManager.Mod.GetSetting(ModIdStr, "AutoReadBook", ref PatchAutoReadBook._enabled);
        DomainManager.Mod.GetSetting(ModIdStr, "CleanseWugKing", ref PatchCleanseWugKing._enabled);
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "FreeActiveBookReading",
            ref PatchFreeActiveBookReading._enabled
        );
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "FreeCricketWishing",
            ref PatchFreeCricketWishing._enabled
        );
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "FreeMoveBuilding",
            ref PatchFreeMoveBuilding._enabled
        );
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "FreeMoveInAdventure",
            ref PatchFreeMoveInAdventure._enabled
        );
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "MaxCricketDurability",
            ref PatchMaxCricketDurability._enabled
        );
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "MaxItemDurability",
            ref PatchMaxItemDurability._enabled
        );
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "NoCricketPregnancy",
            ref PatchNoCricketPregnancy._enabled
        );
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "NoProfessionSkillCooldown",
            ref PatchNoProfessionSkillCooldown._enabled
        );
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "NoProfessionSkillCost",
            ref PatchNoProfessionSkillCost._enabled
        );
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "NoRandomAristocratBoost",
            ref PatchNoRandomAristocratBoost._enabled
        );
        DomainManager.Mod.GetSetting(ModIdStr, "NoTaiwuNTR", ref PatchNoTaiwuNTR._enabled);
        DomainManager.Mod.GetSetting(ModIdStr, "NoTaiwuRape", ref PatchNoTaiwuRape._enabled);
        DomainManager.Mod.GetSetting(ModIdStr, "NoTransgender", ref PatchNoTransgender._enabled);
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "NoTravelerPalaceDisaster",
            ref PatchNoTravelerPalaceDisaster._enabled
        );
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "MoreActionPoint",
            ref PatchMoreActionPoint._enabled
        );
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "AlwaysPassOnClothing",
            ref PatchAlwaysPassOnClothing._enabled
        );
        DomainManager.Mod.GetSetting(
            ModIdStr,
            "EditWorldSettings",
            ref PatchEditWorldSettings._enabled
        );

        DomainManager.Extra.NoProfessionSkillCooldown = PatchNoProfessionSkillCooldown._enabled;
        DomainManager.Extra.NoProfessionSkillCost = PatchNoProfessionSkillCost._enabled;

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
                    harmony.PatchCategory(PatchType.Name);
                else
                    harmony.UnpatchCategory(PatchType.Name);
            }
            catch (Exception ex)
            {
                AdaptableLog.Error(
                    $"{GetGuid()}: Failed to toggle patch {PatchType.Name}={Enabled}: {ex}"
                );
            }
        }
    }

    void InitializeBridgeMethods()
    {
        DomainManager.Mod.AddModMethod(
            ModIdStr,
            "ResetQiRatio",
            (ctx) =>
            {
                AdaptableLog.Info("Backend: Resetting Qi Ratio");
                DomainManager
                    .Taiwu.GetTaiwu()
                    .SetBaseNeiliProportionOfFiveElements(new([20, 20, 20, 20, 20]), ctx);
            }
        );

        DomainManager.Mod.AddModMethod(
            ModIdStr,
            "ResetDurability",
            (ctx) =>
            {
                foreach (var key in DomainManager.Taiwu.GetTaiwu().GetInventory().Items.Keys)
                {
                    var item = DomainManager.Item.TryGetBaseItem(key);
                    item?.SetCurrDurability(item.GetMaxDurability(), ctx);
                }
            }
        );

        // ViewCombatSkillTree - view Sect Arts dialog
        DomainManager.Mod.AddModMethod(
            ModIdStr,
            "TaiwuLearnSectArts",
            (ctx, data) =>
            {
                // ELocationSectId (Starting with 1)
                data.Get("SectId", out int sectId);
                AdaptableLog.Info($"{GetGuid()}: TaiwuLearnSectArts: SectId={sectId}");

                foreach (var it in Config.CombatSkill.Instance)
                {
                    if (it.SectId == sectId)
                    {
                        DomainManager.Taiwu.GetCombatSkillBookAndRead(ctx, it.TemplateId, 100);
                    }
                }
            }
        );
    }
}
