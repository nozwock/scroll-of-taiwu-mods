using GameData.Common;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatch]
static class PatchNoProfessionSkillCooldown
{
    // ProfessionData
    //      ProfessionSkillItem .GetSkillConfig()
    //          .SkillCooldown
    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(GameContext),
        nameof(GameContext.NoProfessionSkillCooldown),
        MethodType.Getter
    )]
    static bool GameContext_get_NoProfessionSkillCooldown_Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}
