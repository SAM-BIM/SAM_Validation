# Benchmark comparison: TAS vs OpenStudio

Independent comparison of two engine-neutral benchmark documents. Tolerance bands are
**provisional reporting buckets**, not validated thresholds. The overall gate is the worst of
the numerical, coverage, provenance and reconciliation statuses. Peak-hour bands and the two
whole-model peak-load bands are informational and are excluded from the numerical status — they are
still calculated and shown, and rows carrying such a band are marked in the Note column;
reconciliation is a separate within-document diagnostic status that also contributes to the
overall gate.

Presentation: engineering quantities, absolute differences and normalized percentages are shown to
two decimals. `comparison.csv` and `comparison-summary.json` keep full precision. `n/a` = not
reported by at least one engine; `—` = not applicable (for example, no percentage for a circular
hour). Neither marker is ever zero. Hour-of-year values and hour differences are integers.

## Summary

| Field | Value |
| --- | --- |
| Tolerance profile | default |
| Gate status (overall) | Fail |
| Numerical status | Fail |
| Coverage status | Warn |
| Provenance status | Fail |
| Reconciliation status | Warn |
| Metrics matched | 10 |
| Metrics warned | 4 |
| Metrics failed (all bands) | 3 |
| Fail rows contributing to numerical status | 1 |
| Fail rows informational (excluded) | 2 |
| Metrics not applicable | 31 |
| Required metrics compared | 17 of 28 |
| Required metrics unavailable | 11 |
| TAS schema version | 1.0.0 |
| OpenStudio schema version | 1.0.0 |

Band counts include informational rows: 2 of the 3 Fail rows are informational and excluded from the numerical status; 1 contribute to it.

## Coverage

How much of the two runs was actually compared. Required metrics are the whole-model metrics plus
the metrics of uniquely matched spaces; the metrics of one-sided, duplicated or ambiguous spaces are
not counted here because those spaces are reported under space alignment instead. An unavailable
metric is never a numerical failure, so it is recorded here — otherwise missing results could read as
a clean pass.

| Field | Value |
| --- | --- |
| Coverage status | Warn |
| Required metrics | 28 |
| Compared | 17 |
| Unavailable (not compared) | 11 |
| Unmatched spaces | 2 |
| Ambiguous (split) space names | 0 |
| Duplicated space identities | 0 |

**Coverage is INCOMPLETE**, so this comparison must not be described as a complete result:

- 11 of 28 required metrics were unavailable on at least one side (reported as N/A, never as a numerical failure).
- 2 space(s) exist on only one side.

## Provenance compatibility

**INCOMPATIBLE** — the two documents are not a valid comparison pair, so a numerical pass is not meaningful.

| Field | TAS | OpenStudio |
| --- | --- | --- |
| weatherHash | sha256:3333333333333333333333333333333333333333333333333333333333333333 | sha256:9999999999999999999999999999999999999999999999999999999999999999 |
| weatherIdentity | London-Gatwick.epw | Boston.epw |

## Tolerance profile

Provisional reporting bands (not validated thresholds). Recorded so a changed profile is never hidden behind an unchanged name.

| Setting | Value |
| --- | --- |
| Name | default |
| Warn (relative) | 5.00% |
| Fail (relative) | 15.00% |
| Peak-hour warn (h) | 1 |
| Peak-hour fail (h) | 24 |
| Default near-zero floor | 0.00 |
| Near-zero floor (kWh) | 1.00 |
| Near-zero floor (Wh) | 1000.00 |
| Near-zero floor (kW) | 0.01 |
| Near-zero floor (W) | 10.00 |
| Near-zero floor (m2) | 0.01 |
| Near-zero floor (m3) | 0.01 |
| Near-zero floor (h) | 0.50 |

## Difference definition

Normalized difference (%) = 100 × |OpenStudio − TAS| / max(|TAS|, |OpenStudio|, applicable near-zero floor).

It is unsigned and is **not** percentage change against TAS. Peak-hour metrics use the circular hour
difference and have no percentage. Bands use the stored fractions, shown here as Match < 5.00%, Warn 5.00% to < 15.00%, Fail >= 15.00% (provisional).

## Provenance

| Field | TAS | OpenStudio |
| --- | --- | --- |
| Engine kind | TAS | OpenStudio |
| Engine name | TAS | EnergyPlus |
| Engine version | 9.5.3 | 24.1.0 |
| SDK version |  | 3.10.0 |
| Route | Native-TAS | Native-OpenStudio |
| Weather | London-Gatwick.epw | Boston.epw |
| Design-day source | DDY | DDY |
| Run state | Success | Success |
| Source model | SingleBox | SingleBox |
| Source model GUID | 0123456789abcdef0123456789abcdef | 0123456789abcdef0123456789abcdef |
| SAM commit | aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa | aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa |

`Design-day source: None` means no design-day source was supplied; sizing-period runs stay disabled.

> **Input limitation:** Matching canonical model, weather and design-day metadata does not prove that the two engines received equivalent converted inputs. Per-engine input manifests are unavailable (SAM_OpenStudio#30, SAM_UI#230).

## Engine warnings and notes

These are the producers' own diagnostics; they are not comparison results and do not change a band.

### TAS

_No warnings or notes._

### OpenStudio

_No warnings or notes._

## Model metrics

> **Caveats (apply even when values pass):** whole-model peak-load semantics differ between
> engines — OpenStudio reports a coincident total, TAS a building-profile maximum — so peak-load
> comparisons are informational, not equivalence claims. `peakHeatingLoad` and `peakCoolingLoad`
> are therefore banded and shown below but **excluded from the numerical status**, exactly like the
> peak-hour metrics; promoting them into the gate needs the B4 corpus and energy-modeller review.
> Every other metric — annual energy, per-space peak loads, unmet hours, geometry — gates normally.
> The primary conditioning pairing (OpenStudio Ideal Loads — TAS thermostats + IZAMs + TBD sizing)
> is a **candidate** pending validation. A tolerance profile cannot remove these limitations.

| Metric | Unit | TAS | OpenStudio | Abs diff | Normalized difference (%) | Band | Note |
| --- | --- | --- | --- | --- | --- | --- | --- |
| consumptionHeating | kWh | 1000.00 | 1000.00 | 0.00 | 0.00 | Match |  |
| consumptionCooling | kWh | 500.00 | n/a | n/a | n/a | N/A | Unavailable (Tas only) |
| peakHeatingLoad | kW | 10.00 | 30.00 | 20.00 | 66.67 | Fail | Informational — excluded from numerical status |
| peakHeatingHour | hourOfYear | 200 | 205 | 5 | — | Warn | Circular hour difference; Informational — excluded from numerical status |
| peakCoolingLoad | kW | 8.00 | 8.80 | 0.80 | 9.09 | Warn | Informational — excluded from numerical status |
| peakCoolingHour | hourOfYear | 8759 | 0 | 1 | — | Match | Circular hour difference; Informational — excluded from numerical status |
| floorArea | m2 | 240.00 | 240.00 | 0.00 | 0.00 | Match |  |
| volume | m3 | 720.00 | 720.00 | 0.00 | 0.00 | Match |  |

## Model-total vs sum-of-spaces reconciliation

Additive quantities only; non-additive peak loads are not reconciled. Only uniquely-matched spaces are summed; unmatched or ambiguous spaces make a reconciliation incomplete.

| Engine | Quantity | Unit | Model total | Sum of spaces | Abs diff | Normalized difference (%) | Band | Used/Missing/Excluded | Complete |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| TAS | floorArea | m2 | 240.00 | 240.00 | 0.00 | 0.00 | Match | 2/0/1 | no |
| TAS | volume | m3 | 720.00 | 720.00 | 0.00 | 0.00 | Match | 2/0/1 | no |
| OpenStudio | floorArea | m2 | 240.00 | 244.00 | 4.00 | 1.64 | Match | 2/0/1 | no |
| OpenStudio | volume | m3 | 720.00 | 732.00 | 12.00 | 1.64 | Match | 2/0/1 | no |

## Space alignment

| Field | Value |
| --- | --- |
| TAS spaces | 3 |
| OpenStudio spaces | 3 |
| Matched by GUID | 2 |
| Matched by name | 0 |

**Only in TAS:** 1
- 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c

**Only in OpenStudio:** 1
- 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d

**Duplicate TAS GUIDs:** none

**Duplicate OpenStudio GUIDs:** none

**Duplicate TAS names:** none

**Duplicate OpenStudio names:** none

**Ambiguous (split) name matches:** none

## Space metrics

| Space | GUID | Match | Metric | Unit | TAS | OpenStudio | Abs diff | Normalized difference (%) | Band | Note |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Office A | 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | area | m2 | 200.00 | 200.00 | 0.00 | 0.00 | Match |  |
| Office A | 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | volume | m3 | 600.00 | 600.00 | 0.00 | 0.00 | Match |  |
| Office A | 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | heating.designLoad | W | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Office A | 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | heating.peakLoad | W | 2000.00 | 2000.00 | 0.00 | 0.00 | Match |  |
| Office A | 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | heating.peakHour | hourOfYear | 205 | 205 | 0 | — | Match | Circular hour difference; Informational — excluded from numerical status |
| Office A | 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | heating.unmetHours | h | 0.00 | 0.00 | 0.00 | 0.00 | Match |  |
| Office A | 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | cooling.designLoad | W | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Office A | 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | cooling.peakLoad | W | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Office A | 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | cooling.peakHour | hourOfYear | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Office A | 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | cooling.unmetHours | h | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Office B / Office B (OS) | 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | area | m2 | 40.00 | 44.00 | 4.00 | 9.09 | Warn |  |
| Office B / Office B (OS) | 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | volume | m3 | 120.00 | 132.00 | 12.00 | 9.09 | Warn |  |
| Office B / Office B (OS) | 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | heating.designLoad | W | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Office B / Office B (OS) | 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | heating.peakLoad | W | 2100.00 | 2600.00 | 500.00 | 19.23 | Fail |  |
| Office B / Office B (OS) | 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | heating.peakHour | hourOfYear | 205 | 240 | 35 | — | Fail | Circular hour difference; Informational — excluded from numerical status |
| Office B / Office B (OS) | 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | heating.unmetHours | h | 0.00 | 0.00 | 0.00 | 0.00 | Match |  |
| Office B / Office B (OS) | 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | cooling.designLoad | W | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Office B / Office B (OS) | 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | cooling.peakLoad | W | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Office B / Office B (OS) | 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | cooling.peakHour | hourOfYear | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Office B / Office B (OS) | 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | cooling.unmetHours | h | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Plant, level 1 | 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | area | m2 | 12.00 | n/a | n/a | n/a | N/A | Unavailable (Tas only) |
| Plant, level 1 | 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | volume | m3 | 36.00 | n/a | n/a | n/a | N/A | Unavailable (Tas only) |
| Plant, level 1 | 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | heating.designLoad | W | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Plant, level 1 | 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | heating.peakLoad | W | 2000.00 | n/a | n/a | n/a | N/A | Unavailable (Tas only) |
| Plant, level 1 | 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | heating.peakHour | hourOfYear | 205 | n/a | n/a | n/a | N/A | Unavailable (Tas only) |
| Plant, level 1 | 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | heating.unmetHours | h | 0.00 | n/a | n/a | n/a | N/A | Unavailable (Tas only) |
| Plant, level 1 | 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | cooling.designLoad | W | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Plant, level 1 | 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | cooling.peakLoad | W | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Plant, level 1 | 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | cooling.peakHour | hourOfYear | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Plant, level 1 | 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | cooling.unmetHours | h | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Atrium | 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | area | m2 | n/a | 18.00 | n/a | n/a | N/A | Unavailable (OpenStudio only) |
| Atrium | 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | volume | m3 | n/a | 54.00 | n/a | n/a | N/A | Unavailable (OpenStudio only) |
| Atrium | 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | heating.designLoad | W | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Atrium | 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | heating.peakLoad | W | n/a | 2000.00 | n/a | n/a | N/A | Unavailable (OpenStudio only) |
| Atrium | 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | heating.peakHour | hourOfYear | n/a | 205 | n/a | n/a | N/A | Unavailable (OpenStudio only) |
| Atrium | 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | heating.unmetHours | h | n/a | 0.00 | n/a | n/a | N/A | Unavailable (OpenStudio only) |
| Atrium | 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | cooling.designLoad | W | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Atrium | 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | cooling.peakLoad | W | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Atrium | 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | cooling.peakHour | hourOfYear | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |
| Atrium | 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | cooling.unmetHours | h | n/a | n/a | n/a | n/a | N/A | Unavailable (neither engine) |

