using HarmonyLib;
using Kingmaker;
using LevelUpPlanCustomizer.Export;
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
            GUILayout.Space(10);
            if (GUILayout.RepeatButton("Export", GUILayout.ExpandWidth(false)))
            {
                var pregen = CharacterExporter.exportMC();
                try
                {
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
                    Settings.MyTextOption = stringWriter.ToString();
                }
                catch (Exception ex)
                {
                    Settings.MyTextOption = ex.ToString();
                }
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
