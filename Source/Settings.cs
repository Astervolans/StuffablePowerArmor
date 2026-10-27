using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace StuffablePowerArmor
{
    /// <summary>
    /// Persistent mod options. The percentage is the only thing that affects stat math;
    /// category toggles are applied to the ThingDefs and generated recipes when changed.
    /// </summary>
    public class StuffablePowerArmorSettings : ModSettings
    {
        public float materialEffectPercent = 100f;
        public bool allowWoody;
        public bool allowFabric;
        public bool allowStony;

        private static readonly float[] PercentageSteps = { 20f, 25f, 30f, 35f, 40f, 45f, 50f, 55f, 60f, 65f, 70f, 75f, 80f, 85f, 90f, 95f, 100f };

        public float EffectFraction
        {
            get
            {
                float pct = Math.Max(20f, Math.Min(100f, materialEffectPercent));
                return pct / 100f;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref materialEffectPercent, "materialEffectPercent", 100f);
            Scribe_Values.Look(ref allowWoody, "allowWoody", false);
            Scribe_Values.Look(ref allowFabric, "allowFabric", false);
            Scribe_Values.Look(ref allowStony, "allowStony", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                materialEffectPercent = SnapToStep(materialEffectPercent);
            }
        }

        public void DoWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("StuffablePowerArmor.MaterialEffect".Translate().ToString());
            materialEffectPercent = listing.SliderLabeled(
                "StuffablePowerArmor.PercentLabel".Translate(materialEffectPercent.ToString("F0") + "%").ToString(),
                materialEffectPercent,
                20f,
                100f,
                0.75f,
                "StuffablePowerArmor.MaterialEffectTooltip".Translate().ToString());
            materialEffectPercent = SnapToStep(materialEffectPercent);
            listing.Label("StuffablePowerArmor.NeedsRestart".Translate().ToString());

            listing.Gap(12f);
            listing.Label("StuffablePowerArmor.AllowedCategories".Translate().ToString());
            listing.GapLine();
            listing.CheckboxLabeled(
                "StuffablePowerArmor.CatWoody".Translate().ToString(),
                ref allowWoody,
                "StuffablePowerArmor.AllowedCategoriesTooltip".Translate().ToString());
            listing.CheckboxLabeled(
                "StuffablePowerArmor.CatFabric".Translate().ToString(),
                ref allowFabric,
                "StuffablePowerArmor.AllowedCategoriesTooltip".Translate().ToString());
            listing.CheckboxLabeled(
                "StuffablePowerArmor.CatStony".Translate().ToString(),
                ref allowStony,
                "StuffablePowerArmor.AllowedCategoriesTooltip".Translate().ToString());
            bool metallicAlwaysAllowed = true;
            listing.CheckboxLabeled(
                "StuffablePowerArmor.CatMetallic".Translate().ToString(),
                ref metallicAlwaysAllowed,
                "StuffablePowerArmor.CatMetallicLocked".Translate().ToString());
            metallicAlwaysAllowed = true;
            listing.Gap(12f);

            if (listing.ButtonText("StuffablePowerArmor.CleanupButton".Translate().ToString(),
                    "StuffablePowerArmor.CleanupTooltip".Translate().ToString()))
            {
                ArmorCleanup.RunAndReport();
            }

            listing.End();

            // Do not touch StatDef/StatWorker caches or defs from the UI thread here. In RimWorld
            // 1.7 StatWorker.DeleteStatCache() sets the cache dictionaries to null (it does not
            // allocate fresh ones), so a following stat lookup throws NullReferenceException.
            // All setting changes are therefore persisted and applied during the next game load.
        }

        private static float SnapToStep(float value)
        {
            if (float.IsNaN(value))
            {
                return 100f;
            }

            float best = PercentageSteps[0];
            float bestDistance = float.MaxValue;
            for (int i = 0; i < PercentageSteps.Length; i++)
            {
                float distance = Math.Abs(PercentageSteps[i] - value);
                if (distance < bestDistance)
                {
                    best = PercentageSteps[i];
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
