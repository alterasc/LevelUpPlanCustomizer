using HarmonyLib;
using UnityModManagerNet;

namespace LevelUpPlanCustomizer
{
    static class Main
    {
        public static bool Enabled;
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll();
            return true;
        }
    }
}
