using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using LevelUpPlanCustomizer.Base.Patches;
using Owlcat.Runtime.Core.Logging;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

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


        [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.ApplyLevelUpActions))]
        static class LevelUpController_ApplyLevelUpActions_Patch
        {

            /*
             * Patch for fixing skill points not being taken when archetype adds skillpoints
             * and spells when spellbook is changed
             */
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                if (!Main.Settings.PatchSelectFeature)
                {
                    return instructions;
                }
                try
                {
                    var code = new List<CodeInstruction>(instructions);
                    var fCallVirtFind = 0;
                    System.Type[] parameterTypes = new System.Type[] { typeof(string), typeof(object[]) };
                    MethodInfo logMethod = AccessTools.Method(typeof(LogChannel), nameof(LogChannel.Log), parameterTypes);

                    for (int i = 0; i < code.Count; i++)
                    {
                        if (code[i].Calls(logMethod))
                        {
                            fCallVirtFind = i;
                            break;
                        }
                    }
                    if (fCallVirtFind != 0)
                    {
                        var newCode =
                          new List<CodeInstruction>()
                          {
                                new CodeInstruction(OpCodes.Ldarg_0),
                                new CodeInstruction(OpCodes.Ldarg_1),
                                new CodeInstruction(OpCodes.Ldloc_0),
                                new CodeInstruction(OpCodes.Ldloc_2),
                                CodeInstruction.Call(typeof(AlwaysAvailableAutoLevelup), nameof(AlwaysAvailableAutoLevelup.ApplySkillSpellInsert)),
                          };
                        code.InsertRange(fCallVirtFind, newCode);
                    }
                    return code;
                }
                catch (System.Exception e)
                {
                    LogChannel logChannel = LogChannelFactory.GetOrCreate("Mods");
                    logChannel.Error("Failed to apply transpiler to LevelUpController.ApplyLevelUpActions: {}", e.Message);
                    return instructions;
                }
            }
            /**
             * Postfix to record skills taken and spells learned
             */
            [HarmonyPostfix]
            static void Postfix(ref List<ILevelUpAction> __result, LevelUpController __instance, UnitEntityData unit)
            {
                if (!unit.IsPlayerFaction || !Game.Instance.Player.AllCharacters.Contains(unit) 
                    || __instance.State.Mode == LevelUpState.CharBuildMode.Mythic) return;
                
                var record = GlobalRecord.Instance.ForCharacter(unit);
                var nextCharacterLevel = __instance.State.NextCharacterLevel;
                record.ResetAtLevel(nextCharacterLevel);
                foreach (var action in __result)
                {
                    if (action is SpendSkillPoint spendSkillPoint)
                    {
                        var lvlupaction = new SpendSkillPointAction
                        {
                            Skill = spendSkillPoint.Skill
                        };
                        record.AddAtLevel(nextCharacterLevel, lvlupaction);
                    }
                    else if (action is SelectSpell selectSpell)
                    {
                        var lvlupaction = new SelectSpellAction
                        {
                            Spell = selectSpell.Spell.AssetGuid.m_Guid.ToString(),
                            Spellbook = selectSpell.Spellbook.AssetGuid.m_Guid.ToString()
                        };
                        record.AddAtLevel(nextCharacterLevel, lvlupaction);
                    }
                    else if (action is SpendAttributePoint addStatPoint)
                    {
                        var lvlupaction = new SpendAttributePointAction
                        {
                            Attribute = addStatPoint.Attribute
                        };
                        record.AddAtLevel(nextCharacterLevel, lvlupaction);
                    }
                }
            }
        }

        static void ApplySkillSpellInsert(LevelUpController __instance, UnitEntityData unit, List<ILevelUpAction> levelUpActionList, ILevelUpAction levelUpAction)
        {
            if ((levelUpAction is SpendSkillPoint || levelUpAction is SelectSpell) && __instance.IsAutoLevelup)
            {
                levelUpActionList.Add(levelUpAction);
                levelUpAction.Apply(__instance.State, unit.Descriptor);
                __instance.State.OnApplyAction();
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
                if (unit != null && state.Mode == LevelUpState.CharBuildMode.CharGen && unit.CharacterName == "Player Character")
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }
    }
}
