# Benchmark methodology

## Purpose

This programme compares results from EDSL Tas and OpenStudio/EnergyPlus for the same SAM `AnalyticalModel`. It is a cross-engine benchmark, not an assertion that either engine is ground truth.

The benchmark is designed to be:

- engine-neutral: neither engine loads or calls the other;
- repeatable: inputs, versions, routes, and weather are recorded;
- portable: engines exchange versioned JSON files, not runtime objects;
- auditable: missing data, alignment fallbacks, and semantic caveats remain visible.

## Workflow

The workflow is two-stage and file-mediated:

1. Each engine runs independently from an agreed source model and weather file.
2. Each producer converts SAM results into the same benchmark JSON contract.
3. The benchmark JSON files are transferred to any machine that can run the headless comparator.
4. The comparator aligns spaces, computes differences and reporting bands, and writes Markdown, CSV, and summary JSON reports.

There is no direct runtime coupling between Tas and OpenStudio. A failed engine run may emit failure provenance, but it must not emit invented measurements.

## Routes

Every document records one route. Results from different routes must not be merged or relabelled.

| Route | Purpose | Gate role |
|---|---|---|
| Native-TAS | SAM model → shared gbXML workflow → Tas TBD/TSD → SAM results | Candidate primary comparison |
| Native-OpenStudio | SAM model → OpenStudio model → EnergyPlus SQL → SAM results | Candidate primary comparison |
| SharedGbXML-TAS / SharedGbXML-OpenStudio | The same gbXML is supplied to both engines | Optional diagnostic comparison, spike-gated |
| TPD production HVAC | Tas production plant through TPD | Later, separately labelled, informational only |

The native routes test the complete production translation paths as well as the simulation engines. The optional shared-gbXML routes narrow the comparison by holding the interchange model constant. They do not replace the native routes.

## Candidate conditioning pairing

The candidate primary pairing is:

- OpenStudio Ideal Loads; and
- Tas thermostats with IZAMs and TBD sizing.

This pairing is **not frozen** in B0. B2 must run the first Tas `SingleBox` validation and confirm meaningful conditioning, annual heating/cooling energy, and heating/cooling loads. An energy modeller must review that evidence before the pairing can become the primary gate. If the checkpoint fails, the pairing and affected contracts must be revised explicitly; production TPD HVAC must not be substituted silently.

## Inputs and run controls

A comparison run must use the same intended source model, weather data, and agreed design-day basis. Provenance records both the exact source-file hash and the canonical SAM-model hash so that byte-level and semantic-model equality can be assessed separately.

Each producer records:

- source model identity and both hashes;
- SAM and producer revisions;
- engine, SDK, route, weather, and design-day information;
- UTC run time, duration, state, warnings, and notes.

Absolute machine paths are runtime details and must not be committed or used as identities. Files are identified by portable names and content hashes.

### The two model hashes

`sourceFileHash` is the SHA-256 of the exact original input-file bytes. It proves the producers read the identical file and nothing more; re-saving the same model with different formatting changes it.

`canonicalModelHash` is the SHA-256 of the canonical representation of the neutral loaded SAM model. It proves the producers simulated the same model once non-semantic ordering and formatting variation has been removed.

Each producer computes the canonical hash by loading the source JSON into an `AnalyticalModel`, converting that model back to its normal SAM JSON representation through the shared SAM serialization API **before applying any engine-specific modification**, canonicalizing that JSON with the engine-neutral helper in `SAM.Analytical.Benchmark`, and hashing the canonical UTF-8 bytes:

```text
source bytes
  → sourceFileHash
  → deserialize AnalyticalModel
  → SAM ToJsonObject
  → canonical JSON v1
  → canonicalModelHash
```

The canonical hash always describes the neutral loaded model, never an engine artefact. Hashing an OpenStudio model, gbXML, a Tas T3D/TBD/TSD file, simulation results, or an engine working directory yields a value that cannot be compared across engines; that is a contract violation, not an approximation.

Producers must not invent their own canonicalization. Both engines call the same helper, so a canonical-hash difference means a model difference rather than an implementation difference. The rules are versioned as `canonicalizationVersion`, currently `1.0.0`, and are specified exactly in [SCHEMA.md](SCHEMA.md). Changing them requires a new canonicalization version, because a changed algorithm changes every hash it produces; a comparator that sees two different canonicalization versions reports the mismatch instead of treating the hashes as comparable evidence.

## Result extraction

Producers read the existing SAM result model. Whole-model values come from `AnalyticalModelSimulationResult`; space values come from `SpaceSimulationResult` separated by `LoadType.Heating` and `LoadType.Cooling`. Engine-specific `Query.Source()` values are retained as provenance rather than treated as metric values.

Peak hours use the SAM interval-hour-of-year convention: integers `0..8759`, with `0` representing the interval ending at 01:00 on 1 January. Date-time conversion uses `Core.Query.IntervalHourOfYear` where a timestamp is available.

Unavailable results remain unavailable. They are represented by `available: false` and `value: null`; zero is reserved for a measured zero.

## Space identity and alignment

Both native producers begin with the same SAM spaces and emit each space's stable GUID and name. The comparator aligns spaces in this order:

1. exact GUID match;
2. unique, normalised name match when GUID matching is unavailable;
3. no match, reported as a diagnostic.

Duplicate GUIDs, duplicate fallback names, missing spaces, and ambiguous matches are never silently combined. Shared-gbXML translation may lose SAM GUIDs, so fallback use must be visible in the report. Geometry-based investigation may support human diagnosis but is not an automatic v1 match rule.

## Comparison and gate interpretation

The comparator checks schema compatibility, units, provenance, space alignment, metric availability, differences, and reconciliation before deriving a gate status. A unit mismatch is a contract error, not a numerical comparison.

The default 5% warning and 15% failure bands are provisional reporting bands. They are configurable and are not scientifically validated thresholds. Missing metrics report `N/A` and cannot fail a numerical band; missing required coverage and invalid provenance are reported separately.

See [METRICS.md](METRICS.md), [SCHEMA.md](SCHEMA.md), and [TOLERANCES.md](TOLERANCES.md) for the normative contracts.

## Required review before merge

An energy modeller must review:

- the metric definitions and sign conventions;
- the different whole-model peak-load semantics;
- the unresolved annual-consumption stored magnitude;
- the candidate conditioning pairing and its B2 checkpoint;
- the provisional nature of the reporting bands.

B0 is documentation only. It does not validate tolerances, freeze the conditioning pairing, or implement a producer or comparator.
