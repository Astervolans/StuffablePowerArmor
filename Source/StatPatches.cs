using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StuffablePowerArmor
{
    /// <summary>
    /// Replaces the native material contribution for the supported armor with the mod's
    /// plasteel-relative, percentage-scaled model.
    ///
    /// At plasteel every branch collapses to the un-stuffed (vanilla) value. The no-stuff
    /// value is computed via the same worker with a StatRequest that has no stuff, so quality
    /// and stat parts are still applied exactly as vanilla would apply them to the base def.
    /// </summary>
    [HarmonyPatch(typeof(StatWorker), "GetValue", new[] { typeof(StatRequest), typeof(bool) })]
    public static class StatWorkerGetValuePatch
    {
        private static readonly FieldInfo StatField =
            typeof(StatWorker).GetField("stat", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly HashSet<int> loggedErrors = new HashSet<int>();

        /// <summary>Temporary effect override used while rescaling saved hit-point ratios.</summary>
        public static float? EffectOverride;

        [HarmonyPostfix]
        public static void Postfix(StatWorker __instance, StatRequest req, bool applyPostProcess, ref float __result)
        {
            try
            {
                Apply(__instance, req, applyPostProcess, ref __result);
            }
            catch (Exception exception)
            {
                // A stat patch must never take down map/world generation. Log once and keep the
                // vanilla-computed value.
                StatDef errorStat = StatField?.GetValue(__instance) as StatDef;
                int hash = (errorStat?.shortHash ?? 0) * 397 ^ (req.Thing?.def?.shortHash ?? 0);
                if (loggedErrors.Add(hash))
                {
                    Log.Error("[Stuffable Power Armor] Stat patch exception stat=" +
                              (errorStat?.defName ?? "?") +
                              " def=" + (req.BuildableDef?.defName ?? "?") +
                              " stuff=" + (req.StuffDef?.defName ?? "null") +
                              " thing=" + (req.Thing?.GetType().Name ?? "null") + ": " + exception);
                }
            }
        }

        private static void Apply(StatWorker __instance, StatRequest req, bool applyPostProcess, ref float __result)
        {
            if (!ArmorDefs.Ready || __instance == null || StatField == null)
            {
                return;
            }

            if (!(req.BuildableDef is ThingDef def) || !ArmorDefs.IsSupported(def))
            {
                return;
            }

            ThingDef stuff = req.StuffDef;
            if (stuff == null)
            {
                return;
            }

            StatDef stat = StatField.GetValue(__instance) as StatDef;
            if (stat == null)
            {
                return;
            }

            // Market value is intentionally left to vanilla: it is derived from the material's
            // own market value/cost and therefore naturally reflects the chosen material.
            if (stat == StatDefOf.MarketValue || stat == StatDefOf.MarketValueIgnoreHp)
            {
                return;
            }

            float effect = EffectOverride ?? ArmorDefs.AppliedEffectFraction;

            StatRequest baseRequest = StatRequest.For(def, null, req.QualityCategory);
            float baseValue = __instance.GetValue(baseRequest, applyPostProcess);

            if (stat == StatDefOf.ArmorRating_Sharp)
            {
                __result = ArmorValue(baseValue, def, stuff, stat, StatDefOf.StuffPower_Armor_Sharp, effect);
                return;
            }

            if (stat == StatDefOf.ArmorRating_Blunt)
            {
                __result = ArmorValue(baseValue, def, stuff, stat, StatDefOf.StuffPower_Armor_Blunt, effect);
                return;
            }

            if (stat == StatDefOf.ArmorRating_Heat)
            {
                __result = ArmorValue(baseValue, def, stuff, stat, StatDefOf.StuffPower_Armor_Heat, effect);
                return;
            }

            if (stat == StatDefOf.Insulation_Cold)
            {
                __result = InsulationValue(baseValue, def, stuff, stat, StatDefOf.StuffPower_Insulation_Cold,
                    StatDefOf.StuffEffectMultiplierInsulation_Cold, effect);
                return;
            }

            if (stat == StatDefOf.Insulation_Heat)
            {
                __result = InsulationValue(baseValue, def, stuff, stat, StatDefOf.StuffPower_Insulation_Heat,
                    StatDefOf.StuffEffectMultiplierInsulation_Heat, effect);
                return;
            }

            if (stat == StatDefOf.MaxHitPoints)
            {
                __result = FactorValue(baseValue, stuff, StatDefOf.MaxHitPoints, effect);
                return;
            }

            if (stat == StatDefOf.WorkToMake)
            {
                __result = FactorValue(baseValue, stuff, StatDefOf.WorkToMake, effect);
                return;
            }

            if (stat == StatDefOf.Mass)
            {
                __result = FactorValue(baseValue, stuff, StatDefOf.Mass, effect);
                return;
            }

            if (stat == StatDefOf.Flammability)
            {
                // Neutralize the native material factor for supported armor. Plasteel (the baseline)
                // would otherwise multiply the vanilla 0.4 by its 0 factor and change vanilla stats.
                __result = baseValue;
                return;
            }

            // Other stats keep their native material behavior. For plasteel these are already the
            // vanilla values (Beauty -3, MarketValue from costList+stuff, DeteriorationRate 2, ...).
        }

        private static float ArmorValue(float baseValue, ThingDef def, ThingDef stuff, StatDef stat,
            StatDef powerStat, float effect)
        {
            float power = ArmorDefs.GetStuffPower(stuff, powerStat);
            float plasteelPower = ArmorDefs.GetStuffPower(ThingDefOf.Plasteel, powerStat);
            float qualityFactor = QualityFactor(def, stat, baseValue);

            // Combat Extended expresses these stats in millimetres of RHA, not in the vanilla
            // 0..2 percentage range. Scale the material delta to the CE armour magnitude and do
            // not clip it at 2 mm (which was the reported “all armour is 2 mm” bug).
            if (ArmorDefs.CombatExtendedActive)
            {
                float ceScale = Math.Max(1f, baseValue) * 0.5f;
                float ceValue = baseValue + effect * (power - plasteelPower) * qualityFactor * ceScale;
                return Math.Max(0f, ceValue);
            }

            float value = baseValue + effect * (power - plasteelPower) * qualityFactor;

            // ArmorRating_* is clamped to [0, 2] by the game; stone blocks deliberately have no
            // StuffPower armor stats so a stone suit ends up with no extra protection, never below 0.
            return Math.Max(0f, Math.Min(2f, value));
        }

        private static float InsulationValue(float baseValue, ThingDef def, ThingDef stuff, StatDef stat,
            StatDef powerStat, StatDef multiplierStat, float effect)
        {
            float power = ArmorDefs.GetStuffPower(stuff, powerStat);
            float plasteelPower = ArmorDefs.GetStuffPower(ThingDefOf.Plasteel, powerStat);
            float qualityFactor = QualityFactor(def, stat, baseValue);
            return baseValue + effect * (power - plasteelPower) * qualityFactor;
        }

        /// <summary>
        /// Armor/insulation stats have a StatPart_Quality that multiplies the whole final value
        /// (the stuff part runs before the quality part). Scale the material delta by the same
        /// quality factor so a legendary wooden suit is not disproportionately good.
        /// </summary>
        private static float QualityFactor(ThingDef def, StatDef stat, float baseValue)
        {
            if (baseValue <= 0f)
            {
                return 1f;
            }

            float normalQualityValue = def.GetStatValueAbstract(stat);
            if (normalQualityValue <= 0f)
            {
                return 1f;
            }

            return baseValue / normalQualityValue;
        }

        private static float FactorValue(float baseValue, ThingDef stuff, StatDef stat, float effect)
        {
            float factor = ArmorDefs.GetFactor(stuff, stat);
            float plasteelFactor = ArmorDefs.GetFactor(ThingDefOf.Plasteel, stat);
            if (plasteelFactor <= 0f)
            {
                return baseValue;
            }

            return baseValue * (1f + effect * (factor / plasteelFactor - 1f));
        }
    }
}
