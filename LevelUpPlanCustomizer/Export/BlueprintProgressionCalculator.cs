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
        public int Level;
    }

    internal class BlueprintProgressionCalculator
    {
        public static int CalcLevel(BlueprintProgression progression, IDictionary<BlueprintCharacterClass, ClassArchetypeData> classes)
        {
            if (progression.m_Classes.Empty() && progression.m_Archetypes.Empty())
                return classes.Values.Select(x => x.Level).Sum();
            int num1 = 0;

            foreach (BlueprintProgression.ClassWithLevel classWithLevel in progression.m_Classes)
            {
                if (!classWithLevel.Class.Archetypes.Any(a => progression.m_Archetypes.Contains(i => i.Archetype == a)))
                {
                    int unitClassLevel = classes.Get(classWithLevel.Class)?.Level ?? 0;
                    num1 += Math.Max(0, unitClassLevel + classWithLevel.AdditionalLevel);
                }
            }

            foreach (BlueprintProgression.ArchetypeWithLevel archetype in progression.m_Archetypes)
            {
                foreach (var classData in classes.Values)
                {
                    if (classData.Archetypes.HasItem(archetype.Archetype))
                        num1 += Math.Max(0, classData.Level + archetype.AdditionalLevel);
                }
            }

            return num1;
        }

        public static int FindCharLevel(UnitDescriptor unit, BlueprintProgression featureProgression, int progressionLevel)
        {
            IDictionary<BlueprintCharacterClass, ClassArchetypeData> classes = new Dictionary<BlueprintCharacterClass, ClassArchetypeData>();
            var progression = unit.Progression;
            for (int i = 0; i < progression.m_ClassesOrder.Count; i++)
            {
                BlueprintCharacterClass classOrder = progression.m_ClassesOrder[i];
                classes.TryGetValue(classOrder, out var myClassData);
                if (myClassData == null)
                {
                    myClassData = new ClassArchetypeData();
                    var classData = progression.Classes.First(x => x.CharacterClass == classOrder);
                    myClassData.Archetypes = classData.Archetypes.ToArray();
                    myClassData.PFClass = classOrder;
                    myClassData.Level = 1;
                    classes.Add(classOrder, myClassData);
                }
                else
                {
                    myClassData.Level++;
                }
                var res = CalcLevel(featureProgression, classes);
                if (res == progressionLevel)
                {
                    return i + 1;
                }
                if (res > progressionLevel)
                {
                    return -1;
                }
            }
            return 1;
        }
    }
}
