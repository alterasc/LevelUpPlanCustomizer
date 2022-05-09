using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Settings;

namespace LevelUpPlanCustomizer.Base.Schemas.v1
{
    public class LevelUpPlan
    {
        public string FeatureList { get; set; }
        public ClassLevel[] Classes { get; set; }
        public ClassLevel[] MythicClasses { get; set; }

        public AddFact[] AddFacts { get; set; }
    }

    public class ClassLevel
    {
        public string m_CharacterClass { get; set; }
        public string[] m_Archetypes { get; set; } = new string[0];
        public int Levels { get; set; }
        public StatType RaceStat { get; set; }
        public StatType LevelsStat { get; set; }
        public StatType[] Skills { get; set; } = new StatType[0];
        public string[] m_SelectSpells { get; set; } = new string[0];
        public string[] m_MemorizeSpells { get; set; } = new string[0];
        public SelectionClass[] Selections { get; set; }

    }

    public class SelectionClass
    {
        public bool IsParametrizedFeature { get; set; } = false;
        public bool IsFeatureSelectMythicSpellbook { get; set; } = false;
        public string m_Selection { get; set; }
        public string[] m_Features { get; set; } = new string[0];
        public string m_ParametrizedFeature { get; set; }
        public SpellSchool ParamSpellSchool { get; set; } = SpellSchool.None;
        public WeaponCategory ParamWeaponCategory { get; set; } = WeaponCategory.UnarmedStrike;

        public StatType Stat { get; set; }
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
