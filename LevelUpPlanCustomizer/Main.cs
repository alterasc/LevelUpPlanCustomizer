using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using LevelUpPlanCustomizer.Base;
using LevelUpPlanCustomizer.Base.Import;
using LevelUpPlanCustomizer.Export;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityModManagerNet;

namespace LevelUpPlanCustomizer
{
#if DEBUG
    [EnableReloading]
#endif
    static class Main
    {
        public static bool Enabled;
        public static Settings Settings;
        public static UnityModManager.ModEntry ModEntry;
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            ModEntry = modEntry;
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
#if DEBUG
            modEntry.OnUnload = OnUnload;
#endif
            harmony.PatchAll();
            return true;
        }
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            return true;
        }

        static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            return true;
        }


        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Label("Always available auto level");
            Settings.AlwaysAutoLevel = GUILayout.Toggle(Settings.AlwaysAutoLevel, "Always allow auto level");
            GUILayout.Label("Archetype skill/spell fix");
            Settings.PatchApplyLevelUpActions = GUILayout.Toggle(Settings.PatchApplyLevelUpActions, "Experimental patch to fix issues with archetype skills and spellbooks");
            GUILayout.Label("Archetype feature selection fix");
            Settings.PatchSelectFeature = GUILayout.Toggle(Settings.PatchSelectFeature, "Experimental patch to fix issues with archetype features");

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.EndHorizontal();

            var player = Game.Instance?.Player;
            if (player == null)
            {
                return;
            }
            var mc = player?.MainCharacter.Value;
            if (mc == null)
            {
                return;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("-----------------------------------------------------");
            GUILayout.EndHorizontal();
            GUIExportAsPregen(mc);
            var campaign = Game.Instance.Player.Campaign;
            var MainCampaign = Utils.GetBlueprint<BlueprintCampaign>("fd2e11ebb8a14d6599450fc27f03486a");
            var Dlc3Campaign = Utils.GetBlueprint<BlueprintCampaign>("e1bde745d6ad47c0bc9fb8e479b29153");
            var activeCompanions = player.ActiveCompanions;
            if (activeCompanions == null)
            {
                return;
            }
            if (campaign == MainCampaign)
            {
                foreach (var comp in activeCompanions.Where(x => x.IsStoryCompanion()))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(comp.CharacterName, GUILayout.Width(140));
                    comp.IsStoryCompanion();
                    GUILayout.Space(10);
                    if (GUILayout.RepeatButton("Export", GUILayout.ExpandWidth(false)))
                    {
                        ExportUnitAsFeatureList(comp);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else if (campaign == Dlc3Campaign)
            {
                foreach (var comp in activeCompanions)
                {
                    GUIExportAsPregen(comp);
                }
            }
        }

        private static void GUIExportAsPregen(UnitEntityData mc)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(mc.CharacterName, GUILayout.Width(140));
            GUILayout.Space(10);
            GUILayout.Label("Export in place of", GUILayout.ExpandWidth(false));
            if (GUILayout.RepeatButton("Taolynn the Cavalier", GUILayout.ExpandWidth(false)))
            {
                ExportUnitAsPregen(mc, "57c2aaeb11ee4f8d81f0a57974a94f1b");
            }
            if (GUILayout.RepeatButton("Sordara the Cleric", GUILayout.ExpandWidth(false)))
            {
                ExportUnitAsPregen(mc, "d27dd725873142039c6015fbf49ac621");
            }
            if (GUILayout.RepeatButton("Yunelard the Fighter", GUILayout.ExpandWidth(false)))
            {
                ExportUnitAsPregen(mc, "8abbe46e26844e02a39645ae34913612");
            }
            if (GUILayout.RepeatButton("Rix the Rogue", GUILayout.ExpandWidth(false)))
            {
                ExportUnitAsPregen(mc, "fad59e6db3aa470ca7e8962e2daa12dc");
            }
            if (GUILayout.RepeatButton("Marnun the Slayer", GUILayout.ExpandWidth(false)))
            {
                ExportUnitAsPregen(mc, "2160635e9bba4b9e81a5cfcd45e3d141");
            }
            if (GUILayout.RepeatButton("Aengi the Sorcerer", GUILayout.ExpandWidth(false)))
            {
                ExportUnitAsPregen(mc, "1f6d72fd52ce418fb677db2243ea4de5");
            }
            GUILayout.EndHorizontal();
        }

        private static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        private static void ExportUnitAsPregen(UnitEntityData unit, string unitId)
        {
            try
            {
                var pregen = CharacterExporter.ExportAsPregen(unit, unitId, out var log);
                var jsonSerializer = new JsonSerializer();
                jsonSerializer.Formatting = Formatting.Indented;
                jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                jsonSerializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                StringWriter stringWriter = new(new StringBuilder(256), CultureInfo.InvariantCulture);
                using (JsonTextWriter jsonTextWriter = new(stringWriter))
                {
                    jsonTextWriter.Formatting = jsonSerializer.Formatting;
                    jsonSerializer.Serialize(jsonTextWriter, pregen);
                }
                var userPath = $"{ModEntry.Path}Pregens";
                var info = Directory.CreateDirectory(userPath);
                var exportFileName = "export.json";
                if (pregen.PregenUnitComponent.PregenName.Length > 0)
                {
                    var sanizedFileName = MakeValidFileName(pregen.PregenUnitComponent.PregenName);
                    if (sanizedFileName.Length > 0)
                    {
                        exportFileName = $"{sanizedFileName}.json";
                    }
                }
                File.WriteAllText(Path.Combine(userPath, exportFileName), stringWriter.ToString());
                CharacterImporter.UpdatePregens(exportFileName);
            }
            catch (Exception ex)
            {
                LogChannel logChannel = LogChannelFactory.GetOrCreate("Mods");
                logChannel.Error("Error during pregen export: {}", ex.Message);
            }
        }

        private static void ExportUnitAsFeatureList(UnitEntityData unit)
        {
            try
            {
                var featureList = CharacterExporter.ExportAsFeatureList(unit, out var log);
                var jsonSerializer = new JsonSerializer();
                jsonSerializer.Formatting = Formatting.Indented;
                jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                jsonSerializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                StringWriter stringWriter = new(new StringBuilder(256), CultureInfo.InvariantCulture);
                using (JsonTextWriter jsonTextWriter = new(stringWriter))
                {
                    jsonTextWriter.Formatting = jsonSerializer.Formatting;
                    jsonSerializer.Serialize(jsonTextWriter, featureList);
                }
                var userPath = $"{ModEntry.Path}FeatureLists";
                var info = Directory.CreateDirectory(userPath);
                var exportFileName = "export.json";
                if (unit.CharacterName.Length > 0)
                {
                    var sanizedFileName = MakeValidFileName(unit.CharacterName);
                    if (sanizedFileName.Length > 0)
                    {
                        exportFileName = $"{sanizedFileName}.json";
                    }
                }
                File.WriteAllText(Path.Combine(userPath, exportFileName), stringWriter.ToString());
                CharacterImporter.UpdateFeatureLists(exportFileName);
            }
            catch (Exception ex)
            {
                LogChannel logChannel = LogChannelFactory.GetOrCreate("Mods");
                logChannel.Error("Error during feature list export: {}", ex.Message);
            }
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }
    }
}
