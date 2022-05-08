using HarmonyLib;
using UnityModManagerNet;

namespace LevelUpPlanCustomizer
{
    static class Main
    {
        public static bool Enabled;
        public static UnityModManager.ModEntry ModEntry;
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            ModEntry = modEntry;
            harmony.PatchAll();
            return true;
        }
    }
}
