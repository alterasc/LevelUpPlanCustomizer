using Kingmaker.Blueprints;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Components;
using Kingmaker.UnitLogic.FactLogic;
using LevelUpPlanCustomizer.Common;
using LevelUpPlanCustomizer.Schemas.v1;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LevelUpPlanCustomizer.Import
{
    internal class CharacterImporter
    {
        static Dictionary<string, string> PregensChanged = new();
        static Dictionary<string, string> NPCChanged = new();

        internal static void UpdatePregens(string filePattern = null)
        {
            LogChannel logChannel = LogChannelFactory.GetOrCreate("Mods");

            var userPath = $"{Main.ModEntry.Path}Pregens";
            var info = Directory.CreateDirectory(userPath);
            string pattern = "*.json";
            if (filePattern != null)
            {
                pattern = filePattern;
            }
            foreach (var file in info.GetFiles(pattern, SearchOption.AllDirectories))
            {
                PregenUnit pregenUnit = null;
                using (var reader = file.OpenText())
                    try
                    {
                        var jsonSerializer = new JsonSerializer();
                        pregenUnit = jsonSerializer.Deserialize<PregenUnit>(new JsonTextReader(reader));
                    }
                    catch (Exception ex)
                    {
                        logChannel.Error($"Unable to parse {file}: {ex}");
                    }
                if (pregenUnit != null)
                {
                    try
                    {
                        ApplyPregen(pregenUnit);
                        Characters.PregenNameById.TryGetValue(Guid.Parse(pregenUnit.UnitId).ToString("N"), out var name);
                        if (name != null)
                        {
                            PregensChanged[name] = file.Name;
                            logChannel.Log($"Successfully updated pregen {name} from {file}");
                        }
                        else
                        {
                            logChannel.Log($"Successfully updated pregen with id: {pregenUnit.UnitId} from {file}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logChannel.Error($"Error when applying {file}: {ex}");
                    }
                }
            }
        }

        internal static void UpdateFeatureLists(string filePattern = null)
        {
            LogChannel logChannel = LogChannelFactory.GetOrCreate("Mods");

            var userPath = $"{Main.ModEntry.Path}FeatureLists";
            var info = Directory.CreateDirectory(userPath);
            string pattern = "*.json";
            if (filePattern != null)
            {
                pattern = filePattern;
            }
            foreach (var file in info.GetFiles(pattern, SearchOption.AllDirectories))
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
                        Characters.PregenNameById.TryGetValue(Guid.Parse(levelUpPlan.FeatureList).ToString("N"), out var companionName);
                        if (companionName != null)
                        {
                            NPCChanged[companionName] = file.Name;
                            logChannel.Log($"Successfully updated build of {companionName} from {file}");
                        }
                        else
                        {
                            logChannel.Log($"Successfully updated feature list with id: {levelUpPlan.FeatureList} from {file}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logChannel.Error($"Error when applying {file}: {ex}");
                    }
                }
            }
        }

        private static string ApplyPregen(PregenUnit pregenUnit)
        {
            var pregenBP = MyUtils.GetBlueprint<BlueprintUnit>(pregenUnit.UnitId);
            var race = MyUtils.GetBlueprint<BlueprintRace>(pregenUnit.m_Race);
            pregenBP.m_Race = race.ToReference<BlueprintRaceReference>();
            pregenBP.Alignment = pregenUnit.Alignment;
            pregenBP.Gender = pregenUnit.Gender;
            pregenBP.Strength = pregenUnit.Strength;
            pregenBP.Dexterity = pregenUnit.Dexterity;
            pregenBP.Constitution = pregenUnit.Constitution;
            pregenBP.Intelligence = pregenUnit.Intelligence;
            pregenBP.Wisdom = pregenUnit.Wisdom;
            pregenBP.Charisma = pregenUnit.Charisma;
            var pregenUnitComponent = pregenBP.GetComponent<PregenUnitComponent>();
            var originalName = pregenUnitComponent.PregenName.ToString();
            var pgUC = pregenUnit.PregenUnitComponent;
            if (pgUC != null)
            {
                if (pgUC.PregenName != null && pgUC.PregenName.Length > 0)
                {
                    pregenUnitComponent.PregenName = MyUtils.CreateLocalizedString($"{pregenUnit.UnitId}.PregenName", pgUC.PregenName);
                }
                if (pgUC.PregenDescription != null && pgUC.PregenDescription.Length > 0)
                {
                    pregenUnitComponent.PregenDescription = MyUtils.CreateLocalizedString($"{pregenUnit.UnitId}.PregenDescription", pgUC.PregenDescription);
                }
            }
            pregenBP.RemoveComponents<PregenDollSettings>();
            var doll = new PregenDollSettings()
            {
                Default = new()
                {
                    m_RacePreset = race.m_Presets.First()
                }
            };
            pregenBP.AddComponent(doll);
            var bpref = pregenBP.m_AddFacts[0];
            pregenUnit.LevelUpPlan.FeatureList = bpref.Guid.ToString();
            ApplyPlan(pregenUnit.LevelUpPlan);
            return originalName;

        }
        private static void ApplyPlan(LevelUpPlan levelUpPlan)
        {
            var featureList = MyUtils.GetBlueprint<BlueprintFeature>(levelUpPlan.FeatureList);
            List<AddClassLevels> addClassLevels = new List<AddClassLevels>();
            foreach (var cl in levelUpPlan.Classes)
            {
                addClassLevels.Add(CreateAddClassLevels(cl));
            }
            var addFacts = levelUpPlan.AddFacts.Select(cl =>
                new AddFacts()
                {
                    m_Facts = cl.m_Facts.Select(c => MyUtils.GetBlueprintReference<BlueprintUnitFactReference>(c)).ToArray(),
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
                m_CharacterClass = MyUtils.GetBlueprintReference<BlueprintCharacterClassReference>(cl.m_CharacterClass),
                m_Archetypes = cl.m_Archetypes != null ? cl.m_Archetypes.Select(c => MyUtils.GetBlueprintReference<BlueprintArchetypeReference>(c)).ToArray() : new BlueprintArchetypeReference[0],
                Levels = cl.Levels,
                RaceStat = cl.RaceStat ?? Kingmaker.EntitySystem.Stats.StatType.Strength,
                LevelsStat = cl.LevelsStat ?? Kingmaker.EntitySystem.Stats.StatType.Strength,
                Skills = cl.Skills,
                m_SelectSpells = cl.m_SelectSpells != null ? cl.m_SelectSpells.Select(c => MyUtils.GetBlueprintReference<BlueprintAbilityReference>(c)).ToArray() : new BlueprintAbilityReference[0],
                m_MemorizeSpells = cl.m_MemorizeSpells != null ? cl.m_MemorizeSpells.Select(c => MyUtils.GetBlueprintReference<BlueprintAbilityReference>(c)).ToArray() : new BlueprintAbilityReference[0]
            };
            AddSelections(cl, r);
            return r;
        }

        private static void AddSelections(ClassLevel cl, AddClassLevels r)
        {
            LogChannel logChannel = LogChannelFactory.GetOrCreate("Mods");
            if (cl.Selections == null)
            {
                r.Selections = new SelectionEntry[0];
                logChannel.Log($"NO SELECTION AT {r.m_CharacterClass}");
            }
            else
            {
                r.Selections = cl.Selections.Select(sel =>
                {
                    if (sel.m_Features == null || sel.m_Features.Length == 0)
                    {
                        throw new ArgumentException($"Selection {sel.m_Selection} has no feature choice");
                    }
                    logChannel.Log($"ADDING SELECTION AT {r.m_CharacterClass}: SELECTION: {sel.m_Selection}, FEATURE {sel.m_Features.First()}");
                    return new SelectionEntry()
                    {
                        IsParametrizedFeature = sel.IsParametrizedFeature ?? false,
                        IsFeatureSelectMythicSpellbook = sel.IsFeatureSelectMythicSpellbook ?? false,
                        m_Selection = MyUtils.GetBlueprintReference<BlueprintFeatureSelectionReference>(sel.m_Selection),
                        m_Features = sel.m_Features.Select(c => MyUtils.GetBlueprintReference<BlueprintFeatureReference>(c)).ToArray(),
                        m_ParametrizedFeature = MyUtils.GetBlueprintReference<BlueprintParametrizedFeatureReference>(sel.m_ParametrizedFeature),
                        ParamSpellSchool = sel.ParamSpellSchool ?? Kingmaker.Blueprints.Classes.Spells.SpellSchool.None,
                        ParamWeaponCategory = sel.ParamWeaponCategory ?? Kingmaker.Enums.WeaponCategory.UnarmedStrike,
                        Stat = sel.Stat ?? Kingmaker.EntitySystem.Stats.StatType.Unknown,
                        m_FeatureSelectMythicSpellbook = MyUtils.GetBlueprintReference<BlueprintFeatureSelectMythicSpellbookReference>(sel.m_FeatureSelectMythicSpellbook),
                        m_Spellbook = MyUtils.GetBlueprintReference<BlueprintSpellbookReference>(sel.m_Spellbook)
                    };
                }).ToArray();
            }
        }
    }
}
