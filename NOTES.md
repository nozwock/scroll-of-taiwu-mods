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

# Logging

Relevant:
```
GameData.Utilities.AdaptableLog
LogManager.GetCurrentClassLogger
```

# General

```cs
using Game.Views.CharacterMenu;
using GameData.Domains;

var menu = UnityEngine.Object.FindObjectOfType<ViewCharacterMenuInfo>();
// Doesn't work because DomainManager is available in Backend only (GameData.exe)
var _char = DomainManager.Character.GetElement_Objects(menu._charId);
_char.GetBaseMorality()

EventHelper.Domain.MainThreadDataContext;
DomainManager.Taiwu.GetTaiwu();
DomainManager.Character.GetElement_Objects(int id);
```

Frontend:
```
Game.Views.NewGame.ViewChallenge // Abyss Mode

ViewSwapSoulEditAvatar

ViewCharacterMenuInfo
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
    Search for value `SingletonObject.getInstance<BasicGameData>().ActionPointCurrMonth` (max of 600 coresponding to
    60.0) from Unity Explorer in Cheat Engine targetting `GameData.exe`.

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
