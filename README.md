# Stuffable Power Armor

English | [简体中文](README.zh-CN.md)

RimWorld 1.7 mod that makes the imperial / mechanitor power-armor sets **stuffable**: you
choose the material when crafting, and the material changes the finished item's stats. The
vanilla plasteel build is preserved exactly.

## Supported items

| Set | Armor | Helmet |
|---|---|---|
| Recon (斥候) | `Apparel_ArmorRecon` | `Apparel_ArmorHelmetRecon` |
| Marine (海军) | `Apparel_PowerArmor` | `Apparel_PowerArmorHelmet` |
| Cataphract / Knight (骑士) | `Apparel_ArmorCataphract` | `Apparel_ArmorHelmetCataphract` |
| Prestige recon (圣装斥候) | `Apparel_ArmorReconPrestige` | `Apparel_ArmorHelmetReconPrestige` |
| Prestige marine (圣装海军) | `Apparel_ArmorMarinePrestige` | `Apparel_ArmorMarineHelmetPrestige` |
| Prestige cataphract (圣装骑士) | `Apparel_ArmorCataphractPrestige` | `Apparel_ArmorHelmetCataphractPrestige` |
| Grenadier (掷弹) | `Apparel_ArmorMarineGrenadier` | — |
| Locust (飞蝗) | `Apparel_ArmorLocust` | — |
| Phoenix (凤凰) | `Apparel_ArmorCataphractPhoenix` | — |
| Mechlord (机械领主) | `Apparel_MechlordSuit` | `Apparel_ArmorHelmetMechlordHelmet` |
| Mechcommander (机械指挥官) | — | `Apparel_ArmorHelmetMechCommander` |

## Features

1. **Material instead of most plasteel.** Each recipe keeps **20 plasteel** as the fixed
   structural requirement and replaces the rest of the plasteel with `original - 20` of the
   chosen material. The other ingredients (components, uranium, gold, chemfuel, special chips)
   are unchanged. Choosing plasteel therefore reproduces the vanilla cost and market value.
2. **Plasteel is exactly vanilla.** At plasteel the mod's stat layer collapses to the un-stuffed
   value, so armor rating, insulation, hit points, work amount, mass, flammability, beauty and
   market value match the unmodded item.
3. **Adjustable material effect.** Mod settings: 20%–100% (default 100%). Material differences
   are measured relative to plasteel, then scaled: at 50%, only half of the difference applies.
   Affected: sharp/blunt/heat armor, hot/cold insulation, max hit points, work to make, mass.
   Material beauty and market value remain native material effects (so gold trims still look
   expensive and valuable). The native **material effect multiplier** stat reads the applied
   percentage (20% / 100% in the item stat card).
   **Setting changes are applied on restart.** The settings window does not recalculate live
   item stats; close the game and start it again after changing the percentage or categories.
   This is deliberate: RimWorld 1.7's `DeleteStatCache()` nulls the stat-cache dictionaries, so
   touching them from a slider causes `NullReferenceException` spam.
4. **Hit-point ratios are preserved.** Max hit points change with the material and with the
   percentage setting. When the setting changes between saves (or when cleanup removes the
   material), every supported item's current hit points are rescaled to the same
   damaged/healthy ratio instead of keeping the old absolute number:
   - a 333-max uranium grenadier suit at 303 HP (91%) becomes ~309/340 after cleanup, still
     91%;
   - after changing 20% -> 100%, 303/333 becomes the same ratio against the new maximum.
   The applied percentage is written into the save as `<SPA_EffectFraction>` inside `<game>`
   and checked on load. Saves from older versions without that node are left unchanged (save
   once with the new version to record it).
5. **Configurable material categories.** Metals are always allowed (default). Wood, cloth &
   leather, and stone blocks can be enabled separately in the mod settings. Category changes
   take effect on the next game start.
6. **Mid-save installation.** Existing supported armor without a material record loads as
   plasteel (`defaultStuff` plus a `Thing.ExposeData` prefix); no red "has no stuff" errors.
7. **Mid-save removal.** The only thing written to item saves is a standard `<stuff>` reference
   to a **vanilla** material (e.g. `Steel`, `WoodLog`); the mod adds no custom ThingDefs or
   comps. The save-level `<SPA_EffectFraction>` node is a plain float. The mod settings page has
   a **Clear material settings** button:
   - it scans maps, pawns (worn/inventory), caravans, world pawns and thing holders;
   - clears the material reference on every supported item;
   - re-scans and, when nothing remains, shows **“可以移除mod，请直接保存并退出。”**
     (English: “You can remove the mod. Please save and exit directly.”)
   - Do this in-game, then save and exit.
8. **English + Chinese (Simplified).** About text and all mod settings/dialog strings are
   localized.

## FAQ

**Q: Does this mod support adding to or removing from an existing save?**

A: Yes. It supports adding to and removing from an existing save. Existing armor is treated as
plasteel by default. To remove it safely, click the **“Clear material settings on all supported
armor”** button in the mod settings, save, and then remove the mod.

**Q: Does this mod support Combat Extended?**

A: Yes, it supports Combat Extended (CE is not required).

**Q: How compatible is this mod?**

A: In principle it should only conflict with mods that modify the vanilla armor recipes or
stats. I have not found any obvious issues in my usual mod setup; if you encounter an unexpected
conflict, please report it.

## Install

- Requires **RimWorld 1.7** and **Harmony** (packageId `brrainz.harmony`).
- Copy the `StuffablePowerArmor` folder (or extract the zip) into
  `<RimWorld>/Mods/`.
- Enable **Stuffable Power Armor** after Harmony and the DLCs.
- Royalty and Biotech parts are patched only when those DLCs are active; Core items work
  without them.

## Compatibility / notes

- **Material effect multiplier tracks the setting (20%–100%).** The supported armor inherits
  the vanilla `StuffEffectMultiplierArmor` / insulation multiplier stats; unlike 1.0.0 they are
  no longer stuck at 0%, and unlike 1.0.1 they are not hardcoded at 100%. The value is written
  when the game starts and therefore changes after a restart.
- **Combat Extended:** a CE-only XML patch (`Patches/CombatExtended.xml`) adds the multiplier
  stats before CE's `ApparelAutoPatcher` runs, preventing CE from replacing the armor ratings
  with its low-tier curve floor (the reported “2 mm” bug). The stat layer detects CE, keeps
  CE's millimetre armor values, removes the vanilla 0–2 clamp, and scales material differences
  to the CE armor magnitude. Plasteel still equals CE's unmodified armor value. Tested with
  Combat Extended 16.7.4.0 on the 1.7 harness; older/newer CE versions use the same
  `StuffEffectMultiplierArmor` and `ArmorRating_Sharp/Blunt` APIs.
- Uses Harmony to adjust the material contribution in `StatWorker.GetValue`. A stat patch
  exception is caught and logged once with context; it never aborts a map or world generation.
- The mod does not alter the vanilla item defs on disk; it only patches them in memory.
- Flammability is deliberately kept at the vanilla value for supported armor (the native
  material factor would otherwise make plasteel suits fire-proof or stone suits burn); this
  avoids a 1.7 world-generation edge case.
- Stone blocks have no native armor power, so stone-stuffed armor gives up most/all additional
  armor protection; this is intended.

## Source / build

The prebuilt `Assemblies/StuffablePowerArmor.dll` is ready to use. To build from source,
provide the RimWorld 1.7 reference assemblies (the game's `Assembly-CSharp.dll`,
`UnityEngine.CoreModule.dll`/`UnityStubs.dll` equivalent, and Harmony's `0Harmony.dll`) in one
folder and pass it as `RimWorldRefsDir`:

```
dotnet build Source\StuffablePowerArmor.csproj -c Release /p:RimWorldRefsDir=C:\path\to\refs
```

The workspace default (`..\..\tools\refs`, used by `tools\build-mod.ps1`) is only for the
development copy of this project.

## Version

1.0.2 — packageId `locals.stuffablepowerarmor`
