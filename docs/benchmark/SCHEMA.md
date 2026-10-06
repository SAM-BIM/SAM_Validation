# Benchmark JSON schema v1.0.0

## Scope

`benchmark-<engine>[-<route>].json` is the portable hand-off between an engine-specific producer and the engine-neutral comparator. The schema owns measurements and run provenance; it does not serialize engine runtime objects or the SAM result graph.

The implementation will use System.Text.Json with invariant-culture numbers, deterministic property order, deterministic array order, and full finite `double` precision without result rounding. JSON object order is not semantic, but deterministic output is required for reproducibility.

## Top-level document

| Field | Type | Requirement |
|---|---|---|
| `schemaVersion` | string | Required semantic version; B0 defines `1.0.0` |
| `provenance` | object | Required |
| `model` | object | Required; every declared metric is present as a `MetricValue` |
| `spaces` | array | Required; sorted deterministically by GUID then ordinal name |

Unknown fields do not change the meaning of known fields. A writer must not emit non-finite JSON numbers (`NaN` or infinity).

## Provenance

| Field | Type | Meaning |
|---|---|---|
| `sourceModelName` | string | Portable model label; not a filesystem path |
| `sourceModelGuid` | string or null | SAM model GUID in 32-character lowercase hexadecimal form; null only for a pre-model failure |
| `sourceFileHash` | string or null | SHA-256 of the exact input model-file bytes; null only when failure prevents hashing |
| `canonicalModelHash` | string or null | SHA-256 of the canonical SAM representation; null only when failure prevents canonicalization |
| `canonicalizationVersion` | string | Semantic version of the canonicalization rules |
| `samCommit` | string | SAM source revision used for the run |
| `runnerCommit` | string | Producer repository revision used for the run |
| `engine` | object | Engine kind, display name, engine version, and SDK version |
| `route` | string | Exact route identifier; routes are never inferred from engine kind |
| `weather` | object | Portable identity plus exact-file SHA-256 |
| `designDaySource` | string | `DDY`, `EmbeddedModel`, or `None` |
| `runTimestampUtc` | string | ISO 8601 UTC instant with `Z` suffix |
| `durationSeconds` | number | Non-negative elapsed wall-clock seconds |
| `state` | string | `Success` or `Failure` |
| `resultSources` | array of strings | Distinct non-empty SAM `Query.Source()` values retained from attached results |
| `warnings` | array of strings | Deterministically ordered warnings; no machine-specific paths |
| `notes` | array of strings | Deterministically ordered material assumptions or limitations |

Hashes use `sha256:` followed by 64 lowercase hexadecimal characters. `sourceFileHash` proves byte equality of the input file. `canonicalModelHash` proves equality under the named canonicalization rules; it must be calculated independently of the source-file bytes. One hash must never be copied into the other merely because the current serialization happens to match.

For a successful run, both model hashes and the weather hash are required. A failure document may use `null` only for a hash that could not be computed, and must explain the reason in `warnings` or `notes`. `canonicalizationVersion` is required on every document, including failures: a document that could not compute a canonical hash must still declare which rules it would have used. Canonicalization rules must remove non-semantic ordering and formatting variation without removing model content that can affect simulation; they are specified in full under [Canonical model hashing](#canonical-model-hashing).

Engine fields are:

| Field | Type | Meaning |
|---|---|---|
| `kind` | string | `OpenStudio` or `TAS` |
| `name` | string | Simulation engine name, such as `EnergyPlus` or `Tas` |
| `version` | string or null | Engine version; null only when unavailable and explained |
| `sdkVersion` | string or null | Translation/runtime SDK version when applicable |

Weather fields are `identity` and `hash`. `identity` is a portable station/file identity, not an absolute path.

v1 route tokens are `Native-OpenStudio`, `Native-TAS`, `SharedGbXML-OpenStudio`, and `SharedGbXML-TAS`. `TPD-TAS` is reserved for the later, informational production-HVAC comparison; a v1 primary-gate producer must not emit it. The two shared-gbXML tokens identify the paired documents generated from the same exact interchange file.

## Canonical model hashing

The two model hashes answer different questions and must never be copied into one another:

| Hash | Covers | Answers |
|---|---|---|
| `sourceFileHash` | the exact original input-file bytes, unmodified | Did both runs read the identical file? |
| `canonicalModelHash` | the canonical JSON v1 form of the neutral loaded SAM model | Did both runs simulate the same model, ignoring non-semantic formatting? |

The same model re-saved with different formatting keeps its `canonicalModelHash` and changes its `sourceFileHash`, so a `sourceFileHash` difference alone is not evidence of a different model.

### Canonical model procedure

For canonicalization version `1.0.0`, a producer must:

1. Load the source SAM JSON into a SAM `AnalyticalModel`.
2. Before applying any engine-specific modification, convert that model back to its normal SAM JSON representation using the shared SAM serialization API.
3. Pass that JSON representation to the engine-neutral canonical JSON helper in `SAM.Analytical.Benchmark`.
4. Compute SHA-256 over the resulting canonical UTF-8 bytes.
5. Store the result as `canonicalModelHash`.
6. Record `1.0.0` in `canonicalizationVersion`.

As a pipeline:

```text
source bytes
  → sourceFileHash
  → deserialize AnalyticalModel
  → SAM ToJsonObject
  → canonical JSON v1
  → canonicalModelHash
```

The canonical hash must represent the neutral loaded SAM model **before** any engine-specific translation or mutation. It must never be taken from an OpenStudio model, gbXML, a Tas T3D/TBD/TSD file, simulation results, or an engine working directory: those are engine artefacts and cannot be compared across engines. Round-tripping through `AnalyticalModel` is what makes the hash comparable between producers, because it discards exactly the writer-formatting variation that `sourceFileHash` deliberately keeps.

Both producers call the same helper. A canonical-hash difference therefore means a model difference, not an implementation difference; a producer must not invent its own canonicalization.

### Canonical JSON v1

The canonical form is produced by `SAM.Analytical.Benchmark.BenchmarkCanonicalJson`. The rules are exactly:

- Input must be a single valid JSON value, parsed with `System.Text.Json`, encoded as UTF-8 without a byte order mark.
- Object properties are written in ascending ordinal property-name order, comparing decoded names by UTF-16 code unit as .NET `StringComparer.Ordinal` does.
- Array order is preserved exactly.
- Property names and string values are emitted using `System.Text.Json` UTF-8 escaping through `JavaScriptEncoder.Default`. Non-ASCII characters are therefore emitted as `\uXXXX` escapes and the canonical form is always ASCII.
- Boolean and null values retain their normal JSON representations.
- Numeric values are emitted deterministically through the parsed `System.Text.Json` value representation: a token representable as `Int64` or `UInt64` is written exactly, and every other number is written through the shortest round-trippable binary64 form. `1`, `1.0`, `1.00` and `1e0` all canonicalize to `1`, and negative zero canonicalizes to `0`.
- No indentation or insignificant whitespace is emitted.
- Output is UTF-8 without a byte order mark.
- Only LF would be emitted if a line ending were ever emitted; v1 emits one compact JSON value with no trailing newline.
- Duplicate object-property names are rejected, including names that differ only in escape spelling.
- Comments and trailing commas are rejected.
- Non-finite values are rejected, including numeric tokens that overflow binary64.
- Text that is not well-formed UTF-8 is rejected, as is nesting deeper than 256 levels.
- No property is removed or ignored in version 1.
- No engine-specific or machine-specific normalization is allowed.

This is a versioned SAM benchmark canonicalization contract, not a claim of full semantic JSON equivalence. Numeric precision beyond binary64 is not preserved, so two distinct integers above 2^53 can canonicalize identically. The guarantee is narrower and sufficient for the benchmark: the same neutral SAM model produces byte-identical canonical output on any machine, in any operating-system culture, whatever the property order or formatting of the input.

### Changing the algorithm

> **Warning**
> Every rule above is load-bearing: changing any of them changes every hash the algorithm produces. A changed algorithm must never masquerade as a changed model. Any change to canonical JSON v1 requires a new `canonicalizationVersion` published with the code change, and hashes carrying different canonicalization major versions must not be compared.

`SAM.Analytical.Benchmark.BenchmarkCanonicalization.CurrentVersion` is the single authoritative source of the version string. DTOs, validation, and tests read it rather than repeating the literal.

## MetricValue

Every metric has the same shape:

```json
{
  "value": 123.4,
  "unit": "kWh",
  "available": true
}
```

`value` is a JSON number or `null`; the DTO representation is nullable. `unit` is always present, including for unavailable metrics. v1 unit tokens are:

- `kWh`
- `kW`
- `W`
- `Wh`
- `m2`
- `m3`
- `hourOfYear`
- `h`

### Availability invariant

- `available: true` requires a finite, non-null `value`.
- `available: false` requires `value: null`.
- A measured zero is `value: 0` and `available: true`.
- Zero must never stand for missing, failed, unsupported, or not-applicable data.

Violating the invariant makes the document invalid. The comparator does not repair it.

Hour-of-year values are represented by the common numeric field but must be integers in `0..8759`. Hours and physical magnitudes must be non-negative. Units must match the field contract in [METRICS.md](METRICS.md).

## Model object

The model object contains these required `MetricValue` properties:

- `consumptionHeating`
- `consumptionCooling`
- `peakHeatingLoad`
- `peakHeatingHour`
- `peakCoolingLoad`
- `peakCoolingHour`
- `floorArea`
- `volume`

Required property means the metric wrapper is present, not that a measurement is available. Unsupported or unresolved measurements use the availability invariant.

## Space object

Each space contains:

| Field | Type | Requirement |
|---|---|---|
| `guid` | string | Source SAM space GUID, 32-character lowercase hexadecimal; nullable only for a route that cannot preserve it |
| `name` | string | Source or translated space name |
| `area` | `MetricValue` | Unit `m2` |
| `volume` | `MetricValue` | Unit `m3` |
| `heating` | load-type object | Required |
| `cooling` | load-type object | Required |

Each load-type object contains `designLoad` (`W`), `peakLoad` (`W`), `peakHour` (`hourOfYear`), and `unmetHours` (`h`). Missing load-type results still emit all wrappers as unavailable.

Native routes must emit the GUID and name from the common source SAM `Space`. The comparator matches GUID first and then a unique normalised name. It diagnoses null/duplicate GUIDs, duplicate fallback names, ambiguous matches, and unmatched spaces.

## Example outline

```json
{
  "schemaVersion": "1.0.0",
  "provenance": {
    "sourceModelName": "SingleBox",
    "sourceModelGuid": "0123456789abcdef0123456789abcdef",
    "sourceFileHash": "sha256:<64 lowercase hex characters>",
    "canonicalModelHash": "sha256:<64 lowercase hex characters>",
    "canonicalizationVersion": "1.0.0",
    "samCommit": "<revision>",
    "runnerCommit": "<revision>",
    "engine": {
      "kind": "OpenStudio",
      "name": "EnergyPlus",
      "version": "<version>",
      "sdkVersion": "3.10.0"
    },
    "route": "Native-OpenStudio",
    "weather": {
      "identity": "<portable identity>",
      "hash": "sha256:<64 lowercase hex characters>"
    },
    "designDaySource": "DDY",
    "runTimestampUtc": "2026-07-21T12:00:00Z",
    "durationSeconds": 12.3,
    "state": "Success",
    "resultSources": ["<Query.Source() value>"],
    "warnings": [],
    "notes": []
  },
  "model": {
    "consumptionHeating": { "value": 1234.5, "unit": "kWh", "available": true },
    "consumptionCooling": { "value": null, "unit": "kWh", "available": false },
    "peakHeatingLoad": { "value": 12.3, "unit": "kW", "available": true },
    "peakHeatingHour": { "value": 205, "unit": "hourOfYear", "available": true },
    "peakCoolingLoad": { "value": 9.8, "unit": "kW", "available": true },
    "peakCoolingHour": { "value": 4602, "unit": "hourOfYear", "available": true },
    "floorArea": { "value": 240.0, "unit": "m2", "available": true },
    "volume": { "value": 720.0, "unit": "m3", "available": true }
  },
  "spaces": [
    {
      "guid": "fedcba9876543210fedcba9876543210",
      "name": "Office 1",
      "area": { "value": 40.0, "unit": "m2", "available": true },
      "volume": { "value": 120.0, "unit": "m3", "available": true },
      "heating": {
        "designLoad": { "value": 2100, "unit": "W", "available": true },
        "peakLoad": { "value": 2000, "unit": "W", "available": true },
        "peakHour": { "value": 210, "unit": "hourOfYear", "available": true },
        "unmetHours": { "value": 3, "unit": "h", "available": true }
      },
      "cooling": {
        "designLoad": { "value": null, "unit": "W", "available": false },
        "peakLoad": { "value": null, "unit": "W", "available": false },
        "peakHour": { "value": null, "unit": "hourOfYear", "available": false },
        "unmetHours": { "value": null, "unit": "h", "available": false }
      }
    }
  ]
}
```

## Version and compatibility policy

`schemaVersion` follows semantic versioning:

- major: incompatible field meaning, structure, unit, or requiredness change;
- minor: backward-compatible additive fields or enum values;
- patch: clarification or serialization fix that does not change meaning.

Comparator and document major versions must be equal. A major mismatch is rejected before metric comparison. An equal major with a different minor version produces a visible compatibility warning; comparison may continue only when all required v1 fields and understood enum values validate. Patch drift does not warn. Producers must not emit a version newer than the contract they actually implement.

`canonicalizationVersion` is versioned independently and follows the same major/minor/patch meanings. Validation requires a well-formed version on every document and rejects a major version other than the implemented `1`; drift within major `1` is accepted. A canonicalization-version mismatch does not prove different models: the comparator reports it and does not directly compare the canonical hashes as equivalent evidence.

The comparator-owned `comparison-summary.json` is a separate contract and carries its own `summarySchemaVersion`.
