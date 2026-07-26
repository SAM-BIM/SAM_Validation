# B3 — cross-engine comparison with per-space design loads and conversion diagnostics

Third run in the series, after the
[first real cross-engine run](../2026-07-25-cross-engine-smoke/README.md) and the
[provenance-fixed repeat](../2026-07-25-cross-engine-provenance-fixed/README.md). Same source model,
same weather bytes, same workstation — now with three merged OpenStudio producer changes:

| PR | Merge | Change |
|---|---|---|
| [SAM_OpenStudio #18](https://github.com/SAM-BIM/SAM_OpenStudio/pull/18) | `67a6688` | design-day barometric pressure (avoids the EnergyPlus 31000 Pa IDD floor) |
| [SAM_OpenStudio #19](https://github.com/SAM-BIM/SAM_OpenStudio/pull/19) | `f86c4c5` | conversion diagnostics propagate on successful runs too |
| [SAM_OpenStudio #20](https://github.com/SAM-BIM/SAM_OpenStudio/pull/20) | `4306625` | per-space design loads from SQL `ZoneSizes` |

Its purpose is narrow: record what those three changes do to coverage and to the reported numbers. It
remains a **mechanical validation of the comparator and the producers**, not a validated
engine-equivalence claim.

## Run

- **Date:** 2026-07-26 (TAS `14:57:18Z`, OpenStudio `14:58:27Z`).
- **Machine:** the same licensed EDSL Tas workstation as the two previous runs.
- **Model:** `HungaryHouse-WeahterDDY.sam` — 14 spaces, carrying embedded design days.
- **Weather:** `HUN_CEN_Budapest.Met.Center.128400_TMYx.epw`.
- **Engines:** Tas `9.5.7.0` (producer duration 56.9 s); EnergyPlus `25.1.0` / OpenStudio SDK
  `3.10.0` (producer duration 22.5 s).
- **Producers built from:** `SAM_Tas@ff00497` and `SAM_OpenStudio@4306625`, both on `sow/2026-Q3`.
- **Comparator:** `SAM_Validation@a4065aad3bfaac41837ae2e784a09ec44b849544`.
- Both producers exited `0` with `state = Success`; the comparator exited `0`.

Artefacts in this folder are the verbatim outputs: the two neutral input documents and the three
generated reports.

### Two provenance deviations from the previous run, both inert

Recorded here so neither is mistaken for a result.

1. **`weather.identity` differs, `weather.hash` does not.** This run records
   `HUN_CEN_Budapest.Met.Center.128400_TMYx`; the previous two recorded `Budapest`. The producers set
   the identity from the file name only
   (`WeatherIdentity = Path.GetFileNameWithoutExtension(weatherPath)`), so the earlier runs were
   handed a copy of the same EPW named `Budapest.epw`. The **hash is unchanged on both sides**
   (`sha256:472ffb4e…`), so the weather bytes are provably identical. The provenance gate compares
   TAS identity against OpenStudio identity within a run, and both sides here got the same path, so
   it still passes. `comparison.csv` does not carry the identity, so no metric row is affected.
2. **`samCommit` is an operator claim, not a measured fact.** `--sam-commit` is passed on the command
   line and the producers cannot verify it, because local SAM assemblies are unstamped (`1.0.0.0`).
   This run records `abcd617887f6792047023820f3efd2463fc5c6b3`, as the previous run did, so the two
   are comparable — but the SAM binaries actually loaded were built **2026-07-21 22:03**, whereas
   `abcd617` was committed **2026-07-24 10:36**, so they cannot contain it. `abcd617` changed one
   file (`Query/TryGetSAMGeometries.cs`, a Grasshopper geometry-conversion path) that is not on the
   producers' route, which is why this is a labelling defect and not a result defect. The binaries
   are pinned instead by SHA-256, truncated to 16 hex characters:

   | Assembly | SHA-256 (first 16) |
   |---|---|
   | `SAM.Core.dll` | `e58a912b08d9d1c9` |
   | `SAM.Geometry.dll` | `943ab8f28019876e` |
   | `SAM.Analytical.dll` | `a9de2c2639efd467` |
   | `SAM.Architectural.dll` | `99b8aa84c45a0b02` |
   | `SAM.Weather.dll` | `6aa69438f08dc9d3` |

   These are the same binaries the previous run used — nothing in this session rebuilt `SAM\build` —
   so SAM is genuinely held constant across the two runs, which is what the comparison below needs.
   **Carried forward as a producer defect:** provenance should record a hash of the loaded assemblies
   rather than an unverifiable commit string.

## Result 1 — coverage rises 82 → 92, and nothing else moves

| Field | Previous run | This run |
|---|---|---|
| Required metrics compared | 82 of 148 | **92 of 148** |
| Required metrics unavailable | 66 | **56** |
| Matched / warned / failed | 48 / 1 / 33 | 48 / 1 / **43** |
| Coverage status | Warn | Warn |
| Provenance status | Pass | Pass |
| Overall gate | Fail | Fail |

`comparison.csv` differs from the previous run in **exactly 10 rows**, and every one of them is a
`heating.designLoad` row moving from `TasOnly` to `Both`. Every other row — all 138 of them — is
byte-identical, including all annual energy, peak load, peak hour, unmet hour and geometry values.

So the whole of the change is the newly extracted design loads. The 10 rows land as `Fail`, which is
where the `fail` count goes from 33 to 43; the `match` and `warn` counts are unchanged. **Reading
this as a regression would be wrong** — these 10 metrics were previously not compared at all, and an
uncompared metric was never a pass.

## Result 2 — the design loads disagree in *both* directions

This is the substantive finding, and it bears directly on the open sizing-factor question.

| Space | TAS (W) | OpenStudio (W) | Rel. diff | Direction |
|---|---:|---:|---:|---|
| `0872cd39` | 574.0 | 1132.1 | 49.3 % | OS higher |
| `c7df54b3` | 1065.1 | 1826.3 | 41.7 % | OS higher |
| `d8294f2a` | 564.8 | 917.5 | 38.4 % | OS higher |
| `d94f4dbe` | 549.7 | 909.1 | 39.5 % | OS higher |
| `354b8786` | 137.8 | 241.9 | 43.0 % | OS higher |
| `7ee49aac` | 366.4 | 170.6 | 53.4 % | OS lower |
| `6e7ef848` | 379.9 | 237.9 | 37.4 % | OS lower |
| `affc43b0` | 204.9 | 128.4 | 37.4 % | OS lower |
| `8d2d2a02` | 69.2 | 43.0 | 37.9 % | OS lower |
| `13a0403a` | 199.8 | 132.9 | 33.5 % | OS lower |
| **Sum** | **4111.7** | **5739.8** | — | OS/TAS = **1.396** |

Five rows up, five rows down, with the aggregate 39.6 % higher on the OpenStudio side. What this
establishes is bounded: **no single multiplicative factor maps the TAS column onto the OpenStudio
column**, so a uniform sizing margin is ruled out as the *sole* explanation of the design-load
disagreement. The producer already reports `CalcDesLoad` rather than `UserDesLoad`, so EnergyPlus's
own 1.25 sizing factor is excluded on that side.

**It does not settle whether TBD's `maxHeatingLoad` is pre- or post-factor**, and this run should not
be cited as having answered that. Two reasons:

- That question is about the TAS side alone. Answering it means comparing a TAS design load against
  its own unfactored TBD result — not against OpenStudio.
- This report documents that the two engines are **not evaluating comparable physics**: the
  design-day translation is lossy (Result 3) and the OpenStudio side drops two apertures. A constant
  multiplier applied to the TAS loads is therefore entirely consistent with ratios above 1 in some
  spaces and below 1 in others, whenever the underlying per-space loads already differ. The
  bidirectional spread cannot separate a sizing margin from that redistribution.

The sizing-factor question stays **open for Stage B**.

### An observed correlation, offered as a lead only

The split is not random with respect to TAS's own unmet hours:

| Direction | Spaces | TAS `heating.unmetHours` |
|---|---:|---|
| OS higher | 5 | 29, 65, 67, 69, 114 — all non-zero |
| OS lower | 5 | 0, 0, 0, 0 — and one outlier at **4761** |

OpenStudio sizes *above* TAS in exactly those spaces where TAS reports moderate unmet heating hours,
and *below* TAS where TAS meets the load in every hour. The single exception, `13a0403a`, is
pathological rather than borderline: 4761 unmet hours is over half the year, and its TAS design load
(199.83349609375 W) sits just 0.0082 W above its annual peak load (199.82528686523438 W), i.e. with
no meaningful headroom at all.

This is a correlation across ten rows on one fixture, and this run was not designed to test it. It is
recorded as a **lead for Stage B**, not a mechanism. Two readings are consistent with it and are not
distinguished by this data: TAS may be sizing to a design day that under-represents the load these
spaces actually see, or the two engines may be attributing load differently between adjacent spaces.

## Result 3 — 68 conversion diagnostics now reach the artefact, where there were none

The previous run recorded `warnings: []` and `notes: []` on the OpenStudio side. That was the
[#19](https://github.com/SAM-BIM/SAM_OpenStudio/pull/19) defect: diagnostics were dropped on
*successful* runs, so they only ever surfaced on failure. This run carries 68:

| Code | Count | What it reports |
|---|---:|---|
| `SAM-OS-GEO-002` | 43 | duplicate/collinear vertices removed from panels (up to 24 → 7) |
| `SAM-OS-IC-001` | 14 | pollutant generation not converted (out of scope) |
| `SAM-OS-RUN-002` | 9 | design-day and weather translation (6 warnings + 3 notes) |
| `SAM-OS-GEO-001` | 2 | **apertures skipped as degenerate** |

Three of these are material to the investigation, and all three are now self-documenting in the
committed artefact rather than resident in a session transcript:

1. **The design days are London.** The warnings name them verbatim:
   `London_TRY ANN HTG 100% CONDS DB` and `London_TRY ANN CLG 0% CONDS DB=>GRad`, on a model whose
   annual weather is Budapest. This confirms in the artefact what was previously only known from
   inspecting the `.sam`: **HungaryHouse is a known-mismatch forensic fixture, not a sizing
   baseline.**
2. **The design-day translation is lossy, in named ways.** The warnings state that the hourly
   humidity profile "was approximated as a constant dew point" (−5.2 °C for heating, 12.6 °C for
   cooling), and that the hourly solar profile "was approximated by the ASHRAEClearSky model" with
   **clearness 0.0 for the heating day** and 1.0 for cooling. A heating design day with clearness 0.0
   carries zero solar for all 24 hours. The `#18` pressure fix is also visible, reporting that the
   elevation-based standard `101025 Pa` is written "instead of leaving the EnergyPlus 31000 Pa
   default". This is precisely the hourly → parametric collapse that the design-day audit exists to
   quantify, and it is now on the record from the producer's own output.
3. **Two apertures were skipped entirely.** `SAM-OS-GEO-001` reports one source aperture
   (`24df7efc-1805-40ef-a7e7-fc8cd33c7225`) collapsing to 2 sides under the 0.01 m proximity rule on
   two surfaces, and being dropped. The EnergyPlus model therefore has two fewer openings than the
   SAM model. Both are on `SIM_INT_SLD` (internal) surfaces, so the likely effect is on inter-space
   heat flow rather than solar gain — which makes it a candidate for the per-space load
   redistribution in Result 2. **Not yet tested**, and the gbXML/TAS side has not been checked for
   the same drop.

The 43 `GEO-002` vertex removals are worth a separate look: reducing a 24-vertex panel to 7 is a
large simplification, and it is applied silently to the OpenStudio side only.

## Carried forward

The 56 unavailable required metrics (all 14 spaces matched, so none of this is an alignment problem):

| Count | Metrics | Availability | Meaning |
|---:|---|---|---|
| 14 | `cooling.designLoad`, all 14 spaces | TasOnly | `ZoneSizes` has **no `Cooling` rows at all** for this model |
| 4 | `heating.designLoad`, 4 spaces | TasOnly | zones EnergyPlus did not size |
| 24 | `cooling.peakLoad` + `cooling.peakHour`, 12 spaces | OpenStudioOnly | TAS reports no cooling result where OpenStudio does |
| 8 | `heating`/`cooling` `peakLoad` + `peakHour`, 2 spaces | Neither | neither engine reports these two spaces |
| 4 | `heating`/`cooling` `unmetHours`, 2 spaces | TasOnly | |
| 2 | `heating.peakLoad` + `heating.peakHour`, 1 space | OpenStudioOnly | |

- **Design loads fell from 28 unavailable to 18.** The remaining 18 are all one-sided from TAS: the
  complete absence of `Cooling` rows in `ZoneSizes` is now the single largest coverage gap, and it is
  a producer/route question, not a comparator one.
- `ZoneSizes.PeakTemp` remains **unmapped to any SAM parameter** and is carried verbatim on
  `OpenStudioZoneSizingResult`. Every row carried `-3.20000004768372`, bit-identical to the design
  day's *outdoor* maximum dry bulb, although EnergyPlus documents it as a zone value. Unsettled.
- `UserDesLoad` is preserved on the result set and may become its own metric
  (`sizingCapacity` / `userDesignLoad`).
- The `Site coordinates taken from …` `Information` diagnostic in `Convert/ToOpenStudio/Weather.cs`
  still carries the `RunCliFailed` code, so it does **not** appear in the 68 above despite being
  material — site elevation feeds the design-day standard pressure that `#18` now writes. Re-coding
  it changes conversion output, so it is a separate PR.
- The `eplusout.err` elevation warning (`Elevation difference=[416.00] percent, [104.00] meters`) is
  **not fixed**, deliberately: which site is authoritative for each engine has to be established
  first.
- TAS still reports `peakCoolingHour = 0` with `available: true` for a measured-zero cooling load.

## Status

- **Design-load coverage: improved and verified.** 10 of 28 recovered, with proof that no other
  metric moved.
- **Diagnostic visibility: closed.** The producer's own warnings now name the London design days, the
  humidity/solar approximations and the skipped apertures, inside the committed artefact.
- **Engine equivalence: open.** No single multiplicative margin reconciles the ten design-load rows,
  but the TAS pre/post-factor question is untouched by this run and stays open — it needs a TAS design
  load compared against its own unfactored TBD result. Next: the `design-day-audit-v1` neutral
  contract, then the TAS (TBD + TSD) and OpenStudio (`.eio`) design-day producers.
