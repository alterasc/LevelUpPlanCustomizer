using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using System.Collections.Generic;

namespace LevelUpPlanCustomizer.Patches
{
    class AlwaysAvailableAutoLevelup
    {
        /*
         * Patch for enabling auto-level regardless of difficulty setting          
         */
        [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.IsPlanEnabled))]
        static class LevelUpController_IsPlanEnabled_Patch
        {
            [HarmonyPrefix]
            static bool Prefix(ref bool __result, UnitEntityData unit, bool ignoreSettings = false)
            {
                if (!Main.Settings.AlwaysAutoLevel)
                {
                    return true;
                }
                __result = true;
                return false;
            }
        }

        /*
         * Patch for fixing skill points not being taken when archetype adds skillpoints
         * and spells when spellbook is changed
         */
        [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.ApplyLevelUpActions))]
        static class LevelUpController_ApplyLevelUpActions_Patch
        {
            [HarmonyPrefix]
            static bool Prefix(ref List<ILevelUpAction> __result, LevelUpController __instance, UnitEntityData unit)
            {
                if (!Main.Settings.PatchApplyLevelUpActions)
                {
                    return true;
                }
                List<ILevelUpAction> levelUpActionList = new();
                foreach (ILevelUpAction levelUpAction in __instance.LevelUpActions)
                {
                    if (!levelUpAction.Check(__instance.State, unit.Descriptor))
                    {
                        PFLog.Default.Log("Invalid action: " + levelUpAction?.ToString());
                        if (levelUpAction is SpendSkillPoint && __instance.IsAutoLevelup)
                        {
                            levelUpActionList.Add(levelUpAction);
                            levelUpAction.Apply(__instance.State, unit.Descriptor);
                            __instance.State.OnApplyAction();
                        }
                        if (levelUpAction is SelectSpell && __instance.IsAutoLevelup)
                        {
                            levelUpActionList.Add(levelUpAction);
                            levelUpAction.Apply(__instance.State, unit.Descriptor);
                            __instance.State.OnApplyAction();
                        }
                    }
                    else
                    {
                        levelUpActionList.Add(levelUpAction);
                        levelUpAction.Apply(__instance.State, unit.Descriptor);
                        __instance.State.OnApplyAction();
                    }
                }
                unit.Progression.ReapplyFeaturesOnLevelUp();
                __result = levelUpActionList;
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
                if (!Main.Settings.PatchSelectFeature)
                {
                    return true;
                }
                if (__instance.Item == null)
                {
                    __result = false;
                    return false;
                }
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
