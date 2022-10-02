using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;

namespace LevelUpPlanCustomizer.Patches
{
    class AlwaysAvailableAutoLevelup
    {
        /*
         *Patch for enabling auto-level regardless of difficulty setting          
         */
        [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.IsPlanEnabled))]
        static class LevelUpController_IsPlanEnabled_Patch
        {
            [HarmonyPrefix]
            static bool Prefix(ref bool __result, UnitEntityData unit, bool ignoreSettings = false)
            {
                __result = true;
                return false;
            }
        }

        /**
         * Patch to fix selections from archetypes failing to get checked as valid
         * at class level 1 after character level 1.
         * (For example Scaled Fist bonus feat).
         */
        [HarmonyPatch(typeof(SelectFeature), nameof(SelectFeature.Check))]
        static class SelectFeature_Check_Patch
        {
            [HarmonyPrefix]
            static bool Prefix(ref bool __result, SelectFeature __instance, LevelUpState state, UnitDescriptor unit)
            {
                if (__instance.Item == null)
                {
                    __result = false;
                    return false;
                }
                FeatureSelectionState selectionState = __instance.GetSelectionState(state);
                var controller = Kingmaker.Game.Instance?.LevelUpController;
                if (controller != null && controller.IsAutoLevelup)
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }
    }
}
