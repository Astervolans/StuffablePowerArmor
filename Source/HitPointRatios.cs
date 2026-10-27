using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace StuffablePowerArmor
{
    /// <summary>
    /// Keeps the damaged/healthy ratio of supported armor when its maximum hit points change.
    ///
    /// MaxHitPoints depends on the material and on the mod's material-effect percentage. If the
    /// percentage is changed (and the game restarted), or if cleanup clears the material, the
    /// maximum changes while the saved HitPoints value is absolute. This helper converts the old
    /// absolute value to the same ratio against the new maximum.
    /// </summary>
    public static class HitPointRatios
    {
        public static float MaxHitPoints(Thing thing, ThingDef stuff, float effect)
        {
            if (thing?.def == null)
            {
                return 0f;
            }

            QualityCategory quality = thing.TryGetComp<CompQuality>()?.Quality ?? QualityCategory.Normal;
            return MaxHitPoints(thing.def, stuff, quality, effect);
        }

        public static float MaxHitPoints(ThingDef def, ThingDef stuff, QualityCategory quality, float effect)
        {
            if (def == null)
            {
                return 0f;
            }

            float? previous = StatWorkerGetValuePatch.EffectOverride;
            StatWorkerGetValuePatch.EffectOverride = effect;
            try
            {
                return StatDefOf.MaxHitPoints.Worker.GetValue(
                    StatRequest.For(def, stuff, quality), true);
            }
            catch (Exception exception)
            {
                Log.ErrorOnce("[Stuffable Power Armor] Could not evaluate MaxHitPoints while scaling hit points: " +
                              exception, def.shortHash * 397 ^ (stuff?.shortHash ?? 0));
                return 0f;
            }
            finally
            {
                StatWorkerGetValuePatch.EffectOverride = previous;
            }
        }

        public static void PreserveRatio(Thing thing, float oldMax, float newMax)
        {
            if (thing == null || oldMax <= 0f || newMax <= 0f)
            {
                return;
            }

            float ratio = Math.Max(0f, Math.Min(1f, thing.HitPoints / oldMax));
            int newHitPoints = Mathf.Clamp(Mathf.RoundToInt(ratio * newMax), 1, Mathf.RoundToInt(newMax));
            thing.HitPoints = newHitPoints;
        }

        /// <summary>
        /// Called once after loading a save: if the save recorded a different material-effect
        /// fraction than the one applied in this session, rescale every supported item's hit points.
        /// </summary>
        public static void ApplySavedEffectFraction(float savedFraction)
        {
            float current = ArmorDefs.AppliedEffectFraction;
            if (savedFraction <= 0f || Math.Abs(savedFraction - current) <= 0.001f)
            {
                return;
            }

            int changed = 0;
            ArmorCleanup.ForEachSavedThing(thing =>
            {
                if (thing == null || !ArmorDefs.IsSupported(thing.def) || thing.Stuff == null)
                {
                    return;
                }

                float oldMax = MaxHitPoints(thing, thing.Stuff, savedFraction);
                float newMax = MaxHitPoints(thing, thing.Stuff, current);
                if (oldMax <= 0f || Math.Abs(oldMax - newMax) < 0.01f)
                {
                    return;
                }

                PreserveRatio(thing, oldMax, newMax);
                changed++;
            });

            if (changed > 0)
            {
                Log.Message("[Stuffable Power Armor] Rescaled hit-point ratios on " + changed +
                            " armor item(s) after the material-effect setting changed (" +
                            savedFraction.ToString("P0") + " -> " + current.ToString("P0") + ").");
            }
        }
    }
}
