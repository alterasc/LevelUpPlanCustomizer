using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Components;
using Kingmaker.UnitLogic.FactLogic;
using LevelUpPlanCustomizer.Base.Schemas;
using LevelUpPlanCustomizer.Base.Schemas.v1;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LevelUpPlanCustomizer.Base.Patches
{
    class PlanUpdater
    {
        [HarmonyPatch(typeof(BlueprintsCache), "Init")]
        static class BlueprintsCache_Init_Patch
        {
            static bool Initialized;

            static void Postfix()
            {
                if (Initialized) return;
                Initialized = true;

                UpdateLevelUpPlans();
                UpdatePregens();
            }

            private static void UpdatePregens()
            {
                LogChannel logChannel = LogChannelFactory.GetOrCreate("Mods");

                var userPath = $"{Main.ModEntry.Path}Pregens";
                var info = Directory.CreateDirectory(userPath);
                foreach (var file in info.GetFiles("*.json", SearchOption.AllDirectories))
                {
                    PregenUnit levelUpPlan = null;
                    using (var reader = file.OpenText())
                        try
                        {
                            var jsonSerializer = new JsonSerializer();
                            levelUpPlan = jsonSerializer.Deserialize<PregenUnit>(new JsonTextReader(reader));
                        }
                        catch (Exception ex)
                        {
                            logChannel.Error($"Unable to parse {file}: {ex}");
                        }
                    if (levelUpPlan != null)
                    {
                        try
                        {
                            ApplyPregen(levelUpPlan);
                            logChannel.Log($"Successfully updated pregen with id: {levelUpPlan.UnitId}");
                        }
                        catch (Exception ex)
                        {
                            logChannel.Error($"Error when applying {file}: {ex}");
                        }
                    }
                }
            }

            private static void UpdateLevelUpPlans()
            {
                LogChannel logChannel = LogChannelFactory.GetOrCreate("Mods");

                var userPath = $"{Main.ModEntry.Path}FeatureLists";
                var info = Directory.CreateDirectory(userPath);
                foreach (var file in info.GetFiles("*.json", SearchOption.AllDirectories))
                {
                    LevelUpPlan levelUpPlan = null;
                    using (var reader = file.OpenText())
                        try
                        {
                            var jsonSerializer = new JsonSerializer();
                            levelUpPlan = jsonSerializer.Deserialize<LevelUpPlan>(new JsonTextReader(reader));
                        }
                        catch (Exception ex)
                        {
                            logChannel.Error($"Unable to parse {file}: {ex}");
                        }
                    if (levelUpPlan != null)
                    {
                        try
                        {
                            ApplyPlan(levelUpPlan);
                            logChannel.Log($"Successfully updated feature list with id: {levelUpPlan.FeatureList}");
                        }
                        catch (Exception ex)
                        {
                            logChannel.Error($"Error when applying {file}: {ex}");
                        }
                    }
                }
            }

            private static void ApplyPregen(PregenUnit pregenUnit)
            {
                var pregenBP = Utils.GetBlueprint<BlueprintUnit>(pregenUnit.UnitId);
                var race = Utils.GetBlueprint<BlueprintRace>(pregenUnit.m_Race);
                pregenBP.m_Race = race.ToReference<BlueprintRaceReference>();
                pregenBP.Alignment = pregenUnit.Alignment;
                pregenBP.Gender = pregenUnit.Gender;
                pregenBP.Strength = pregenUnit.Strength;
                pregenBP.Dexterity = pregenUnit.Dexterity;
                pregenBP.Constitution = pregenUnit.Constitution;
                pregenBP.Intelligence = pregenUnit.Intelligence;
                pregenBP.Wisdom = pregenUnit.Wisdom;
                pregenBP.Charisma = pregenUnit.Charisma;
                pregenBP.RemoveComponents<PregenDollSettings>();
                var doll = new PregenDollSettings()
                {
                    Default = new(){
                        m_RacePreset = race.m_Presets.First()
                    }
                };
                pregenBP.AddComponent(doll);
                var bpref = pregenBP.m_AddFacts[0];
                pregenUnit.LevelUpPlan.FeatureList = bpref.Guid.ToString();
                ApplyPlan(pregenUnit.LevelUpPlan);

            }
            private static void ApplyPlan(LevelUpPlan levelUpPlan)
            {
                var featureList = Utils.GetBlueprint<BlueprintFeature>(levelUpPlan.FeatureList);
                List<AddClassLevels> addClassLevels = new List<AddClassLevels>();
                foreach (var cl in levelUpPlan.Classes)
                {
                    addClassLevels.Add(CreateAddClassLevels(cl));
                }
                var addFacts = levelUpPlan.AddFacts.Select(cl =>
                    new AddFacts()
                    {
                        m_Facts = cl.m_Facts.Select(c => Utils.GetBlueprintReference<BlueprintUnitFactReference>(c)).ToArray(),
                        CasterLevel = cl.CasterLevel,
                        MinDifficulty = cl.MinDifficulty
                    }
                ).ToList();

                featureList.RemoveComponents<AddClassLevels>();
                featureList.RemoveComponents<AddFacts>();

                addClassLevels.ForEach(c => featureList.AddComponent(c));
                addFacts.ForEach(c => featureList.AddComponent(c));
            }

            private static AddClassLevels CreateAddClassLevels(ClassLevel cl)
            {
                var r = new AddClassLevels
                {
                    m_CharacterClass = Utils.GetBlueprintReference<BlueprintCharacterClassReference>(cl.m_CharacterClass),
                    m_Archetypes = cl.m_Archetypes.Select(c => Utils.GetBlueprintReference<BlueprintArchetypeReference>(c)).ToArray(),
                    Levels = cl.Levels,
                    RaceStat = cl.RaceStat,
                    LevelsStat = cl.LevelsStat,
                    Skills = cl.Skills,
                    m_SelectSpells = cl.m_SelectSpells.Select(c => Utils.GetBlueprintReference<BlueprintAbilityReference>(c)).ToArray(),
                    m_MemorizeSpells = cl.m_MemorizeSpells.Select(c => Utils.GetBlueprintReference<BlueprintAbilityReference>(c)).ToArray()
                };
                AddSelections(cl, r);
                return r;
            }

            private static void AddSelections(ClassLevel cl, AddClassLevels r)
            {
                r.Selections = cl.Selections.Select(sel =>
                {
                    return new SelectionEntry()
                    {
                        IsParametrizedFeature = sel.IsParametrizedFeature,
                        IsFeatureSelectMythicSpellbook = sel.IsFeatureSelectMythicSpellbook,
                        m_Selection = Utils.GetBlueprintReference<BlueprintFeatureSelectionReference>(sel.m_Selection),
                        m_Features = sel.m_Features.Select(c => Utils.GetBlueprintReference<BlueprintFeatureReference>(c)).ToArray(),
                        m_ParametrizedFeature = Utils.GetBlueprintReference<BlueprintParametrizedFeatureReference>(sel.m_ParametrizedFeature),
                        ParamSpellSchool = sel.ParamSpellSchool,
                        ParamWeaponCategory = sel.ParamWeaponCategory,
                        Stat = sel.Stat,
                        m_FeatureSelectMythicSpellbook = Utils.GetBlueprintReference<BlueprintFeatureSelectMythicSpellbookReference>(sel.m_FeatureSelectMythicSpellbook),
                        m_Spellbook = Utils.GetBlueprintReference<BlueprintSpellbookReference>(sel.m_Spellbook)
                    };
                }).ToArray();
            }
        }
    }
}
