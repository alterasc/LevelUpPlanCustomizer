using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Settings;
using Newtonsoft.Json;

namespace LevelUpPlanCustomizer.Schemas.v1
{
    public class LevelUpPlan
    {
        [JsonProperty("$schema")]
        public string Schema { get; set; } = "";
        public string FeatureList { get; set; }
        public ClassLevel[] Classes { get; set; }
        public AddFact[] AddFacts { get; set; } = new AddFact[0];
    }

    public class ClassLevel
    {
        public string m_CharacterClass { get; set; }
        public string[] m_Archetypes { get; set; } = new string[0];
        public int Levels { get; set; }
        public StatType? RaceStat { get; set; }
        public StatType? LevelsStat { get; set; }
        public StatType[] Skills { get; set; } = new StatType[0];
        public string[] m_SelectSpells { get; set; } = new string[0];
        public string[] m_MemorizeSpells { get; set; } = new string[0];
        public SelectionClass[] Selections { get; set; }

    }

    public class SelectionClass
    {
        public bool? IsParametrizedFeature { get; set; }
        public bool? IsFeatureSelectMythicSpellbook { get; set; }
        public string m_Selection { get; set; }
        public string[] m_Features { get; set; } = new string[0];
        public string m_ParametrizedFeature { get; set; }
        public SpellSchool? ParamSpellSchool { get; set; }
        public WeaponCategory? ParamWeaponCategory { get; set; }

        public StatType? Stat { get; set; }
        public string m_FeatureSelectMythicSpellbook { get; set; }
        public string m_Spellbook { get; set; }
    }

    public class AddFact
    {
        public string[] m_Facts { get; set; }
        public int CasterLevel { get; set; } = 0;
        public GameDifficultyOption MinDifficulty { get; set; } = GameDifficultyOption.Story;
    }
}
