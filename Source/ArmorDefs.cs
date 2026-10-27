using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace StuffablePowerArmor
{
    /// <summary>
    /// Registry and helpers for the 18 supported power-armor ThingDefs.
    /// </summary>
    public static class ArmorDefs
    {
        public static readonly string[] DefNames =
        {
            "Apparel_ArmorRecon",
            "Apparel_ArmorHelmetRecon",
            "Apparel_PowerArmor",
            "Apparel_PowerArmorHelmet",
            "Apparel_MechlordSuit",
            "Apparel_ArmorHelmetMechlordHelmet",
            "Apparel_ArmorHelmetMechCommander",
            "Apparel_ArmorMarineGrenadier",
            "Apparel_ArmorLocust",
            "Apparel_ArmorCataphract",
            "Apparel_ArmorHelmetCataphract",
            "Apparel_ArmorCataphractPhoenix",
            "Apparel_ArmorMarinePrestige",
            "Apparel_ArmorMarineHelmetPrestige",
            "Apparel_ArmorReconPrestige",
            "Apparel_ArmorHelmetReconPrestige",
            "Apparel_ArmorCataphractPrestige",
            "Apparel_ArmorHelmetCataphractPrestige"
        };

        private static readonly HashSet<ThingDef> supported = new HashSet<ThingDef>();
        private static List<StuffCategoryDef> enabledCategories;
        private static float appliedEffectFraction = 1f;

        public static bool Ready { get; private set; }

        /// <summary>
        /// Effect fraction applied to this running session. Settings are read once at startup, so a
        /// slider/category change cannot affect items mid-session (which would break hit-point
        /// ratios and is why the settings window asks for a restart).
        /// </summary>
        public static float AppliedEffectFraction => appliedEffectFraction;

        public static IReadOnlyCollection<ThingDef> All => supported;

        public static void InitializeAppliedEffect(float fraction)
        {
            appliedEffectFraction = Math.Max(0.2f, Math.Min(1f, fraction));
            foreach (ThingDef def in supported)
            {
                SetMaterialEffectMultipliers(def, appliedEffectFraction);
            }
        }

        public static void SetMaterialEffectMultipliers(ThingDef def, float fraction)
        {
            SetStatBase(def, StatDefOf.StuffEffectMultiplierArmor, fraction);
            SetStatBase(def, StatDefOf.StuffEffectMultiplierInsulation_Cold, fraction);
            SetStatBase(def, StatDefOf.StuffEffectMultiplierInsulation_Heat, fraction);
        }

        public static void Init()
        {
            supported.Clear();
            foreach (string name in DefNames)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (def == null)
                {
                    Log.Warning("[Stuffable Power Armor] Supported def not found (missing DLC?): " + name);
                    continue;
                }

                supported.Add(def);
                SetMaterialEffectMultipliers(def, appliedEffectFraction);
                ApplyCostSplit(def);
            }

            Ready = true;
            Log.Message("[Stuffable Power Armor] Registered " + supported.Count + " supported armor defs.");
        }

        private static bool? combatExtendedActive;

        /// <summary>True when Combat Extended is active; used by the stat layer for its mm armor scale.</summary>
        public static bool CombatExtendedActive
        {
            get
            {
                if (combatExtendedActive.HasValue)
                {
                    return combatExtendedActive.Value;
                }

                combatExtendedActive = AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
                                            string.Equals(assembly.GetName().Name, "CombatExtended",
                                                StringComparison.OrdinalIgnoreCase)) ||
                                        ModLister.AllInstalledMods.Any(meta =>
                                            meta != null && meta.Active &&
                                            (string.Equals(meta.PackageId, "CETeam.CombatExtended", StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(meta.Name, "Combat Extended", StringComparison.OrdinalIgnoreCase)));
                return combatExtendedActive.Value;
            }
        }

        /// <summary>
        /// Adds/replaces the native stuff-effect multiplier stats. They are the game's own
        /// material-effect mechanism, shown in the item stat card as “material effect multiplier”
        /// and read by Combat Extended's apparel autopatcher. The value tracks the applied
        /// percentage setting (20%..100%), so the stat card reflects the mod setting.
        /// </summary>
        private static void SetStatBase(ThingDef def, StatDef stat, float value)
        {
            var list = def.statBases == null
                ? new List<StatModifier>()
                : new List<StatModifier>(def.statBases);

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].stat == stat)
                {
                    list[i] = new StatModifier { stat = stat, value = value };
                    def.statBases = list;
                    return;
                }
            }

            // Copy before adding: in 1.7 inherited statBases lists can be shared with the parent
            // def, and mutating the shared list would add these stats to unrelated apparel.
            list.Add(new StatModifier { stat = stat, value = value });
            def.statBases = list;
        }

        /// <summary>
        /// The XML patch leaves the original costList untouched (changing map-form costList at XML
        /// inheritance time proved hazardous during world generation). Instead, after defs resolve,
        /// keep 20 plasteel in costList and fix the generated recipe's fixed plasteel ingredient to
        /// the same 20. The generated stuff ingredient already uses costStuffCount = original - 20.
        /// </summary>
        private static void ApplyCostSplit(ThingDef def)
        {
            List<ThingDefCountClass> costList = def.CostList;
            if (costList != null)
            {
                foreach (ThingDefCountClass cost in costList)
                {
                    if (cost != null && cost.thingDef == ThingDefOf.Plasteel && cost.count > 20)
                    {
                        cost.count = 20;
                    }
                }
            }

            List<RecipeDef> allRecipes = DefDatabase<RecipeDef>.AllDefsListForReading;
            for (int i = 0; i < allRecipes.Count; i++)
            {
                RecipeDef recipe = allRecipes[i];
                if (recipe == null || recipe.ProducedThingDef != def || recipe.ingredients == null)
                {
                    continue;
                }

                foreach (IngredientCount ingredient in recipe.ingredients)
                {
                    if (ingredient != null && ingredient.IsFixedIngredient &&
                        ingredient.FixedIngredient == ThingDefOf.Plasteel)
                    {
                        ingredient.SetBaseCount(20f);
                    }
                }
            }
        }

        public static bool IsSupported(ThingDef def)
        {
            return def != null && supported.Contains(def);
        }

        public static bool IsSupportedName(string defName)
        {
            return defName != null && DefNames.Contains(defName);
        }

        public static float GetFactor(ThingDef stuff, StatDef stat)
        {
            if (stuff?.stuffProps?.statFactors == null)
            {
                return 1f;
            }

            for (int i = 0; i < stuff.stuffProps.statFactors.Count; i++)
            {
                StatModifier modifier = stuff.stuffProps.statFactors[i];
                if (modifier.stat == stat)
                {
                    return modifier.value;
                }
            }

            return 1f;
        }

        public static float GetOffset(ThingDef stuff, StatDef stat)
        {
            if (stuff?.stuffProps?.statOffsets == null)
            {
                return 0f;
            }

            for (int i = 0; i < stuff.stuffProps.statOffsets.Count; i++)
            {
                StatModifier modifier = stuff.stuffProps.statOffsets[i];
                if (modifier.stat == stat)
                {
                    return modifier.value;
                }
            }

            return 0f;
        }

        public static float GetStuffPower(ThingDef stuff, StatDef powerStat)
        {
            return stuff == null ? 0f : stuff.GetStatValueAbstract(powerStat);
        }

        public static float GetApparelMultiplier(ThingDef apparelDef, StatDef multiplierStat)
        {
            return apparelDef == null || multiplierStat == null
                ? 1f
                : apparelDef.GetStatValueAbstract(multiplierStat);
        }

        public static List<StuffCategoryDef> EnabledCategories()
        {
            if (enabledCategories == null)
            {
                RebuildEnabledCategories();
            }

            return enabledCategories;
        }

        public static void RebuildEnabledCategories()
        {
            var list = new List<StuffCategoryDef>();
            StuffablePowerArmorSettings settings = StuffablePowerArmorMod.Settings;

            AddCategory(list, "Metallic");
            if (settings != null && settings.allowWoody)
            {
                AddCategory(list, "Woody");
            }

            if (settings != null && settings.allowFabric)
            {
                AddCategory(list, "Fabric");
                AddCategory(list, "Leathery");
            }

            if (settings != null && settings.allowStony)
            {
                AddCategory(list, "Stony");
            }

            enabledCategories = list;
        }

        private static void AddCategory(List<StuffCategoryDef> list, string defName)
        {
            StuffCategoryDef category = DefDatabase<StuffCategoryDef>.GetNamedSilentFail(defName);
            if (category != null && !list.Contains(category))
            {
                list.Add(category);
            }
        }

        /// <summary>
        /// Applies the user's category toggles to every supported ThingDef, to the generated
        /// recipes' stuff filters, and to existing bills. The generated recipes are created before
        /// StaticConstructorOnStartup, so their IngredientCount filters have to be adjusted here.
        /// The recipeMaker default filter disallows Plasteel/Gold/Silver/Uranium; SetAllow overrides
        /// that because ThingFilter.Allows(ThingDef) only consults the materialized allowedDefs set.
        /// </summary>
        public static void ApplyCategorySettings()
        {
            if (!Ready)
            {
                return;
            }

            RebuildEnabledCategories();
            List<StuffCategoryDef> categories = enabledCategories;
            var enabledCategorySet = new HashSet<StuffCategoryDef>(categories);
            string categorySummary = string.Join(", ", categories.Select(c => c.LabelCap));

            var recipesToUpdate = new List<RecipeDef>();
            foreach (ThingDef apparelDef in supported)
            {
                apparelDef.stuffCategories = new List<StuffCategoryDef>(categories);
            }

            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe == null || recipe.ingredients == null)
                {
                    continue;
                }

                if (IsSupported(recipe.ProducedThingDef))
                {
                    recipesToUpdate.Add(recipe);
                }
            }

            foreach (RecipeDef recipe in recipesToUpdate)
            {
                if (recipe.ingredients != null)
                {
                    foreach (IngredientCount ingredient in recipe.ingredients)
                    {
                        if (ingredient == null || ingredient.IsFixedIngredient || ingredient.filter == null)
                        {
                            continue;
                        }

                        ApplyStuffAllowances(ingredient.filter, enabledCategorySet);
                        ingredient.filter.customSummary = categorySummary;
                    }
                }

                ApplyStuffAllowances(recipe.defaultIngredientFilter, enabledCategorySet);
                ApplyStuffAllowances(recipe.fixedIngredientFilter, enabledCategorySet);
            }

            // Existing bills keep their own materialized ThingFilter (there is no Bill.stuff field).
            if (Find.Maps != null)
            {
                foreach (Map map in Find.Maps)
                {
                    if (map?.listerThings == null)
                    {
                        continue;
                    }

                    foreach (Thing thing in map.listerThings.AllThings)
                    {
                        if (thing is IBillGiver billGiver && billGiver.BillStack?.Bills != null)
                        {
                            foreach (Bill bill in billGiver.BillStack.Bills)
                            {
                                if (bill?.recipe != null && bill.ingredientFilter != null &&
                                    IsSupported(bill.recipe.ProducedThingDef))
                                {
                                    ApplyStuffAllowances(bill.ingredientFilter, enabledCategorySet);
                                }
                            }
                        }
                    }
                }
            }

            Log.Message("[Stuffable Power Armor] Applied material categories: " +
                        string.Join(", ", categories.Select(c => c.defName)));
        }

        private static void ApplyStuffAllowances(ThingFilter filter, HashSet<StuffCategoryDef> enabledCategorySet)
        {
            if (filter == null)
            {
                return;
            }

            foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (stuff == null || !stuff.IsStuff || stuff.stuffProps?.categories == null)
                {
                    continue;
                }

                bool allowed = stuff.stuffProps.categories.Any(c => enabledCategorySet.Contains(c));
                filter.SetAllow(stuff, allowed);
            }
        }

        /// <summary>
        /// Clears the stat caches of the stats we override so a percentage change takes
        /// effect immediately (several of these stats are cached per Thing).
        /// </summary>
        public static void InvalidateStatCaches()
        {
            if (!Ready)
            {
                return;
            }

            StatDef[] stats =
            {
                StatDefOf.ArmorRating_Sharp,
                StatDefOf.ArmorRating_Blunt,
                StatDefOf.ArmorRating_Heat,
                StatDefOf.Insulation_Cold,
                StatDefOf.Insulation_Heat,
                StatDefOf.MaxHitPoints,
                StatDefOf.WorkToMake,
                StatDefOf.Flammability,
                StatDefOf.Beauty,
                StatDefOf.Mass,
                StatDefOf.MarketValue
            };

            foreach (StatDef stat in stats)
            {
                stat?.Worker?.TryClearCache();
            }
        }
    }
}
