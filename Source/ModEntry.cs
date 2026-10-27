using UnityEngine;
using Verse;

namespace StuffablePowerArmor
{
    public class StuffablePowerArmorMod : Mod
    {
        public static StuffablePowerArmorMod Instance;
        public static StuffablePowerArmorSettings Settings;

        public StuffablePowerArmorMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<StuffablePowerArmorSettings>();
        }

        public override string SettingsCategory()
        {
            return "StuffablePowerArmor.SettingsCategory".Translate().ToString();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            Settings.Write();
        }
    }
}
