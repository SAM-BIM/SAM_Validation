# B3 — cross-engine comparison repeated on the fixed OpenStudio producer

This repeats the [first real cross-engine run](../2026-07-25-cross-engine-smoke/README.md) — same
source model, same weather file, same workstation — now that the OpenStudio producer's design-day
provenance defect is fixed ([SAM_OpenStudio #17](https://github.com/SAM-BIM/SAM_OpenStudio/pull/17),
merged to `sow/2026-Q3` as `1ae8637`).

Its purpose is narrow: establish whether the provenance gate now accepts the pair, and whether
correcting the declared design-day basis moves any number. It remains a **mechanical validation of
the comparator and the producers**, not a validated engine-equivalence claim.

## Run

- **Date:** 2026-07-25 (TAS `14:35:23Z`, OpenStudio `14:36:36Z`).
- **Machine:** the same licensed EDSL Tas workstation as the previous run.
- **Model:** `HungaryHouse-WeahterDDY.sam` — 14 spaces, carrying embedded design days.
- **Weather:** `HUN_CEN_Budapest.Met.Center.128400_TMYx.epw`.
- **Engines:** Tas `9.5.7.0` (producer duration 60.8 s); EnergyPlus `25.1.0` / OpenStudio SDK
  `3.10.0` (producer duration 21.5 s).
- **Producers built from:** `SAM_Tas@ff00497` and `SAM_OpenStudio@1ae8637`, both on `sow/2026-Q3`.
- **Comparator:** `SAM_Validation@c3eec2cafaf42267a506579a9efd078cb7fcc473` (the B3 squash merge).
- Both producers exited `0` with `state = Success`; the comparator exited `0`.

Artefacts in this folder are the verbatim outputs: the two neutral input documents and the three
generated reports.

## Result 1 — the provenance gate now passes

| Field | Previous run | This run |
|---|---|---|
| TAS `designDaySource` | `EmbeddedModel` | `EmbeddedModel` |
| OpenStudio `designDaySource` | `None` (hardcoded — the defect) | **`EmbeddedModel`** (derived from the route) |
| **Provenance status** | **Fail** | **Pass** |

The two documents also agree on `canonicalModelHash`
(`sha256:07a3ef15…`), `sourceModelGuid` and the weather hash, so the comparator now confirms in full
that the pair is a valid comparison: same canonical model, same weather, same design-day basis, one
engine and route on each side.

## Result 2 — not one simulated number changed

The complete generated `comparison.csv` is **byte-identical** to the previous run — not merely equal on
the value columns. The two files share one Git blob:

```text
git hash-object ../2026-07-25-cross-engine-smoke/comparison.csv comparison.csv
  840655621cd7c9e95d24233687c229f50c8f3cca
  840655621cd7c9e95d24233687c229f50c8f3cca
```

Every metric value, absolute and relative difference, band, note and tolerance column therefore agrees
exactly. `comparison.md` and `comparison-summary.json` do differ, as they must: they carry the
provenance block, which is precisely what changed.

Two things follow, and they matter more than the passing gate:

1. **The defect was exactly what it was diagnosed as** — a *reporting* defect. The OpenStudio route
   was already building design days from the embedded model; only the declared `designDaySource` was
   wrong. Had the fix altered the sizing basis, the loads would have moved.
2. **The pipeline is reproducible.** Two independent full runs of both engines, hours apart, produced
   identical metrics down to the last binary64 digit — across TAS COM, a gbXML round trip,
   EnergyPlus, and both producers' extraction paths.

## Result 3 — the candidate conditioning pairing is still not equivalent

The overall gate is still `Fail`, now on the numerical status alone:

| Status | Value |
|---|---|
| Numerical | **Fail** |
| Coverage | Warn (82 of 148 required metrics comparable; 66 unavailable) |
| Provenance | Pass |
| Reconciliation | Pass |
| **Overall** | **Fail** |

This is the point of the exercise. Previously the `Fail` was confounded: a provenance mismatch made the
comparison invalid regardless of the numbers, so the numerical disagreement could be set aside as
"probably the declared design-day basis". That specific objection is answered — **the neutral inputs are
provably identical, the declared design-day basis now agrees, and the numbers still disagree.**

What this does *not* establish is covered under
[what remains open](#what-this-run-does-not-establish-about-design-days) below.

| Metric | TAS | OpenStudio | Rel. diff | Band | Gates? |
|---|---:|---:|---:|---|---|
| Annual heating (kWh) | 4331.96 | 1751.18 | **59.6 %** | Fail | **yes** |
| Annual cooling (kWh) | 0 | 5.25 | 100 % | Fail | **yes** |
| Peak heating load (kW) | 3.934 | 5.721 | 31.2 % | Fail | no — designated informational |
| Peak cooling load (kW) | 0 | 0.040 | 100 % | Fail | no — designated informational |
| Floor area | 167.95195245742798 | 167.95195207345205 | 2.3e-9 (= 2.3e-7 %) | Match | yes |

Among comparable per-space heating peak-load rows, relative differences span approximately **12–100 %**
(11 comparable rows). The previously quoted 29 % and 52 % values are selected examples, not the complete
range. The cooling rows compare a TAS *measured zero* against a
near-zero OpenStudio value, so their 100 % figures are not meaningful magnitudes. The two whole-model
peak loads are banded and shown but excluded from the numerical status
(TOLERANCES.md → Informational metrics), so they are not what fails this comparison — annual energy,
per-space peak loads and unmet hours are.

**Conclusion:** the D10 candidate pairing (OpenStudio Ideal Loads ↔ TAS thermostats + IZAMs + TBD
sizing) is **not** validated. It must stay labelled a *candidate* and be investigated against the B4
corpus, metric by metric — not by adjusting tolerances.

### What this run does *not* establish about design days

It would be wrong to read this run as eliminating design-day handling as a cause of the load gap. Two
things were shown: the two runs declare the same design-day *category*, and the provenance fix changed
no result. Neither is evidence that the engines built **equivalent design days** after translation.

- `canonicalModelHash` describes the neutral loaded SAM model and is computed *before any
  engine-specific modification* ([METHODOLOGY.md](../../METHODOLOGY.md) → The two model hashes). It
  says nothing about what either engine did with the embedded design days afterwards.
- `designDaySource` records only the coarse category `Ddy` / `EmbeddedModel` / `None`. Both sides
  reporting `EmbeddedModel` means both drew on the model's own design days, not that they instantiated
  the same ones.
- Identical metrics before and after a provenance-only fix show that *the fix* did not alter results —
  by construction, since it only changed a reported field.

**Differing design-day translation therefore remains an open hypothesis**, and a leading one: TAS sizes
through TBD while OpenStudio emits `SizingPeriod:DesignDay` objects into the EnergyPlus model, and a
differing sizing basis moves heating loads. Closing it needs engine-level evidence — compare the design
days actually instantiated on each side (the TBD sizing inputs against the generated OSM/IDF
`SizingPeriod:DesignDay` objects), which is outside this run's provenance-only scope and belongs with
the B4 investigation.

## Carried forward

The 66 unavailable required metrics break down as follows (all 14 spaces matched, so none of this is
an alignment problem):

| Count | Metrics | Availability | Meaning |
|---:|---|---|---|
| 28 | `heating.designLoad` + `cooling.designLoad`, all 14 spaces | TasOnly | OpenStudio emits no per-space design load on the native route |
| 24 | `cooling.peakLoad` + `cooling.peakHour`, 12 spaces | OpenStudioOnly | TAS reports no cooling result where OpenStudio does |
| 8 | `heating`/`cooling` `peakLoad` + `peakHour`, 2 spaces | Neither | neither engine reports these two spaces |
| 4 | `heating`/`cooling` `unmetHours`, 2 spaces | TasOnly | |
| 2 | `heating.peakLoad` + `heating.peakHour`, 1 space | OpenStudioOnly | |

- OpenStudio's missing per-space design loads account for **28 of the 66** unavailable required metrics
  and are the largest single source of incomplete coverage (a documented route limitation).
- TAS still reports `peakCoolingHour = 0` with `available: true` for a measured-zero cooling load; a
  peak *hour* for a zero load carries no information (a B2 producer-semantics question).
- The engines still disagree about which spaces are conditioned, and the table above quantifies it: 24
  metrics are unavailable because TAS reports no cooling result where OpenStudio does, and one space
  reports `heating.peakLoad` as available `0` from OpenStudio while TAS reports it unavailable. This is
  a concrete, bounded lead for the B4 investigation.
- **Design-day translation is an open hypothesis** for the load gap (see above). It needs engine-level
  evidence comparing the design days each engine actually instantiated, not provenance fields.

## Status

- **Provenance gate: closed.** The defect it caught is fixed and the fix is verified end to end.
- **Comparator: validated twice on real two-engine data**, now including a reproducibility check.
- **Engine equivalence: open.** The declared-provenance objection is answered, but design-day
  *translation* remains untested, so the cause of the load gap is still unidentified. Next: the B4
  corpus (`SAM_Samples`) and energy-modeller review.
