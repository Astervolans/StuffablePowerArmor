using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using Verse;

namespace StuffablePowerArmor
{
    /// <summary>
    /// Harness-only production smoke test. Runs only when RW_TEST_OUTPUT is set by the isolated
    /// test loop; it never executes in a normal game and never changes any game state.
    /// </summary>
    internal static class HarnessChecks
    {
        public static void RunIfRequested()
        {
            string directory = Environment.GetEnvironmentVariable("RW_TEST_OUTPUT");
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            try
            {
                bool costOk = CheckCosts(out string costDetail);
                bool statsOk = CheckStats(out string statsDetail);
                Marker(directory, costOk ? "SPA_PROD_COST_OK " + costDetail : "SPA_PROD_COST_FAIL " + costDetail);
                Marker(directory, statsOk ? "SPA_PROD_STATS_OK " + statsDetail : "SPA_PROD_STATS_FAIL " + statsDetail);
                Marker(directory, (ArmorDefs.CombatExtendedActive ? "SPA_PROD_CE_ACTIVE" : "SPA_PROD_CE_OFF") +
                                  " armorSharp=" + SafeStat("Apparel_PowerArmor", StatDefOf.ArmorRating_Sharp).ToString("0.###"));
                int multiplierCount = ArmorDefs.All.Count(d =>
                    Math.Abs(d.GetStatValueAbstract(StatDefOf.StuffEffectMultiplierArmor) -
                             ArmorDefs.AppliedEffectFraction) <= 0.01f);
                Marker(directory, "SPA_PROD_MULT_DEFS count=" + multiplierCount + "/" + ArmorDefs.All.Count);
            }
            catch (Exception exception)
            {
                Marker(directory, "SPA_PROD_CHECK_FAIL " + exception.GetType().Name + " " + exception.Message);
            }
        }

        private static bool CheckCosts(out string detail)
        {
            var problems = new List<string>();
            foreach (ThingDef def in ArmorDefs.All)
            {
                int plasteelInList = 0;
                if (def.CostList != null)
                {
                    foreach (ThingDefCountClass cost in def.CostList)
                    {
                        if (cost?.thingDef == ThingDefOf.Plasteel)
                        {
                            plasteelInList += cost.count;
                        }
                    }
                }

                if (plasteelInList != 20)
                {
                    problems.Add(def.defName + ":costList=" + plasteelInList);
                }

                if (def.CostStuffCount <= 0)
                {
                    problems.Add(def.defName + ":stuffCount=" + def.CostStuffCount);
                }

                RecipeDef recipe = DefDatabase<RecipeDef>.AllDefsListForReading
                    .FirstOrDefault(r => r != null && r.defName == "Make_" + def.defName);
                if (recipe == null)
                {
                    problems.Add(def.defName + ":noRecipe");
                    continue;
                }

                IngredientCount fixedPlasteel = recipe.ingredients?
                    .FirstOrDefault(i => i != null && i.IsFixedIngredient && i.FixedIngredient == ThingDefOf.Plasteel);
                if (fixedPlasteel == null || Math.Abs(fixedPlasteel.GetBaseCount() - 20f) > 0.01f)
                {
                    problems.Add(def.defName + ":recipePlasteel=" +
                                 (fixedPlasteel == null ? "null" : fixedPlasteel.GetBaseCount().ToString("0.##")));
                }

                IngredientCount stuffIngredient = recipe.ingredients?
                    .FirstOrDefault(i => i != null && !i.IsFixedIngredient);
                if (stuffIngredient == null ||
                    Math.Abs(stuffIngredient.GetBaseCount() - def.CostStuffCount) > 0.01f)
                {
                    problems.Add(def.defName + ":recipeStuff=" +
                                 (stuffIngredient == null ? "null" : stuffIngredient.GetBaseCount().ToString("0.##")));
                }

                if (stuffIngredient?.filter == null || !stuffIngredient.filter.Allows(ThingDefOf.Plasteel))
                {
                    problems.Add(def.defName + ":filterNoPlasteel");
                }
            }

            detail = problems.Count == 0
                ? "defs=" + ArmorDefs.All.Count + " plasteel=20 stuff=original-20"
                : string.Join(";", problems.Take(8)) + " total=" + problems.Count;
            return problems.Count == 0;
        }

        private static bool CheckStats(out string detail)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_PowerArmor");
            if (def == null)
            {
                detail = "missing Apparel_PowerArmor";
                return false;
            }

            float Place(ThingDef stuff, StatDef stat)
            {
                return StatExtension.GetStatValueAbstract(def, stat, stuff);
            }

            float plasteelSharp = Place(ThingDefOf.Plasteel, StatDefOf.ArmorRating_Sharp);
            float steelSharp = Place(ThingDefOf.Steel, StatDefOf.ArmorRating_Sharp);
            float vanillaSharp = def.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp);
            float plasteelHp = Place(ThingDefOf.Plasteel, StatDefOf.MaxHitPoints);
            float steelHp = Place(ThingDefOf.Steel, StatDefOf.MaxHitPoints);
            float vanillaHp = def.GetStatValueAbstract(StatDefOf.MaxHitPoints);
            float multiplier = def.GetStatValueAbstract(StatDefOf.StuffEffectMultiplierArmor);
            float expectedMultiplier = ArmorDefs.AppliedEffectFraction;

            bool ok = Math.Abs(plasteelSharp - vanillaSharp) <= 0.01f &&
                      Math.Abs(plasteelHp - vanillaHp) <= 0.01f &&
                      steelSharp < plasteelSharp &&
                      steelHp < plasteelHp &&
                      Math.Abs(multiplier - expectedMultiplier) <= 0.01f;
            detail = "sharp " + plasteelSharp.ToString("0.###") + "/" + steelSharp.ToString("0.###") +
                     " vanilla=" + vanillaSharp.ToString("0.###") +
                     " hp " + plasteelHp.ToString("0.##") + "/" + steelHp.ToString("0.##") +
                     " vanilla=" + vanillaHp.ToString("0.##") +
                     " materialMultiplier=" + multiplier.ToString("0.###");
            return ok;
        }

        private static float SafeStat(string defName, StatDef stat)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            return def == null ? 0f : def.GetStatValueAbstract(stat);
        }

        private static void Marker(string directory, string text)
        {
            try
            {
                Directory.CreateDirectory(directory);
                File.AppendAllText(Path.Combine(directory, "markers.txt"),
                    "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + text + Environment.NewLine);
            }
            catch
            {
                // The game log is the fallback.
            }

            Log.Message("TESTMARKER " + text);
        }
    }
}
