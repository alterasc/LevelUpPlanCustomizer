using HarmonyLib;
using Kingmaker;
using LevelUpPlanCustomizer.Base.Import;
using LevelUpPlanCustomizer.Export;
using LevelUpPlanCustomizer.Patches;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
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
            Settings = Settings.Load<Settings>(modEntry);
            var harmony = new Harmony(modEntry.Info.Id);
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
            GUILayout.BeginHorizontal();
            GUILayout.Label("TextArea", GUILayout.ExpandWidth(false));
            GUILayout.Space(10);
            Settings.MyTextOption = GUILayout.TextArea(Settings.MyTextOption, GUILayout.Width(300f));
            GUILayout.EndHorizontal();

            var player = Game.Instance.Player;
            var activeCompanions = player.ActiveCompanions;

            var mc = player.MainCharacter.Value;
            GUILayout.BeginHorizontal();
            GUILayout.Label(mc.CharacterName, GUILayout.ExpandWidth(false));
            GUILayout.Space(40);
            GUILayout.Label("Export in place of", GUILayout.ExpandWidth(false));
            if (GUILayout.RepeatButton("Taolynn the Cavalier", GUILayout.ExpandWidth(false)))
            {
                ExportMC("57c2aaeb11ee4f8d81f0a57974a94f1b");
            }
            if (GUILayout.RepeatButton("Sordara the Cleric", GUILayout.ExpandWidth(false)))
            {
                ExportMC("d27dd725873142039c6015fbf49ac621");
            }
            if (GUILayout.RepeatButton("Yunelard the Fighter", GUILayout.ExpandWidth(false)))
            {
                ExportMC("8abbe46e26844e02a39645ae34913612");
            }
            if (GUILayout.RepeatButton("Rix the Rogue", GUILayout.ExpandWidth(false)))
            {
                ExportMC("fad59e6db3aa470ca7e8962e2daa12dc");
            }
            if (GUILayout.RepeatButton("Marnun the Slayer", GUILayout.ExpandWidth(false)))
            {
                ExportMC("2160635e9bba4b9e81a5cfcd45e3d141");
            }
            if (GUILayout.RepeatButton("Aengi the Sorcerer", GUILayout.ExpandWidth(false)))
            {
                ExportMC("1f6d72fd52ce418fb677db2243ea4de5");
            }
            GUILayout.EndHorizontal();

            foreach (var comp in activeCompanions)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(comp.CharacterName, GUILayout.ExpandWidth(false));
                GUILayout.Space(10);
                if (GUILayout.RepeatButton("Export", GUILayout.ExpandWidth(false)))
                {
                    Settings.MyTextOption = comp.Blueprint.AssetGuid.ToString();
                }
                GUILayout.EndHorizontal();
            }

        }

        private static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        private static void ExportMC(string unitId)
        {
            try
            {
                var pregen = CharacterExporter.ExportMC(unitId, out var log);
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
                var userPath = $"{Main.ModEntry.Path}Pregens";
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
                Settings.MyTextOption = log + "\n\n" + stringWriter.ToString();
                CharacterImporter.UpdatePregens();
            }
            catch (Exception ex)
            {
                Settings.MyTextOption = ex.ToString();
            }
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }
    }

    public class Settings : UnityModManager.ModSettings
    {
        public float MyFloatOption = 2f;
        public bool MyBoolOption = true;
        public string MyTextOption = "Hello";

        public override void Save(UnityModManager.ModEntry modEntry)
        {

        }
    }
}
