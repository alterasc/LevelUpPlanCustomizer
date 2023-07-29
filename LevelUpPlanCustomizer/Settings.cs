using UnityModManagerNet;

namespace LevelUpPlanCustomizer
{
    public class Settings : UnityModManager.ModSettings
    {
        public bool AlwaysAutoLevel = true;
        public bool PatchApplyLevelUpActions = true;
        public bool PatchSelectFeature = true;

        public virtual void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
    }
}
