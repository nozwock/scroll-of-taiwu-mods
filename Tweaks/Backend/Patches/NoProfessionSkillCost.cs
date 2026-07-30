using GameData.Domains.Extra;
using HarmonyLib;

namespace Tweaks.Patches;

[HarmonyPatchCategory(nameof(PatchNoProfessionSkillCost))]
static class PatchNoProfessionSkillCost
{
    internal static bool _enabled;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ExtraDomain), nameof(ExtraDomain.SetProfessionTestSetting))]
    static void ExtraDomain_SetProfessionTestSetting_Prefix(ref bool noSkillCost)
    {
        noSkillCost = true;
    }
}
