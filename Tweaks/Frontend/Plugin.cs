using System;
using System.Reflection;
using Game.Views.NewGame;
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
    static int _backgroundTraitLimit = 0;

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
        ModManager.GetSetting(ModIdStr, "BackgroundTraitLimit", ref _backgroundTraitLimit);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NewGameSubPageFeature), nameof(NewGameSubPageFeature.OnEnable))]
    static void NewGameSubPageFeature_OnEnable_Postfix(NewGameSubPageFeature __instance)
    {
        var self = __instance;
        if (_backgroundTraitLimit > 0)
            self.MaxPoints = _backgroundTraitLimit;
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
