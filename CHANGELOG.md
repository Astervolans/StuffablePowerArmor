# Changelog

All notable changes to this project are documented here.

## 1.0.2

- The native material effect multiplier now reflects the configured
  material-effect percentage (20%–100%) instead of being hardcoded at 100%.
- Preserve the current/maximum hit-point ratio when the material-effect
  percentage changes between saves, or when the cleanup button removes the
  material.
- Save the applied material-effect fraction as `<SPA_EffectFraction>` inside
  `<game>`; read it on load and rescale hit points when the setting changed.
- Clear the immutable MaxHitPoints cache after cleanup so the displayed maximum
  matches the rescaled current hit points.
- Settings continue to apply on restart only; no stat-cache manipulation from
  the settings UI.

## 1.0.1

- Fixed the settings slider causing `NullReferenceException` spam
  (`StatWorker.DeleteStatCache()` nulls the caches in RimWorld 1.7).
- Restored the native `StuffEffectMultiplierArmor` and insulation multiplier
  stats (previously shown as 0%).
- Added Combat Extended compatibility: material multipliers are added before
  CE's apparel autopatcher, the vanilla 0–2 armor clamp is not applied to CE
  millimetre values, and material differences are scaled to CE armor values.

## 1.0.0

- Initial release: 18 stuffable imperial/mechanitor power-armor pieces,
  20-plasteel fixed cost, 20%–100% material effect, configurable material
  categories, mid-save add/remove support, English and Simplified Chinese.
