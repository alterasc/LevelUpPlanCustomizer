using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using LevelUpPlanCustomizer.Base.Import;

namespace LevelUpPlanCustomizer.Patches
{
    class PlanUpdater
    {
        [HarmonyPatch(typeof(BlueprintsCache), "Init")]
        internal static class BlueprintsCache_Init_Patch
        {
            static bool Initialized;

            [HarmonyPostfix]
            static void Postfix()
            {
                if (Initialized) return;
                Initialized = true;

                CharacterImporter.UpdateLevelUpPlans();
                CharacterImporter.UpdatePregens();
            }

            
        }
    }
}
