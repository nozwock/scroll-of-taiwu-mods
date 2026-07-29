using System;
using System.Reflection;
using Game.Components.SortAndFilter.CharacterLocationDisplayData;
using GameData.Domains.Mod;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine;

namespace Tweaks;

[HarmonyPatch]
[PluginConfig("Tweaks", "nozwock", "0.0.1")]
public class Plugin : TaiwuRemakePlugin
{
    internal static Plugin? Instance
    {
        get => _instance?.TryGetTarget(out var it) == true ? it : null;
        set => _instance = value is null ? null : new(value);
    }

    static WeakReference<Plugin>? _instance;

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

        _go = new($"{GetGuid()}.Frontend");
        UnityEngine.Object.DontDestroyOnLoad(_go);
        _go.AddComponent<PluginMono>();
    }

    public override void Dispose()
    {
        _harmony?.UnpatchSelf();
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
