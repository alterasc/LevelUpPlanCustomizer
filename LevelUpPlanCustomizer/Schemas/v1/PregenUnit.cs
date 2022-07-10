using Kingmaker.Blueprints;
using Kingmaker.Enums;

namespace LevelUpPlanCustomizer.Schemas.v1
{
    public class PregenUnit
    {
        public string UnitId { get; set; }
        public PregenUnitComp PregenUnitComponent { get; set; }

        public Gender Gender { get; set; }
        public string m_Race { get; set; }
        public Alignment Alignment { get; set; }

        public int Strength { get; set; } = 10;
        public int Dexterity { get; set; } = 10;
        public int Constitution { get; set; } = 10;
        public int Intelligence { get; set; } = 10;
        public int Wisdom { get; set; } = 10;
        public int Charisma { get; set; } = 10;
        public LevelUpPlan LevelUpPlan { get; set; }

    }

    public class PregenUnitComp
    {
        public string PregenName { get; set; }
        public string PregenDescription { get; set; }
        public string PregenClass { get; set; }
        public string PregenRole { get; set; }
    }
}
