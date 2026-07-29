# Mod Entry

Subclass of `TaiwuModdingLib.Core.Plugin.TaiwuRemakePlugin` is the mod entry.

```cs
using TaiwuModdingLib.Core.Plugin;

[PluginConfig("MyModName", "Author", "0.0.1")]
class Plugin : TaiwuRemakePlugin
{
    public override void Initialize() { }
    public override void Dispose() { }
}
```

## Mod IPC Communication

### Backend

```cs
// Parameter-less
DomainManager.Mod.AddModMethod(ModIdStr, "MyMethodName", (ctx) => {});

// With parameter
DomainManager.Mod.AddModMethod(ModIdStr, "MyMethodName", (ctx, data) => {
    data.Get("Param", out int param);
    data.Get<SerializableType>("SerializableParam", out var serializableParam);
});
```

### Frontend

```cs
// Parameter-less
ModDomainMethod.Call.CallModMethod(ModIdStr, "MyMethodName");

// With parameter
var mod = new SerializableModData();
mod.Set("Param", 42);
ModDomainMethod.Call.CallModMethodWithParam(ModIdStr, "MyMethodName", mod);
```

# Logging

Relevant:

```
GameData.Utilities.AdaptableLog
LogManager.GetCurrentClassLogger
```

# General

Frontend:

```
SingletonObject.getInstance<BasicGameData>()
    .ActionPointCurrMonth

ViewEventWindow
    .eventContent

CommonUtils
    .ShowDialog()

ViewCricketWishing // Cricket wishing dialog in Cricket Chamber
Game.Views.Obtain.ViewObtain // Obtained items UI
Game.Views.NewGame.ViewChallenge // Abyss Mode

ViewSwapSoulEditAvatar

ViewCharacterMenuInfo
    FeatureScroll .featureScroll
    ._characterMenuInfoDisplayData
        .CharacterDisplayData

// Building Move/Remove Planning
Game.Views.Building.ViewBuildingArea
        .ConfirmResetBuild()
            calls BuildingAreaResourceChange.RefreshResourceChangeOnPlan()

// Overworld bottom UI with energy ball, etc.
Game.Views.Bottom.ViewBottom

// Energy ball
Game.Views.Looping.ViewLooping

BuildingBlockItem // Shared
    .MoveBuildCostResourceRate

// Celestial Blessing (9th reincarnation)
GameData.Domains.Taiwu.TaiwuDomainMethod.Call.TaiwuAddFeature(232);
```

Backend:

```
// Building Move Planning
BuildingDomain.ConfirmPlanBuilding()

TaiwuDomain.GetTaiwuVillageBaseSpace()
```

# Game's IPC

Game's IPC b/w main Unity game process and `GameData.exe` backend process.

Frontend:

```cs
GameDataBridge.RegisterListener()
GameDataBridge.AddMethodCall()
```

Backend: Prefix hook `GameDataBridge.ProcessMethodCall()` and handle your method calls in there if domain id is yours.

On the backend, you can see the pipe being setup in `GameDataBridge.Initialize()` as `Slaver.Connect()`.

# Save Data

Investigate starting with the abstract class `ArchiveFileBase`.

```
GameData.ArchiveData.ArchiveFileBase
    GlobalArchiveFile
    LocalArchiveFile
```

`GameData.Domains.Taiwu.TaiwuDomain.OnLoadWorld()`.

And besides the metadata header at the top of a savefile, rest is just compressed sqlite database.

# Cheat Engine

- Energy/Action Points:
  Search for value `SingletonObject.getInstance<BasicGameData>().ActionPointCurrMonth` (max of 600 corresponding to
  60.0) from Unity Explorer in Cheat Engine targeting `GameData.exe`.

  The ball in the middle is named `TimeBall`.

Willpower, Comprehension are u16.

Mood:

```
01 01 03 D2 03 0A 0B 03
01 01 00 EF 04 0F 06 02
         ^

byte at [u16 mindset + 23 bytes]
150 ~ Miserable
180 ~ Sorrowful
210 ~ Depressed
240 ~ Ordinary
```

## Glossary

- `Wug` : Gu; symbiotic insects
- `WugKing` : Prime Gu; T1 Gu
