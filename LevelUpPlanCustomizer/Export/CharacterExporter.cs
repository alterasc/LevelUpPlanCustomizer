﻿using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Entities;
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
        public static PregenUnit ExportAsPregen(UnitEntityData unit, string unitId, out string log)
        {
            StringBuilder sb = new();
            var pregen = new PregenUnit
            {
                UnitId = unitId,
                Gender = unit.Gender,
                m_Race = unit.Progression.m_Race.AssetGuid.ToString(),
                Alignment = unit.Alignment.ValueVisible,
                PregenUnitComponent = new PregenUnitComp
                {
                    PregenName = unit.CharacterName
                }
            };
            //get stats
            getStats(unit, out var attributes, out var levelUps);

            pregen.Strength = attributes[StatType.Strength];
            pregen.Dexterity = attributes[StatType.Dexterity];
            pregen.Constitution = attributes[StatType.Constitution];
            pregen.Intelligence = attributes[StatType.Intelligence];
            pregen.Wisdom = attributes[StatType.Wisdom];
            pregen.Charisma = attributes[StatType.Charisma];

            var levelUpPlan = new LevelUpPlan();
            pregen.LevelUpPlan = levelUpPlan;

            //set ClassOrder
            setClassOrder(levelUpPlan, unit, sb, levelUps);

            //set skills
            setSkills(levelUpPlan, unit, sb);

            // selection
            getSelections(levelUpPlan, unit, sb);

            log = sb.ToString();
            return pregen;
        }

        public static LevelUpPlan ExportAsFeatureList(UnitEntityData unit, out string log)
        {
            StringBuilder sb = new();

            var levelUpPlan = new LevelUpPlan();
            var maybeFeatureList = unit.Blueprint.m_AddFacts.Select(x => x.GetBlueprint()).Where(x => x is BlueprintFeature y)
                .Select(x => x as BlueprintFeature)
                .FirstOrDefault(x => x.GetComponent<AddClassLevels>() != null);

            if (maybeFeatureList == null)
            {
                log = sb.ToString();
                return levelUpPlan;
            }
            levelUpPlan.FeatureList = $"Blueprint:{maybeFeatureList.AssetGuid}:{maybeFeatureList}";

            //get stats
            getStatsForCompanion(unit, out var levelUps);

            //set ClassOrder
            setClassOrder(levelUpPlan, unit, sb, levelUps);

            //set skills
            setSkills(levelUpPlan, unit, sb);

            // selection
            getSelections(levelUpPlan, unit, sb);

            log = sb.ToString();
            return levelUpPlan;
        }

        static void setSkills(LevelUpPlan levelUpPlan, Kingmaker.EntitySystem.Entities.UnitEntityData mc, StringBuilder sb)
        {
            var skills = new StatType[] { StatType.SkillAthletics, StatType.SkillMobility, StatType.SkillThievery, StatType.SkillStealth, StatType.SkillKnowledgeArcana,
                StatType.SkillKnowledgeWorld, StatType.SkillLoreNature, StatType.SkillLoreReligion, StatType.SkillPerception, StatType.SkillPersuasion, StatType.SkillUseMagicDevice};
            var levelSkills = mc.Stats.AllStats
                .Where(x => skills.Contains(x.Type) && x.BaseValue > 0)
                .OrderByDescending(x => x.BaseValue)
                .Select(x => x.Type)
                .ToArray();
            foreach (var item in levelUpPlan.Classes)
            {
                item.Skills = levelSkills;
            }
        }

        static void setClassOrder(LevelUpPlan levelUpPlan, UnitEntityData unit, StringBuilder sb, IDictionary<StatType, int> levelUps)
        {
            var lvl = unit.Progression.CharacterLevel;
            var classOrder = unit.Progression.ClassesOrder;
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
                    m_Archetypes = unit.Progression.GetClassData(characterClass).Archetypes.Select(x => $"Blueprint:{x.AssetGuid}:{x}").ToArray(),
                    Levels = 1
                };
                foreach (var spellbook in unit.Descriptor.m_Spellbooks)
                {
                    if (!spellbook.Key.AllSpellsKnown)
                    {
                        var cClass = spellbook.Key.m_CharacterClass;
                        if (cClass.Guid == characterClass.AssetGuid)
                        {
                            classLevel.m_SelectSpells = spellbook.Value.GetAllKnownSpells()
                                .Where(x => x.SpellLevel > 0).Select(x => $"Blueprint:{x.Blueprint.AssetGuid}:{x.Blueprint}").ToArray();
                        }
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

        static void getSelections(LevelUpPlan levelUpPlan, UnitEntityData unit, StringBuilder sb)
        {
            var selections = unit.Progression.Selections;
            var selectionSkips = new List<BlueprintFeatureSelection>() {
                Utils.GetBlueprint<BlueprintFeatureSelection>("9ee0f6745f555484299b0a1563b99d81"),
                Utils.GetBlueprint<BlueprintFeatureSelection>("ba0e5a900b775be4a99702f1ed08914d"),
                Utils.GetBlueprint<BlueprintFeatureSelection>("1421e0034a3afac458de08648d06faf0")
            };
            foreach (var selection in selections)
            {
                Kingmaker.UnitLogic.FeatureSelectionData value = selection.Value;
                if (selectionSkips.Contains(selection.Key))
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
                        int cLvl = BlueprintProgressionCalculator.FindCharLevel(unit, obj, sel.Key);
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
                                    var enumer = unit.Progression.Features.Enumerable.First(x => x.Blueprint == paramFeature);
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

        static void getStats(UnitEntityData unit, out Dictionary<StatType, int> attributes, out IDictionary<StatType, int> levelUps)
        {
            attributes = unit.Stats.Attributes.ToDictionary(a => a.Type, a => a.BaseValue);
            levelUps = new Dictionary<StatType, int>();
            var statLevelUps = unit.Progression.CharacterLevel / 4;
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

        static void getStatsForCompanion(UnitEntityData unit, out IDictionary<StatType, int> levelUps)
        {
            var attributes = unit.Stats.Attributes.ToDictionary(a => a.Type, a => a.BaseValue);
            levelUps = new Dictionary<StatType, int>();
            var statLevelUps = unit.Progression.CharacterLevel / 4;
            if (statLevelUps > 0)
            {
                if (unit.Stats.Strength > unit.Blueprint.Strength)
                {
                    levelUps[StatType.Strength] = unit.Stats.Strength - unit.Blueprint.Strength;
                }
                if (unit.Stats.Dexterity > unit.Blueprint.Dexterity)
                {
                    levelUps[StatType.Dexterity] = unit.Stats.Dexterity - unit.Blueprint.Dexterity;
                }
                if (unit.Stats.Constitution > unit.Blueprint.Constitution)
                {
                    levelUps[StatType.Constitution] = unit.Stats.Constitution - unit.Blueprint.Constitution;
                }
                if (unit.Stats.Intelligence > unit.Blueprint.Intelligence)
                {
                    levelUps[StatType.Intelligence] = unit.Stats.Intelligence - unit.Blueprint.Intelligence;
                }
                if (unit.Stats.Wisdom > unit.Blueprint.Wisdom)
                {
                    levelUps[StatType.Wisdom] = unit.Stats.Wisdom - unit.Blueprint.Wisdom;
                }
                if (unit.Stats.Charisma > unit.Blueprint.Charisma)
                {
                    levelUps[StatType.Charisma] = unit.Stats.Charisma - unit.Blueprint.Charisma;
                }
            }
        }
    }
}
