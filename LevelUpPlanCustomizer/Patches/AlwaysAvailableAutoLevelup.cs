using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Class.LevelUp;


namespace LevelUpPlanCustomizer.Patches
{
    class AlwaysAvailableAutoLevelup
    {

        [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.IsPlanEnabled))]
        static class LevelUpController_IsPlanEnabled_Patch
        {
            static bool Prefix(ref bool __result, UnitEntityData unit, bool ignoreSettings = false)
            {
                __result = true;
                return false;
            }
        }
    }
}
