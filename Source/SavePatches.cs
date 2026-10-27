using HarmonyLib;
using RimWorld;
using Verse;

namespace StuffablePowerArmor
{
    /// <summary>
    /// Older saves contain supported armor with no &lt;stuff&gt; node. Vanilla then logs a red
    /// error during PostLoadInit before assigning a default. Set plasteel as soon as the load
    /// phase starts so the item is treated as plasteel and no error is emitted.
    /// </summary>
    [HarmonyPatch(typeof(Thing), nameof(Thing.ExposeData))]
    public static class ThingExposeDataPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Thing __instance)
        {
            if (__instance?.def == null)
            {
                return;
            }

            LoadSaveMode mode = Scribe.mode;
            if (mode == LoadSaveMode.Saving || mode == LoadSaveMode.Inactive)
            {
                return;
            }

            // Do not require ArmorDefs.Ready here: an autostart/quicktest save can be loaded very
            // early, and the name check only needs the static def-name list.
            if (__instance.Stuff == null && ArmorDefs.IsSupportedName(__instance.def.defName))
            {
                __instance.SetStuffDirect(ThingDefOf.Plasteel);
            }
        }
    }

    /// <summary>
    /// Records the material-effect fraction that was actually applied when the save was written.
    /// On the next load, if the user changed the setting in the meantime, hit-point ratios are
    /// rescaled from the saved maximum to the newly applied maximum.
    /// </summary>
    public static class SavedEffectFraction
    {
        private const string NodeName = "SPA_EffectFraction";
        private static float pending = -1f;

        [HarmonyPatch(typeof(Game), nameof(Game.ExposeData))]
        public static class GameExposeDataPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    float value = ArmorDefs.AppliedEffectFraction;
                    Scribe_Values.Look(ref value, NodeName, 1f);
                }
            }
        }

        /// <summary>
        /// Game.LoadGame reads the game node manually (it never calls Game.ExposeData on load),
        /// so read the saved fraction here, while Scribe's current node is still &lt;game&gt;.
        /// </summary>
        [HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
        public static class GameLoadGamePatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                float value = -1f;
                Scribe_Values.Look(ref value, NodeName, -1f);
                pending = value;
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
        public static class GameFinalizeInitPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                float saved = pending;
                pending = -1f;
                if (saved > 0f)
                {
                    HitPointRatios.ApplySavedEffectFraction(saved);
                }
            }
        }
    }
}
