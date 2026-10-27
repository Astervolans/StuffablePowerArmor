using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace StuffablePowerArmor
{
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {
            try
            {
                var harmony = new HarmonyLib.Harmony("locals.stuffablepowerarmor");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("[Stuffable Power Armor] Harmony patches applied.");
                HarnessMarker("SPA_PROD_HARMONY_OK");
            }
            catch (Exception exception)
            {
                Log.Error("[Stuffable Power Armor] Failed to apply Harmony patches: " + exception);
                HarnessMarker("SPA_PROD_HARMONY_FAIL " + exception.GetType().Name);
            }

            ArmorDefs.Init();
            ArmorDefs.InitializeAppliedEffect(StuffablePowerArmorMod.Settings?.EffectFraction ?? 1f);

            try
            {
                ArmorDefs.ApplyCategorySettings();
            }
            catch (Exception exception)
            {
                Log.Error("[Stuffable Power Armor] Failed to apply material category settings: " + exception);
            }

            Log.Message("[Stuffable Power Armor] Initialized.");
            HarnessMarker("SPA_PROD_LOAD_OK defs=" + ArmorDefs.All.Count);
            HarnessChecks.RunIfRequested();
        }

        /// <summary>
        /// Writes a marker for the isolated test harness only (RW_TEST_OUTPUT is set by the harness);
        /// this does nothing in a normal game and does not affect gameplay.
        /// </summary>
        private static void HarnessMarker(string text)
        {
            string directory = Environment.GetEnvironmentVariable("RW_TEST_OUTPUT");
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(directory);
                File.AppendAllText(Path.Combine(directory, "markers.txt"),
                    "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + text + Environment.NewLine);
            }
            catch
            {
                // Ignore harness marker failures; the game log still has the same information.
            }

            Log.Message("TESTMARKER " + text);
        }
    }
}
