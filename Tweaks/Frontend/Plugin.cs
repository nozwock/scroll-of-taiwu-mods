using System;
using System.Reflection;
using Game.Views.NewGame;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine;

namespace Tweaks;

[HarmonyPatch]
[PluginConfig("Tweaks", "nozwock", "0.0.1")]
public class Plugin : TaiwuRemakePlugin
{
    static int _originLimit = 15;

    GameObject? _go;
    Harmony? _harmony;

    public override void Initialize()
    {
        AdaptableLog.Info($"{GetGuid()}: Init Frontend");

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
        // NewGameSubPageFeature.MaxPoints
        ModManager.GetSetting(ModIdStr, "OriginLimit", ref _originLimit);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NewGameSubPageFeature), nameof(NewGameSubPageFeature.OnEnable))]
    static void NewGameSubPageFeature_OnEnable_Postfix(NewGameSubPageFeature __instance)
    {
        var self = __instance;
        self.MaxPoints = _originLimit;
    }
}
