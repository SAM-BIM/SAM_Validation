# Benchmark glossary

## Canonicalization

The deterministic process that converts a SAM model into the representation hashed by `canonicalModelHash`. Its rules are identified by `canonicalizationVersion` so a changed algorithm cannot masquerade as a changed model.

## Canonical unit

The unit required by the benchmark contract for a metric. Producers convert values at emission time and include the unit token on every metric; consumers never infer a unit from a field name.

## Collapse loss

What an hourly → parametric design-day translation discards. Measured on one engine's side alone, by summarising an `ObservedHourly` series into parametric terms and comparing that against the `ProcessedParametric` definition the engine echoed. It needs no reconstruction, so it is the strongest design-day evidence available. See [DESIGN-DAY-AUDIT.md](DESIGN-DAY-AUDIT.md).

## Conditioning pairing

The two engine configurations intended to represent comparable space conditioning. The B0 candidate is OpenStudio Ideal Loads versus Tas thermostats, IZAMs, and TBD sizing. It remains provisional until the B2 `SingleBox` checkpoint and energy-modeller review.

## Derived hourly

A 24-hour design-day series **reconstructed** by a producer from an engine's `ProcessedParametric` definition using that engine's documented design-day algorithms. It is never the engine's own output, and describing one as "reported by" the engine is prohibited. Requires a declared algorithm and version. See [DESIGN-DAY-AUDIT.md](DESIGN-DAY-AUDIT.md).

## Engine

The independent simulation implementation that produces physical results. In this programme the engine families are OpenStudio/EnergyPlus and EDSL Tas. Engine identity and version belong in provenance.

## Gate status

The overall comparator outcome derived from contract validity, coverage diagnostics, and metric bands. A gate status is not a claim that one engine is correct. `N/A` metrics do not create a numerical failure.

## Metric availability

Whether a producer obtained a real metric from its route. Available means `available: true` with a non-null value, including a measured zero. Unavailable means `available: false` with `value: null`.

## Observed hourly

A 24-hour design-day series an engine actually consumed, as authored in the model, with nothing fitted or modelled. In this programme TAS supplies it from TBD design days. See [DESIGN-DAY-AUDIT.md](DESIGN-DAY-AUDIT.md).

## Processed parametric

The parametric design-day definition an engine accepted and echoed back after its own input processing — maximum dry bulb, daily range, humidity condition, solar model and clearness, and so on. It is not a series, and must never be compared hour-by-hour against one. EnergyPlus supplies it from `eplusout.eio`. See [DESIGN-DAY-AUDIT.md](DESIGN-DAY-AUDIT.md).

## Provenance

The evidence describing what was run and how: model and weather identities and hashes, canonicalization version, code revisions, engine versions, route, design-day source, run time, state, warnings, and notes.

## Reported load basis

Whether a sizing load a producer reports is stated before or after that engine's sizing factor (`PreSizingFactor`, `PostSizingFactor` or `Unknown`). It is settled per engine, by comparing a reported load against that engine's own unfactored result — never by comparing two engines with each other, because a cross-engine ratio cannot separate a sizing margin from a physical difference. See [DESIGN-DAY-AUDIT.md](DESIGN-DAY-AUDIT.md).

## Route

The complete model-translation and simulation path used to produce a benchmark document. A route is distinct from an engine because one engine may be exercised through native SAM translation or a shared interchange model.

## Tolerance band

A configurable reporting classification based on the difference between two available, unit-compatible values. B0 defines provisional pass/warn/fail bands; it does not validate them as acceptance thresholds.

## Value basis

How an audited number came to exist, declared on every audit value: `Observed` (read as authored), `EngineEchoed` (the engine's own echo of what it accepted) or `Reconstructed` (computed by the producer from echoed inputs). It is recorded independently of the representation the value sits in, so an over-claimed value is a validation error rather than a matter of wording. See [DESIGN-DAY-AUDIT.md](DESIGN-DAY-AUDIT.md).

## Whole-model reconciliation

A diagnostic comparison between a reported model total and an aggregation of matched space results where the metric semantics permit aggregation. A reconciliation result is kept separate from the direct cross-engine comparison.
