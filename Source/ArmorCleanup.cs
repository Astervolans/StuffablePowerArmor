using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace StuffablePowerArmor
{
    /// <summary>
    /// Clears the material selection on every supported armor item in the current save and reports
    /// whether it is safe to remove the mod. Scans maps, containers, pawns, caravans and world pawns.
    /// </summary>
    public static class ArmorCleanup
    {
        public static void RunAndReport()
        {
            if (Current.Game == null)
            {
                ShowDialog("StuffablePowerArmor.CleanupNone".Translate().ToString());
                return;
            }

            int cleaned = CleanAll();
            int remaining = CountConfigured();

            string text;
            if (remaining == 0)
            {
                string safe = "StuffablePowerArmor.SafeToRemove".Translate().ToString();
                if (cleaned > 0)
                {
                    text = "StuffablePowerArmor.CleanupDone".Translate(cleaned).ToString() + "\n\n" + safe;
                }
                else
                {
                    text = safe;
                }
            }
            else
            {
                text = "StuffablePowerArmor.CleanupRemaining".Translate(remaining).ToString();
            }

            ShowDialog(text);
        }

        private static void ShowDialog(string text)
        {
            string title = "StuffablePowerArmor.CleanupDialogTitle".Translate().ToString();
            Find.WindowStack.Add(new Dialog_MessageBox(text, "OK".Translate().ToString(), null, null, null, title));
        }

        internal static int CleanAll()
        {
            int cleaned = 0;
            ForEachSavedThing(thing =>
            {
                if (!IsConfigured(thing))
                {
                    return;
                }

                // Clear the material reference completely. If the player then saves without the mod,
                // the item loads as a plain vanilla item; if they save with the mod still active, the
                // Thing.ExposeData prefix re-defaults it to plasteel on the next load.
                ThingDef oldStuff = thing.Stuff;
                float oldMax = HitPointRatios.MaxHitPoints(thing, oldStuff, ArmorDefs.AppliedEffectFraction);
                thing.SetStuffDirect(null);
                float newMax = HitPointRatios.MaxHitPoints(thing, null, ArmorDefs.AppliedEffectFraction);
                HitPointRatios.PreserveRatio(thing, oldMax, newMax);
                cleaned++;
            });

            if (cleaned > 0)
            {
                // SetStuffDirect does not clear MaxHitPoints' immutable per-Thing cache. Without
                // this, the item would show the old maximum while HitPoints was already rescaled
                // against the new one.
                ArmorDefs.InvalidateStatCaches();
                Log.Message("[Stuffable Power Armor] Cleanup cleared the material setting on " + cleaned + " armor item(s).");
            }

            return cleaned;
        }

        internal static int CountConfigured()
        {
            int count = 0;
            ForEachSavedThing(thing =>
            {
                if (IsConfigured(thing))
                {
                    count++;
                }
            });

            return count;
        }

        private static bool IsConfigured(Thing thing)
        {
            return thing != null && thing.def is ThingDef def && ArmorDefs.IsSupported(def) &&
                   thing.Stuff != null;
        }

        internal static void ForEachSavedThing(Action<Thing> visitor)
        {
            var seen = new HashSet<Thing>();

            if (Find.Maps != null)
            {
                foreach (Map map in Find.Maps)
                {
                    if (map == null)
                    {
                        continue;
                    }

                    if (map.listerThings != null)
                    {
                        foreach (Thing thing in map.listerThings.AllThings)
                        {
                            Walk(thing, seen, visitor);
                        }
                    }

                    if (map.mapPawns != null)
                    {
                        foreach (Pawn pawn in map.mapPawns.AllPawns)
                        {
                            Walk(pawn, seen, visitor);
                        }
                    }
                }
            }

            if (Find.WorldPawns != null)
            {
                foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
                {
                    Walk(pawn, seen, visitor);
                }
            }

            if (Find.WorldObjects != null)
            {
                foreach (WorldObject worldObject in Find.WorldObjects.AllWorldObjects)
                {
                    if (worldObject is Caravan caravan)
                    {
                        foreach (Thing thing in caravan.AllThings)
                        {
                            Walk(thing, seen, visitor);
                        }
                    }

                    if (worldObject is IThingHolder holder)
                    {
                        WalkOwner(holder.GetDirectlyHeldThings(), seen, visitor);
                    }
                }
            }
        }

        private static void Walk(Thing thing, HashSet<Thing> seen, Action<Thing> visitor)
        {
            if (thing == null || !seen.Add(thing))
            {
                return;
            }

            visitor(thing);

            if (thing is Pawn pawn)
            {
                if (pawn.apparel != null)
                {
                    foreach (Apparel apparel in pawn.apparel.WornApparel)
                    {
                        Walk(apparel, seen, visitor);
                    }
                }

                if (pawn.equipment != null)
                {
                    foreach (ThingWithComps equipment in pawn.equipment.AllEquipmentListForReading)
                    {
                        Walk(equipment, seen, visitor);
                    }
                }

                if (pawn.inventory != null)
                {
                    WalkOwner(pawn.inventory.innerContainer, seen, visitor);
                }

                if (pawn.carryTracker != null)
                {
                    WalkOwner(pawn.carryTracker.innerContainer, seen, visitor);
                }
            }

            if (thing is Corpse corpse && corpse.InnerPawn != null)
            {
                Walk(corpse.InnerPawn, seen, visitor);
            }

            if (thing is IThingHolder holder)
            {
                WalkOwner(holder.GetDirectlyHeldThings(), seen, visitor);
            }
        }

        private static void WalkOwner(ThingOwner owner, HashSet<Thing> seen, Action<Thing> visitor)
        {
            if (owner == null)
            {
                return;
            }

            for (int i = 0; i < owner.Count; i++)
            {
                Walk(owner[i], seen, visitor);
            }
        }
    }
}
