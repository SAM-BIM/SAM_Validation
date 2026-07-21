# Benchmark glossary

## Canonicalization

The deterministic process that converts a SAM model into the representation hashed by `canonicalModelHash`. Its rules are identified by `canonicalizationVersion` so a changed algorithm cannot masquerade as a changed model.

## Canonical unit

The unit required by the benchmark contract for a metric. Producers convert values at emission time and include the unit token on every metric; consumers never infer a unit from a field name.

## Conditioning pairing

The two engine configurations intended to represent comparable space conditioning. The B0 candidate is OpenStudio Ideal Loads versus Tas thermostats, IZAMs, and TBD sizing. It remains provisional until the B2 `SingleBox` checkpoint and energy-modeller review.

## Engine

The independent simulation implementation that produces physical results. In this programme the engine families are OpenStudio/EnergyPlus and EDSL Tas. Engine identity and version belong in provenance.

## Gate status

The overall comparator outcome derived from contract validity, coverage diagnostics, and metric bands. A gate status is not a claim that one engine is correct. `N/A` metrics do not create a numerical failure.

## Metric availability

Whether a producer obtained a real metric from its route. Available means `available: true` with a non-null value, including a measured zero. Unavailable means `available: false` with `value: null`.

## Provenance

The evidence describing what was run and how: model and weather identities and hashes, canonicalization version, code revisions, engine versions, route, design-day source, run time, state, warnings, and notes.

## Route

The complete model-translation and simulation path used to produce a benchmark document. A route is distinct from an engine because one engine may be exercised through native SAM translation or a shared interchange model.

## Tolerance band

A configurable reporting classification based on the difference between two available, unit-compatible values. B0 defines provisional pass/warn/fail bands; it does not validate them as acceptance thresholds.

## Whole-model reconciliation

A diagnostic comparison between a reported model total and an aggregation of matched space results where the metric semantics permit aggregation. A reconciliation result is kept separate from the direct cross-engine comparison.
