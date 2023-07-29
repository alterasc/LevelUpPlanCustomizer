using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Stats;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace LevelUpPlanCustomizer.Patches
{
    [HarmonyPatch]
    static class SaveHooker
    {

        [HarmonyPatch(typeof(ZipSaver))]
        [HarmonyPatch(nameof(ZipSaver.SaveJson)), HarmonyPostfix]
        static void Zip_Saver(string name, ZipSaver __instance)
        {
            DoSave(name, __instance);
        }

        [HarmonyPatch(typeof(FolderSaver))]
        [HarmonyPatch(nameof(FolderSaver.SaveJson)), HarmonyPostfix]
        static void Folder_Saver(string name, FolderSaver __instance)
        {
            DoSave(name, __instance);
        }

        static void DoSave(string name, ISaver saver)
        {
            if (name != "header")
                return;
            try
            {
                var serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                var writer = new StringWriter();
                serializer.Serialize(writer, GlobalRecord.Instance);
                writer.Flush();
                saver.SaveJson(LoadHooker.FileName, writer.ToString());
            }
            catch (Exception ex)
            {
                LogChannel logChannel = LogChannelFactory.GetOrCreate("Mods");
                logChannel.Error($"Error when saving level up data: {ex}");
            }
        }
    }


    static class LoadHooker
    {
        public const string FileName = "header.json.alterasc_levelupplancustomizer";

        [HarmonyPatch(typeof(Game), nameof(Game.LoadGame)), HarmonyPostfix]
        static void LoadGame(SaveInfo saveInfo)
        {
            using (saveInfo)
            {
                using (saveInfo.GetReadScope())
                {
                    ThreadedGameLoader.RunSafelyInEditor(() =>
                    {
                        string raw;
                        using (ISaver saver = saveInfo.Saver.Clone())
                        {
                            raw = saver.ReadJson(FileName);
                        }
                        if (raw != null)
                        {
                            var serializer = new JsonSerializer();
                            var rawReader = new StringReader(raw);
                            var jsonReader = new JsonTextReader(rawReader);
                            GlobalRecord.Instance = serializer.Deserialize<GlobalRecord>(jsonReader);
                        }
                        else
                        {
                            GlobalRecord.Instance = new GlobalRecord();
                        }
                    }).Wait();
                }
            }
        }
    }

    public class CharacterRecord
    {
        public Dictionary<int, List<ILevelupAction>> LevelUpActions = new();

        public void ResetAtLevel(int level)
        {
            LevelUpActions[level] = new();
        }
        public void AddAtLevel(int level, ILevelupAction levelupAction)
        {
            if (!LevelUpActions.TryGetValue(level, out var list))
            {
                list = new();
                LevelUpActions[level] = list;
            }
            list.Add(levelupAction);
        }
    }

    public interface ILevelupAction { }

    public class SpendSkillPointAction : ILevelupAction
    {
        public StatType Skill;
    }

    public class SelectSpellAction : ILevelupAction
    {
        public string Spell;
        public string Spellbook;
    }
    public class SpendAttributePointAction : ILevelupAction
    {
        public StatType Attribute;
    }

    public class GlobalRecord
    {

        public Dictionary<string, CharacterRecord> PerCharacter = new();

        public CharacterRecord ForCharacter(UnitEntityData unit)
        {
            var key = unit.UniqueId;
            if (!PerCharacter.TryGetValue(key, out var record))
            {
                record = new();
                PerCharacter[key] = record;
            }
            return record;
        }

        public int version = 1;

        public static GlobalRecord Instance = new();
    }
}
