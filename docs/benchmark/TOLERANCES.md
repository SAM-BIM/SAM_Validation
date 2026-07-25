# Benchmark tolerances and reporting bands

## Status

The B0 bands are configurable reporting aids, **not validated acceptance thresholds**. They support early corpus review and must not be presented as evidence that two engines are equivalent.

The default relative bands are:

| Band | Absolute relative difference |
|---|---|
| Pass | Less than 5% |
| Warn | At least 5% and less than 15% |
| Fail | At least 15% |

Reports must name the tolerance profile and include its actual values. A changed profile changes the report classification and must not be hidden.

## Difference calculation

For two available, finite, unit-compatible scalar values `tas` and `openStudio`:

```text
signedDifference   = openStudio - tas
absoluteDifference = abs(signedDifference)
scale              = max(abs(tas), abs(openStudio), nearZeroFloor(metric))
relativeDifference = absoluteDifference / scale
```

The magnitude is symmetric: swapping the two engines does not change the band. The signed difference is retained to show direction. `nearZeroFloor(metric)` is a positive, canonical-unit value configured per metric or metric family; B0 does not claim scientifically validated floor values.

The comparator must report the absolute and relative differences used for classification. It must not round inputs before calculating a band; rounding is presentation-only.

## Near-zero rule

The floor prevents tiny denominators from turning negligible absolute differences into extreme percentages. When both magnitudes are below the configured floor, the formula is equivalent to comparing the absolute difference with 5% and 15% of that floor.

Examples for a hypothetical load floor of `100 W`:

| Tas | OpenStudio | Difference | Effective relative difference | Band |
|---:|---:|---:|---:|---|
| `0 W` | `0 W` | `0 W` | `0%` | Pass |
| `0 W` | `4 W` | `4 W` | `4%` | Pass |
| `0 W` | `10 W` | `10 W` | `10%` | Warn |
| `0 W` | `20 W` | `20 W` | `20%` | Fail |

The example explains the rule; `100 W` is not a programme default. Each production profile requires energy-modeller review of its floors.

## Unavailable and invalid values

If either metric has `available: false` and `value: null`, the comparison band is `N/A`. `N/A` never becomes Pass, Warn, Fail, or a numeric zero and never causes a numerical gate failure.

The following are contract errors rather than `N/A` comparisons:

- the `available`/`value` invariant is broken;
- units differ;
- a value is non-finite;
- a magnitude is negative without a documented conversion; or
- an hour-of-year value is outside `0..8759` or non-integral.

Coverage diagnostics count unavailable required metrics and unmatched spaces separately. A numerically passing report with poor coverage must not be described as complete.

Concretely, the coverage status is `Warn` when any required metric is unavailable, when any space is unmatched, duplicated or ambiguous, or when nothing was comparable at all. Required metrics are the whole-model metrics plus the metrics of uniquely matched spaces; the metrics of one-sided or ambiguous spaces are not counted again, because those spaces are already reported by the unmatched-space diagnostics. Because `N/A` cannot affect the numerical status either, this is what stops a run that produced few results from presenting as a clean overall pass.

## Peak-hour comparison

Peak hours are circular over a normal 8760-hour year. For valid hours `a` and `b`:

```text
directDifference = abs(a - b)
circularDifference = min(directDifference, 8760 - directDifference)
```

Thus hours `8759` and `0` differ by one hour, not 8759 hours. Percentage bands do not apply to `hourOfYear`; the tolerance profile must provide absolute warn/fail thresholds in hours, and both thresholds are applied so a large gap is not reported identically to a small one. Until those thresholds are reviewed, peak-hour differences remain informational: `hourOfYear` metrics are excluded from the numerical status, so a reported peak-hour Fail band cannot produce a numerical Fail. The exclusion — not a suppressed band — is what keeps them informational.

Leap-year or sub-hourly runs are outside v1. A producer must not force such indices into the `0..8759` contract without an explicit normalization policy and schema revision.

## Whole-model reconciliation

Reconciliation is a within-document diagnostic and remains separate from the Tas-versus-OpenStudio comparison.

| Metric | Reconciliation rule |
|---|---|
| Floor area | Compare available model `floorArea` with the sum of available, uniquely matched space `area` values |
| Volume | Compare available model `volume` with the sum of available, uniquely matched space `volume` values |
| Annual heating/cooling | No v1 reconciliation because per-space annual energy is excluded |
| Peak heating/cooling load | Do not require equality with the sum of per-space peaks; peaks may occur at different hours. The sum may be shown only as a labelled non-coincident diagnostic |
| Peak hour | Not additive; no reconciliation |
| Design load | No model-level design-load metric exists in v1 |
| Unmet hours | Do not sum across spaces as a model total; simultaneous unmet intervals would be double-counted |

Reconciliation uses only available values. The report states the number and identity of spaces included and excluded. Missing or ambiguous spaces prevent a reconciliation from being labelled complete.

## Gate aggregation

The comparator keeps these concerns distinct:

1. schema and unit validity;
2. provenance compatibility;
3. space-alignment and metric coverage;
4. numerical bands; and
5. reconciliation diagnostics.

For numerical bands, overall severity is the worst comparable metric: Fail over Warn over Pass. `N/A` does not affect that ordering. Contract errors prevent a valid numerical gate. Coverage and reconciliation statuses are always reported alongside the numerical status so missing data cannot improve the apparent result.

The whole-model peak-load caveat and candidate conditioning pairing remain visible even when their numbers fall in Pass. Tolerance profiles cannot remove those methodological limitations.

## Validation path

The 5%/15% bands and all near-zero and peak-hour floors remain provisional through B0. They may be promoted or revised only after the benchmark corpus has been run across both engines and an energy modeller has reviewed distributions, outliers, route effects, and metric semantics. Any revision must be versioned and recorded with the report.
