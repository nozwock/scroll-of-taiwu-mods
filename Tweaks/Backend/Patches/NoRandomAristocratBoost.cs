using GameData.Domains.Taiwu.Profession;
using HarmonyLib;
using Redzen.Random;

namespace Tweaks.Patches;

[HarmonyPatch]
static class PatchNoRandomAristocratBoost // Also called "Promotion Advocate"
{
    // ProfessionRelatedConstants.AristocratGradeRange
    // ViewProfessionMask
    //     TeammateRise // Adovcate
    //         ProfessionSkillController.ShowTeammateUI
    //             ProfessionSkillController.ExecuteSkillDirect
    //             ProfessionSkillController.StartShowSkillAnim
    // ShowProfessionTeammateUI
    //
    // Unsuccessful with search in the above, looked towards character creation logic in Character class, and found:
    // CharacterCreation.CreateMainAttributes
    //     Stack trace for which reveals:
    //     EventHelper.RecreateLifeSkillQualifications
    //     EventHelper.RecreateCombatSkillQualifications
    //     EventHelper.RecreateMainAttributes
    //     EventHelper.RecreateFeatures
    //
    // In these functions, The `growingGrade` and other params are passed by some function like this that are defined in
    // ./Event/EventLib/Taiwu_EventPackage_zhiye1.dll:
    // ConchShip.EventConfig.Taiwu.TaiwuEvent_d535b82cfb6642a8a70d60c358e98358.OnOption1Select()
    // Which calls in `EventHelper.GetGrowingGradeBySeniority()` for the passed `growingGrade`.
    //
    // EventHelper.GetGrowingGradeBySeniority
    //      ProfessionData.SeniorityToGrowingGrade
    //
    // By which point I've come back full circle since SeniorityToGrowingGrade is what I had patched initially but
    // thought it didn't work since the total attribute points for the now patched growinGrade=8 (Exalted) were
    // sometimes lower than grade 7 or below. But that's just randomness I guess.
    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(ProfessionData),
        nameof(ProfessionData.SeniorityToGrowingGrade),
        [typeof(int), typeof(IRandomSource)]
    )]
    static bool ProfessionData_SeniorityToGrowingGrade(int seniority, ref sbyte __result)
    {
        __result = (sbyte)(
            ProfessionRelatedConstants.AristocratGradeRange[1]
            + ProfessionData.SeniorityToGrowingGrade(seniority)
        );
        return false;
    }
}
