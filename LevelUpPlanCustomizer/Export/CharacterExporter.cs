using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Utility;
using LevelUpPlanCustomizer.Base.Export;
using LevelUpPlanCustomizer.Schemas.v1;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LevelUpPlanCustomizer.Export
{
    internal class CharacterExporter
    {
        public static PregenUnit ExportMC(string unitId, out string log)
        {
            StringBuilder sb = new();
            var player = Game.Instance.Player;
            Kingmaker.EntitySystem.Entities.UnitEntityData mc = player.MainCharacter.Value;
            var stats = mc.Stats;

            var pregen = new PregenUnit();

            pregen.UnitId = unitId;
            pregen.Gender = mc.Gender;
            pregen.m_Race = mc.Progression.m_Race.AssetGuid.ToString();
            pregen.Alignment = mc.Alignment.ValueVisible;
            pregen.PregenUnitComponent = new PregenUnitComp
            {
                PregenName = mc.CharacterName
            };
            //get stats
            getStats(mc, out var attributes, out var levelUps);

            pregen.Strength = attributes[StatType.Strength];
            pregen.Dexterity = attributes[StatType.Dexterity];
            pregen.Constitution = attributes[StatType.Constitution];
            pregen.Intelligence = attributes[StatType.Intelligence];
            pregen.Wisdom = attributes[StatType.Wisdom];
            pregen.Charisma = attributes[StatType.Charisma];

            var levelUpPlan = new LevelUpPlan();
            pregen.LevelUpPlan = levelUpPlan;

            //set ClassOrder
            setClassOrder(levelUpPlan, mc, sb, levelUps);

            //set skills
            setSkills(levelUpPlan, mc, sb);

            // selection
            getSelections(levelUpPlan, mc, sb);

            log = sb.ToString();
            return pregen;
        }

        static void setSkills(LevelUpPlan levelUpPlan, Kingmaker.EntitySystem.Entities.UnitEntityData mc, StringBuilder sb)
        {
            foreach (var item in levelUpPlan.Classes)
            {
                item.Skills = new StatType[] { StatType.SkillAthletics, StatType.SkillMobility, StatType.SkillThievery, StatType.SkillStealth, StatType.SkillKnowledgeArcana,
                StatType.SkillKnowledgeWorld, StatType.SkillLoreNature, StatType.SkillLoreReligion, StatType.SkillPerception, StatType.SkillPersuasion, StatType.SkillUseMagicDevice};
            }
        }

        static void setClassOrder(LevelUpPlan levelUpPlan, Kingmaker.EntitySystem.Entities.UnitEntityData mc, StringBuilder sb, IDictionary<StatType, int> levelUps)
        {
            var lvl = mc.Progression.CharacterLevel;
            var classOrder = mc.Progression.ClassesOrder;
            var tmpList = new List<ClassLevel>();
            //set class order
            var nonMythicClasses = classOrder.Where(x => !x.IsMythic).ToList();
            nonMythicClasses.ForEach(x => sb.AppendLine($"Took class: {x}"));
            for (int i = 0; i < nonMythicClasses.Count; i++)
            {
                BlueprintCharacterClass characterClass = nonMythicClasses[i];
                var classLevel = new ClassLevel
                {
                    m_CharacterClass = $"Blueprint:{characterClass.AssetGuid}:{characterClass}",
                    m_Archetypes = mc.Progression.GetClassData(characterClass).Archetypes.Select(x => $"Blueprint:{x.AssetGuid}:{x}").ToArray(),
                    Levels = 1
                };
                foreach (var spellbook in mc.Descriptor.m_Spellbooks)
                {
                    var cClass = spellbook.Key.m_CharacterClass;
                    if (cClass.Guid == characterClass.AssetGuid)
                    {
                        classLevel.m_SelectSpells = spellbook.Value.GetAllKnownSpells().Select(x => $"Blueprint:{x.Blueprint.AssetGuid}:{x.Blueprint}").ToArray();
                    }
                }
                //set stat levelups
                try
                {
                    if ((i + 1) % 4 == 0)
                    {
                        var stat = levelUps.Keys.First();
                        classLevel.LevelsStat = stat;
                        if (levelUps[stat] == 1)
                        {
                            levelUps.Remove(stat);
                        }
                        else
                        {
                            levelUps[stat] = levelUps[stat] - 1;
                        }
                    }
                }
                catch (System.Exception ex)
                {

                    sb.AppendLine(ex.Message);
                }
                tmpList.Add(classLevel);
            }
            levelUpPlan.Classes = tmpList.ToArray();
        }

        static void getSelections(LevelUpPlan levelUpPlan, Kingmaker.EntitySystem.Entities.UnitEntityData mc, StringBuilder sb)
        {
            var selections = mc.Progression.Selections;
            var mythicFeatSelection = Utils.GetBlueprint<BlueprintFeatureSelection>("9ee0f6745f555484299b0a1563b99d81");
            var mythicAbilitySelection = Utils.GetBlueprint<BlueprintFeatureSelection>("ba0e5a900b775be4a99702f1ed08914d");

            foreach (var selection in selections)
            {
                Kingmaker.UnitLogic.FeatureSelectionData value = selection.Value;
                if (selection.Key == mythicFeatSelection || selection.Key == mythicAbilitySelection)
                {
                    continue;
                }
                if (value.Source.Blueprint is BlueprintRace objRace)
                {
                    foreach (var sel in value.m_SelectionsByLevel)
                    {
                        foreach (var selectedItem in sel.Value)
                        {
                            var slvl = 0;
                            if (sel.Key > 1)
                            {
                                slvl = sel.Key - 1;
                            }
                            sb.AppendLine($"At lvl {slvl + 1} in race {value.Source.Blueprint} selection {selection.Key} took {selectedItem}");
                            SelectionClass[] planSelections = levelUpPlan.Classes[slvl].Selections ?? new SelectionClass[0];
                            var selClass = new SelectionClass()
                            {
                                m_Selection = $"Blueprint:{selection.Key.AssetGuid}:{selection.Key}",
                                m_Features = new string[] { $"Blueprint:{selectedItem.AssetGuid}:{selectedItem}" }
                            };
                            planSelections = planSelections.Append(selClass).ToArray();
                            levelUpPlan.Classes[slvl].Selections = planSelections;
                        }
                    }

                }
                if (value.Source.Blueprint is BlueprintProgression obj)
                {
                    foreach (var sel in value.m_SelectionsByLevel)
                    {
                        int cLvl = BlueprintProgressionCalculator.FindCharLevel(mc, obj, sel.Key);
                        foreach (var selectedItem in sel.Value)
                        {
                            sb.AppendLine($"At {sel.Key} (clvl {cLvl}) in progression {value.Source.Blueprint} selection {selection.Key} took {selectedItem}");
                            SelectionClass[] planSelections = levelUpPlan.Classes[cLvl - 1].Selections ?? new SelectionClass[0];
                            var selClass = new SelectionClass()
                            {
                                m_Selection = $"Blueprint:{selection.Key.AssetGuid}:{selection.Key}",
                                m_Features = new string[] { $"Blueprint:{selectedItem.AssetGuid}:{selectedItem}" }
                            };
                            planSelections = planSelections.Append(selClass).ToArray();

                            if (selectedItem is BlueprintParametrizedFeature paramFeature)
                            {
                                try
                                {
                                    var selParamed = new SelectionClass()
                                    {
                                        m_Selection = $"Blueprint:{selection.Key.AssetGuid}:{selection.Key}",
                                        m_Features = new string[] { $"Blueprint:{selectedItem.AssetGuid}:{selectedItem}" }
                                    };
                                    selParamed.IsParametrizedFeature = true;
                                    selParamed.m_ParametrizedFeature = $"Blueprint:{selectedItem.AssetGuid}:{selectedItem}";
                                    var enumer = mc.Progression.Features.Enumerable.First(x => x.Blueprint == paramFeature);
                                    if (paramFeature.ParameterType == FeatureParameterType.WeaponCategory)
                                    {
                                        sb.AppendLine($"At {sel.Key} (clvl {cLvl}) in progression " +
                                            $"{value.Source.Blueprint} selection {selection.Key} " +
                                            $"took {selectedItem}, parametrized {enumer.Param.WeaponCategory}");
                                        selParamed.ParamWeaponCategory = enumer.Param.WeaponCategory.Value;
                                    }
                                    else if (paramFeature.ParameterType == FeatureParameterType.SpellSchool)
                                    {
                                        sb.AppendLine($"At {sel.Key} (clvl {cLvl}) in progression " +
                                            $"{value.Source.Blueprint} selection {selection.Key} " +
                                            $"took {selectedItem}, parametrized {enumer.Param.SpellSchool}");
                                        selParamed.ParamSpellSchool = enumer.Param.SpellSchool.Value;
                                    }
                                    else if (paramFeature.ParameterType == FeatureParameterType.Skill)
                                    {
                                        sb.AppendLine($"At {sel.Key} (clvl {cLvl}) in progression " +
                                            $"{value.Source.Blueprint} selection {selection.Key} " +
                                            $"took {selectedItem}, parametrized {enumer.Param.StatType.Value}");
                                        selParamed.Stat = enumer.Param.StatType.Value;
                                    }
                                    else if (paramFeature.ParameterType == FeatureParameterType.FeatureSelection)
                                    {
                                        sb.AppendLine($"At {sel.Key} (clvl {cLvl}) in progression " +
                                            $"{value.Source.Blueprint} selection {selection.Key} " +
                                            $"took {selectedItem}, parametrized {enumer.Param.Blueprint}");
                                    }
                                    planSelections = planSelections.Append(selParamed).ToArray();
                                }
                                catch (System.Exception ex)
                                {

                                    sb.AppendLine(ex.Message);
                                }
                            }
                            levelUpPlan.Classes[cLvl - 1].Selections = planSelections;
                        }
                    }

                }
            }
        }

        static void getStats(Kingmaker.EntitySystem.Entities.UnitEntityData mc, out Dictionary<StatType, int> attributes, out IDictionary<StatType, int> levelUps)
        {
            attributes = mc.Stats.Attributes.ToDictionary(a => a.Type, a => a.BaseValue);
            levelUps = new Dictionary<StatType, int>();
            var statLevelUps = mc.Progression.CharacterLevel / 4;
            if (statLevelUps > 0)
            {
                IDictionary<int, int> pointBuy = new Dictionary<int, int>()
                {
                    {7, -4},
                    {8, -2},
                    {9, -1},
                    {10, 0},
                    {11, 1},
                    {12, 2},
                    {13, 3},
                    {14, 5},
                    {15, 7},
                    {16, 10},
                    {17, 13},
                    {18, 17}
                };
                var statPB = 0;

                foreach (var attrKey in attributes.Keys.ToList())
                {
                    var baseValue = attributes[attrKey];
                    if (baseValue > 18)
                    {
                        levelUps.TryGetValue(attrKey, out var ups);
                        levelUps[attrKey] = ups + (baseValue - 18);
                        statLevelUps -= (baseValue - 18);
                        attributes[attrKey] = 18;
                    }
                    statPB += pointBuy[attributes[attrKey]];
                }

                while (statLevelUps > 0)
                {
                    var highest = attributes.MaxBy(a => a.Value);
                    attributes[highest.Key]--;
                    var newV = attributes[highest.Key];
                    statPB -= pointBuy[newV + 1] - pointBuy[newV];
                    levelUps.TryGetValue(highest.Key, out var ups);
                    levelUps[highest.Key] = ups + 1;
                    statLevelUps--;
                }
                if (statPB != 25)
                {
                    // log it
                }
            }
        }
    }
}
