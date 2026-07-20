using System;
using System.Reflection;
using GameData.Domains;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;

namespace Tweaks;

// XXX "Taiwu village merchants are capped at level 1"
// Maybe make the merchants higher tier relative to the player's PE level

[PluginConfig("Tweaks", "nozwock", "0.0.1")]
public class Plugin : TaiwuRemakePlugin
{
    internal static Plugin? Instance
    {
        get => _instance?.TryGetTarget(out var it) == true ? it : null;
        set => _instance = value is null ? null : new(value);
    }

    static WeakReference<Plugin>? _instance;

    Harmony? _harmony;

    public override void Initialize()
    {
        AdaptableLog.Info($"{GetGuid()}: Init Backend");

        Instance = this;

        try
        {
            AdaptableLog.Info($"{GetGuid()}: Applying Harmony patches");

            _harmony = new($"{ModIdStr}.Backend");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

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
    }
}
