using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Utility;
using LevelUpPlanCustomizer.Schemas.v1;
using System.Collections.Generic;
using System.Linq;

namespace LevelUpPlanCustomizer.Export
{
    internal class CharacterExporter
    {
        public static PregenUnit exportMC()
        {
            var player = Game.Instance.Player;
            Kingmaker.EntitySystem.Entities.UnitEntityData mc = player.MainCharacter.Value;
            var stats = mc.Stats;

            var pregen = new PregenUnit();

            pregen.Gender = mc.Gender;
            pregen.m_Race = mc.Progression.m_Race.AssetGuid.ToString();
            pregen.Alignment = mc.Alignment.ValueVisible;

            var lvl = mc.Progression.CharacterLevel;
            var classOrder = mc.Progression.ClassesOrder;

            getStats(mc, out var attributes, out var levelUps);

            pregen.Strength = attributes[StatType.Strength];
            pregen.Dexterity = attributes[StatType.Dexterity];
            pregen.Constitution = attributes[StatType.Constitution];
            pregen.Intelligence = attributes[StatType.Intelligence];
            pregen.Wisdom = attributes[StatType.Wisdom];
            pregen.Charisma = attributes[StatType.Charisma];

            var selections = mc.Progression.Selections;

            foreach (var selection in selections)
            {
                var key = selection.Key;
                Kingmaker.UnitLogic.FeatureSelectionData value = selection.Value;
                if (value.Source.Blueprint is BlueprintProgression obj)
                {

                }
            }

            return pregen;
        }

        static void getStats(Kingmaker.EntitySystem.Entities.UnitEntityData mc, out Dictionary<StatType, int> attributes, out IDictionary<StatType, int> levelUps)
        {
            attributes = mc.Stats.Attributes.Select(a => (a.Type, a.BaseValue)).ToDictionary(a => a.Type, a => a.BaseValue);
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

                foreach (var attr in attributes)
                {
                    var baseValue = attr.Value;
                    if (baseValue > 18)
                    {
                        levelUps.TryGetValue(attr.Key, out var ups);
                        levelUps[attr.Key] = ups + (baseValue - 18);
                        statLevelUps -= (baseValue - 18);
                        attributes[attr.Key] = 18;
                    }
                    statPB += pointBuy[attributes[attr.Key]];
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
