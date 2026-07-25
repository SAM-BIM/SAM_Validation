# B3 — first real cross-engine comparison (same model, same weather)

This records the first end-to-end run of the **B3 comparator** over two real engine documents
produced from the **same source model and the same weather file** — the numeric pairing that the B2
/ D10 checkpoint deferred (it had to use a geographically mismatched EPW).

It is a **mechanical validation of the comparator**, not a validated engine-equivalence claim. The
tolerance bands remain provisional reporting buckets.

## Run

- **Date:** 2026-07-25
- **Machine:** licensed EDSL Tas install + OpenStudio/EnergyPlus on the same workstation.
- **Model:** `HungaryHouse-WeahterDDY.sam` — real Revit-derived SAM `AnalyticalModel`, 14 spaces
  (conditioned rooms + unconditioned voids), carrying embedded design days.
- **Weather:** `HUN_CEN_Budapest.Met.Center.128400_TMYx.epw` — geographically correct for this model
  (the D10 checkpoint used a Boston EPW purely to exercise the pipeline).
- **Engines:** Tas `9.5.7.0`; EnergyPlus `25.1.0`.
- **Commands** (both producers, then the comparator):

  ```
  benchmark-tas        --model HungaryHouse.sam --weather Budapest.epw --tbd hungary-budapest.tbd \
                       --out benchmark-TAS.json --sam-commit <sha> --runner-commit <sha>
  benchmark-openstudio --model HungaryHouse.sam --weather Budapest.epw \
                       --out benchmark-OpenStudio.json --work <ABSOLUTE run dir> \
                       --sam-commit <sha> --runner-commit <sha>
  benchmark-compare    --tas benchmark-TAS.json --openstudio benchmark-OpenStudio.json \
                       --out report --tolerance-profile default
  ```

- **Result:** both producers exited `0` with `state = Success`; the comparator exited `0` and wrote
  all three reports. Durations: TAS ≈ 58 s, OpenStudio ≈ 21 s.

## What the comparator got right (its acceptance evidence)

| Check | Outcome |
|---|---|
| Space alignment | **14/14 matched by GUID**, 0 by name, none one-sided → coverage `Pass` |
| Geometry agreement | floor area `167.95195245742798` vs `167.95195207345205` m² → rel. diff **2.3e-7**, `Match` |
| Reconciliation | model total vs sum of matched spaces **complete (14/0/0)** and `Match` for both engines, both quantities |
| Circular peak hour | TAS hour `0` vs OpenStudio `7974` correctly reported as **786 h** apart (wrapped), not 7974 |
| Unavailable ⇒ N/A | 66 metrics N/A, none contributing a failure (e.g. OpenStudio emits no per-space *design* loads — a documented route limitation, so every `heating.designLoad` is `TasOnly`) |
| Determinism | reports written LF / UTF-8 no BOM, full-precision values |

The near-identical geometry is the strongest signal that the whole chain is sound: two independent
engines, two independent producers, one neutral schema, and the areas/volumes agree to seven
significant figures.

## Finding 1 — the provenance gate caught a real producer defect

The comparator reported **provenance INCOMPATIBLE** on a single field:

| Field | TAS | OpenStudio |
|---|---|---|
| `designDaySource` | `EmbeddedModel` | `None` |

This is **not** a genuine input difference — both runs used the identical model, and the OpenStudio
run demonstrably did construct design days. The OpenStudio producer **hardcodes**
`DesignDaySource.None` (`SAM_OpenStudio/benchmark/SAM.Analytical.OpenStudio.Benchmark/Program.cs:86`)
instead of deriving it as the TAS producer does.

So the provenance gate did exactly the job it was added for: it refused to bless a comparison whose
declared inputs did not line up, and pointed at the responsible field. **Fix belongs in
SAM_OpenStudio** (separate repo, separate PR) — tracked as a follow-up; nothing to change in B3.

## Finding 2 — the candidate conditioning pairing is NOT yet numerically equivalent

With the correct weather and an aligned model, the whole-model numbers still diverge well beyond the
provisional fail band:

| Metric | TAS | OpenStudio | Rel. diff | Band |
|---|---:|---:|---:|---|
| Annual heating (kWh) | 4331.96 | 1751.18 | **59.6 %** | Fail |
| Peak heating load (kW) | 3.934 | 5.721 | **31.2 %** | Fail |
| Peak heating hour | 919 | 775 | 144 h | Warn (informational) |
| Annual cooling (kWh) | 0 | 5.25 | 100 % | Fail |
| Peak cooling load (kW) | 0 | 0.040 | 100 % | Fail |
| Floor area / volume | — | — | ~2e-7 | Match |

Per-space heating peaks diverge similarly (sampled: 29 %, 52 %). Note TAS reports a *measured zero*
for cooling on this heating-dominated model, so the cooling rows compare 0 against a near-zero
OpenStudio value — the 100 % figures there are not meaningful magnitudes.

**Conclusion:** the D10 candidate pairing (OpenStudio Ideal Loads ↔ TAS thermostats + IZAMs + TBD
sizing) is **not** validated by this run and must remain labelled a *candidate*. The design-day
reporting defect above is a plausible contributor (a differing sizing basis changes loads), so the
pairing should be re-assessed only after that fix, and then investigated properly against the
B4 corpus — not by tuning tolerances.

Two further observations worth carrying into that work:

- TAS emits `peakCoolingHour = 0` with `available: true` while its cooling load is a measured zero;
  a peak *hour* for a zero load carries no information and would be better reported unavailable
  (a B2 producer-semantics question, not a comparator one).
- One space reported `heating.peakLoad` as available `0` from OpenStudio while TAS reported it
  unavailable, i.e. the two engines disagree about which spaces are conditioned.

## Status

- **B3 comparator: validated on real two-engine data.** Alignment, banding, circular hours,
  reconciliation, availability handling, determinism and the provenance gate all behaved as specified.
- **Engine equivalence: open.** Deliberately out of B3's scope; it needs the producer fix, then the
  B4 corpus and an energy-modeller review.

Artefacts in this folder are the verbatim outputs of the run above: the two neutral input documents
and the three generated reports.
