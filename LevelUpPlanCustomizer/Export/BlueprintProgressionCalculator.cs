using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LevelUpPlanCustomizer.Base.Export
{
    internal class ClassArchetypeData
    {
        public BlueprintCharacterClass PFClass;
        public BlueprintArchetype[] Archetypes;
        public int level;
    }

    internal class BlueprintProgressionCalculator
    {
        public static int CalcLevel(BlueprintProgression progression, IDictionary<BlueprintCharacterClass, ClassArchetypeData> classes)
        {
            if (progression.m_Classes.Empty() && progression.m_Archetypes.Empty())
                return classes.Values.Select(x => x.level).Sum();
            int num1 = 0;
            
            foreach (BlueprintProgression.ClassWithLevel classWithLevel in progression.m_Classes)
            {
                if (!classWithLevel.Class.Archetypes.Any(a => progression.m_Archetypes.Contains(i => i.Archetype == a)))
                {
                    int unitClassLevel = classes.Get(classWithLevel.Class)?.level ?? 0;
                    num1 += Math.Max(0, unitClassLevel + classWithLevel.AdditionalLevel);
                }
            }

            foreach (BlueprintProgression.ArchetypeWithLevel archetype in progression.m_Archetypes)
            {                
                foreach (var classData in classes.Values)
                {
                    if (classData.Archetypes.HasItem(archetype.Archetype))
                        num1 += Math.Max(0, classData.level + archetype.AdditionalLevel);
                }
            }

            return num1;
        }

        public static void FindCharLevel()
        {
            IEnumerable<int> list = new List<int>();            
        }
    }
}
