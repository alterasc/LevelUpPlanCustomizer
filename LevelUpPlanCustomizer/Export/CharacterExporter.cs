using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Utility;
using LevelUpPlanCustomizer.Common;
using LevelUpPlanCustomizer.Patches;
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
            GetStats(unit, out var attributes, out var levelUps);

            pregen.Strength = attributes[StatType.Strength];
            pregen.Dexterity = attributes[StatType.Dexterity];
            pregen.Constitution = attributes[StatType.Constitution];
            pregen.Intelligence = attributes[StatType.Intelligence];
            pregen.Wisdom = attributes[StatType.Wisdom];
            pregen.Charisma = attributes[StatType.Charisma];

            var levelUpPlan = new LevelUpPlan();
            pregen.LevelUpPlan = levelUpPlan;

            //set ClassOrder
            SetClassOrder(levelUpPlan, unit, sb, levelUps);

            //set skills
            SetSkills(levelUpPlan, unit, sb);

            // selection
            GetSelections(levelUpPlan, unit, sb);

            log = sb.ToString();
            pregen.Schema = "https://raw.githubusercontent.com/alterasc/LevelUpPlanCustomizer/main/schemas/v1/PregenV1.json";
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
            GetStatsForCompanion(unit, out var levelUps);

            //set ClassOrder
            SetClassOrder(levelUpPlan, unit, sb, levelUps);

            //set skills
            SetSkills(levelUpPlan, unit, sb);

            // selection
            GetSelections(levelUpPlan, unit, sb);

            log = sb.ToString();
            levelUpPlan.Schema = "https://raw.githubusercontent.com/alterasc/LevelUpPlanCustomizer/main/schemas/v1/LevelUpPlanV1.json";
            return levelUpPlan;
        }

        static void SetSkills(LevelUpPlan levelUpPlan, UnitEntityData mc, StringBuilder sb)
        {
            var skills = new StatType[] { StatType.SkillAthletics, StatType.SkillMobility, StatType.SkillThievery, StatType.SkillStealth, StatType.SkillKnowledgeArcana,
                StatType.SkillKnowledgeWorld, StatType.SkillLoreNature, StatType.SkillLoreReligion, StatType.SkillPerception, StatType.SkillPersuasion, StatType.SkillUseMagicDevice};
            //all skills character put points in descending order of points invested
            var levelSkills = mc.Stats.AllStats
                .Where(x => skills.Contains(x.Type) && x.BaseValue > 0)
                .OrderByDescending(x => x.BaseValue)
                .Select(x => x.Type)
                .ToArray();

            for (var i = 0; i < levelUpPlan.Classes.Count(); i++)
            {
                var classTaken = levelUpPlan.Classes[i];
                var characterLevel = i + 1;
                var charRecord = GlobalRecord.Instance.ForCharacter(mc).LevelUpActions.TryGetValue(characterLevel, out var actions);
                if (actions != null)
                {
                    classTaken.Skills = actions.OfType<SpendSkillPointAction>().Select(x => x.Skill).ToArray();
                }
                else
                {
                    //if no record of skills taken was found, level all skills leveled.
                    classTaken.Skills = levelSkills;
                }
            }
        }

        static void SetClassOrder(LevelUpPlan levelUpPlan, UnitEntityData unit, StringBuilder sb, IDictionary<int, StatType> levelUps)
        {
            var lvl = unit.Progression.CharacterLevel;
            var classOrder = unit.Progression.ClassesOrder;
            var tmpList = new List<ClassLevel>();
            //set class order
            var nonMythicClasses = classOrder.Where(x => !x.IsMythic).ToList();
            nonMythicClasses.ForEach(x => sb.AppendLine($"Took class: {x}"));
            for (int i = 0; i < nonMythicClasses.Count; i++)
            {
                var nextLevel = i + 1;
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
                        GlobalRecord.Instance.ForCharacter(unit).LevelUpActions.TryGetValue(nextLevel, out var recActions);

                        List<string> spellsTolearn = new();
                        if (recActions != null)
                        {
                            spellsTolearn = recActions.OfType<SelectSpellAction>().Where(x => x.Spellbook == spellbook.Key.AssetGuid.m_Guid.ToString())
                                .Select(x => ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(BlueprintGuid.Parse(x.Spell)))
                                .NotNull()
                                .Select(x => $"Blueprint:{x.AssetGuid}:{x}").ToList();
                        }


                        //trying to get additional spells to be safe - for prepared arcane casters who can learn spells from scrolls
                        //or if there's no record
                        var classWithSpellBook = unit.Progression.Classes.FirstOrDefault(x => x.Spellbook == spellbook.Key && x.CharacterClass == characterClass);
                        if (classWithSpellBook != null && (spellbook.Key.CanCopyScrolls || spellsTolearn.Count == 0))
                        {
                            var addSpells = spellbook.Value.GetAllKnownSpells()
                                .Where(x => !x.IsFromMythicSpellList)
                                .Where(x => x.SpellLevel > 0)
                                .OrderByDescending(x => x.SpellLevel)
                                .Select(x => $"Blueprint:{x.Blueprint.AssetGuid}:{x.Blueprint}")
                                .Where(x => !spellsTolearn.Contains(x));
                            spellsTolearn.AddRange(addSpells);

                        }
                        if (spellsTolearn.Count > 0)
                        {
                            classLevel.m_SelectSpells = spellsTolearn.ToArray();
                        }
                    }
                }
                //set stat levelups
                try
                {
                    if (nextLevel % 4 == 0)
                    {
                        classLevel.LevelsStat = levelUps[nextLevel];
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

        static void GetSelections(LevelUpPlan levelUpPlan, UnitEntityData unit, StringBuilder sb)
        {
            var selections = unit.Progression.Selections;
            var selectionSkips = new List<BlueprintFeatureSelection>() {
                Common.MyUtils.GetBlueprint<BlueprintFeatureSelection>("9ee0f6745f555484299b0a1563b99d81"),
                Common.MyUtils.GetBlueprint<BlueprintFeatureSelection>("ba0e5a900b775be4a99702f1ed08914d"),
                Common.MyUtils.GetBlueprint<BlueprintFeatureSelection>("1421e0034a3afac458de08648d06faf0")
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

        static void GetStats(UnitEntityData unit, out Dictionary<StatType, int> attributes, out IDictionary<int, StatType> levelUps)
        {
            attributes = unit.Stats.Attributes.ToDictionary(a => a.Type, a => a.BaseValue);
            levelUps = new Dictionary<int, StatType>();
            var statLevelUps = unit.Progression.CharacterLevel / 4;
            if (statLevelUps > 0)
            {
                var characterRecord = GlobalRecord.Instance.ForCharacter(unit);
                var curStatLevel = statLevelUps * 4;
                var recordPresent = true;

                //recorded levelups
                while (curStatLevel >= 4 && recordPresent)
                {
                    characterRecord.LevelUpActions.TryGetValue(curStatLevel, out var actions);
                    if (actions == null)
                    {
                        recordPresent = false;
                        break;
                    }
                    var action = actions.OfType<SpendAttributePointAction>().FirstOrDefault();
                    if (action == null)
                    {
                        recordPresent = false;
                        break;
                    }
                    levelUps[curStatLevel] = action.Attribute;
                    attributes[action.Attribute] = attributes[action.Attribute] - 1;
                    curStatLevel -= 4;
                }

                //non-recordded level ups
                while (curStatLevel >= 4)
                {
                    var highest = attributes.MaxBy(a => a.Value);
                    attributes[highest.Key] = attributes[highest.Key] - 1;
                    levelUps[curStatLevel] = highest.Key;

                    curStatLevel -= 4;
                }
            }
        }

        static void GetStatsForCompanion(UnitEntityData unit, out IDictionary<int, StatType> levelUps)
        {
            var attributes = unit.Stats.Attributes.ToDictionary(a => a.Type, a => a.BaseValue);
            levelUps = new Dictionary<int, StatType>();
            var statLevelUps = unit.Progression.CharacterLevel / 4;

            if (statLevelUps > 0)
            {
                var characterRecord = GlobalRecord.Instance.ForCharacter(unit);
                var curStatLevel = statLevelUps * 4;
                var recordPresent = true;

                //recorded levelups
                while (curStatLevel >= 4 && recordPresent)
                {
                    characterRecord.LevelUpActions.TryGetValue(curStatLevel, out var actions);
                    if (actions == null)
                    {
                        recordPresent = false;
                        break;
                    }
                    var action = actions.OfType<SpendAttributePointAction>().FirstOrDefault();
                    if (action == null)
                    {
                        recordPresent = false;
                        break;
                    }
                    levelUps[curStatLevel] = action.Attribute;
                    attributes[action.Attribute] = attributes[action.Attribute] - 1;
                    curStatLevel -= 4;
                }

                //non-recordded level ups
                while (curStatLevel >= 4)
                {
                    if (unit.Stats.Strength > unit.Blueprint.Strength)
                    {
                        levelUps[curStatLevel] = StatType.Strength;
                    }
                    else if (unit.Stats.Dexterity > unit.Blueprint.Dexterity)
                    {
                        levelUps[curStatLevel] = StatType.Dexterity;
                    }
                    else if (unit.Stats.Constitution > unit.Blueprint.Constitution)
                    {
                        levelUps[curStatLevel] = StatType.Constitution;
                    }
                    else if (unit.Stats.Intelligence > unit.Blueprint.Intelligence)
                    {
                        levelUps[curStatLevel] = StatType.Intelligence;
                    }
                    else if (unit.Stats.Wisdom > unit.Blueprint.Wisdom)
                    {
                        levelUps[curStatLevel] = StatType.Wisdom;
                    }
                    else if (unit.Stats.Charisma > unit.Blueprint.Charisma)
                    {
                        levelUps[curStatLevel] = StatType.Charisma;
                    }
                    else
                    {
                        //if none of stats is higher than in blueprint, let's just say we're increasing strength
                        levelUps[curStatLevel] = StatType.Strength;
                    }
                    curStatLevel -= 4;
                }
            }
        }
    }
}
