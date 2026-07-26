# Design-day audit contract `design-day-audit-v1` — DRAFT

**Status: draft for agreement.** Nothing is implemented against this document yet — no schema types, no
producers, no comparator. Per the agreed sequence, the TAS and OpenStudio producer branches are not
created until this contract is settled. Field names, enum tokens and required/optional status are all
open to revision here, and only here; once producers exist, changing them costs a schema version.

## 1. Scope

This contract defines `design-day-audit-<engine>.json`: a portable, engine-neutral record of **the
design days each engine actually used, and how each engine responded to them**.

It exists because the benchmark's cross-engine load disagreement is unexplained, and the leading
hypothesis is that the two engines are not sizing from equivalent design days. The recorded runs
already show that the OpenStudio route collapses SAM's 24-hour hourly design-day profiles into a
parametric `SizingPeriod:DesignDay` — humidity to a single dew point, solar to `ASHRAEClearSky` — while
TAS consumes the hourly profile directly. This contract makes that difference measurable instead of
narrated.

### What it is not

- **Not a gate.** A design-day audit document carries evidence, not a pass/fail verdict on either
  engine. It must never contribute to the benchmark gate defined in
  [TOLERANCES.md](TOLERANCES.md).
- **Not part of the benchmark metric contract.** It is a separate document type with its own version
  line. It does not extend `benchmark-<engine>.json`
  ([SCHEMA.md](SCHEMA.md)), and its values use a deliberately distinct type
  ([§4](#4-auditvalue-and-hourlyseries)) so an audit value can never be fed to the gating comparator by
  accident.
- **Not a re-simulation.** v1 is derivable from artefacts the existing producers already leave behind
  (TBD, TSD, `eplusout.eio`, `eplusout.sql`). Adding EnergyPlus `Output:Variable` requests to capture
  sizing-period weather is **out of scope for v1** — it would change converter output and require a new
  simulation to serve the audit.

## 2. The three representations

Every quantity in a design-day audit belongs to exactly one of three representations. These are
declared, never inferred, and they are the core of the contract: a comparison that does not say which
representation each side contributed is not interpretable.

| Representation | Meaning | Who can supply it | Source |
|---|---|---|---|
| `ObservedHourly` | A 24-hour series the engine actually consumed, as authored in the model. Nothing is fitted or modelled. | TAS | TBD design days, via `SAM.Analytical.Tas.Query.DesignDays` |
| `ProcessedParametric` | The parametric design-day definition the engine **accepted and echoed back**, after its own input processing. Not a series. | EnergyPlus | `eplusout.eio` — `Environment:Design Day Data` and `Environment:Design Day Misc` |
| `DerivedHourly` | A 24-hour series **reconstructed** from a `ProcessedParametric` definition using the engine's documented design-day algorithms. | EnergyPlus | computed offline from the `.eio` parameters |

### The wording rule is normative

A `DerivedHourly` series was **not reported by the engine**. Any producer output, report, commit
message or README describing one must say it was *reconstructed from the engine's processed
parameters using the documented design-day algorithms*. Writing that EnergyPlus "reported" an hourly
sizing-weather series would be false: the recorded runs prove the SQL contains no sizing-period
outdoor conditions, because the producer never requests them.

The corresponding name for the whole exercise is an **"EnergyPlus processed design-day parameter audit
with reconstructed hourly conditions"** — not an "hourly weather comparison".

### Legitimate pairings

| Comparison | Sides | What it establishes |
|---|---|---|
| **Collapse loss** | `ObservedHourly` → its own parametric summary, against `ProcessedParametric` | What the hourly → parametric translation discarded, on the OpenStudio side alone. Needs no reconstruction and is therefore the strongest evidence available. |
| **Instantiated conditions** | `ObservedHourly` vs `DerivedHourly` | Whether the two engines sized from equivalent 24-hour conditions. Weaker: one side is reconstructed. |

A `ProcessedParametric` definition must never be compared hour-by-hour against an `ObservedHourly`
series — they are not the same kind of object. Reconstruct first, and label the result.

## 3. Stage A and Stage B are separate

The document has two independent top-level sections, and they must stay separate through extraction,
serialization, comparison and reporting:

| Stage | Question | Content |
|---|---|---|
| **Stage A — conditions** | What weather did each engine size from? | Design-day identity, location, parametric definition, hourly series |
| **Stage B — sizing response** | What did each engine do with it? | Per-space peak sizing load, peak time, zone and outdoor conditions at peak, conditioned classification |

Rules:

- Stage A and Stage B are **never merged into one comparison object** and never share a status.
- A Stage B difference is **not interpretable without its Stage A pairing** — if the conditions differ,
  a differing response is expected, not a finding. A report that shows Stage B without the
  corresponding Stage A must say so explicitly.
- Stage A carries no space-level data; Stage B carries no weather series. Each Stage B space references
  the design day that drove it by `designDayKey` ([§7](#7-stage-b--sizing-response)).

## 4. `AuditValue` and `HourlySeries`

`AuditValue` is the audit counterpart of `MetricValue` ([SCHEMA.md](SCHEMA.md#metricvalue)) — the same
availability discipline, plus a mandatory basis:

| Field | Type | Requirement |
|---|---|---|
| `value` | number or null | Finite; `null` if and only if `available` is `false` |
| `unit` | string | Canonical unit token, always present, never inferred from the field name |
| `available` | boolean | `true` only when the source genuinely supplied it; a measured zero is available |
| `basis` | string or null | `Observed`, `EngineEchoed` or `Reconstructed`; `null` if and only if `available` is `false` |

`basis` records **how the number came to exist**, independently of the representation it sits in:

| `basis` | Meaning |
|---|---|
| `Observed` | Read from the model or an input file as authored. No engine processing, no fitting. |
| `EngineEchoed` | Read from the engine's own echo of what it accepted (`.eio`, `ZoneSizes`). The engine's value, after its input processing. |
| `Reconstructed` | Computed by the producer from `EngineEchoed` inputs using a documented algorithm. Never the engine's own output. |

A separate type from `MetricValue` is deliberate: it keeps audit evidence structurally incapable of
entering the gating comparator, and it forces the basis to be stated on every number rather than
implied by which file it came from.

`AuditValue` holds numbers only. Two sibling shapes carry non-numeric evidence with the same
`available`/`basis` discipline but **no `unit`** — `AuditToken` (a string enumeration token) and
`AuditFlag` (a boolean indicator). They are defined and enumerated in
[§6](#6-stage-a--conditions), and are compared for exact ordinal equality only, never banded.

`HourlySeries` carries a 24-hour profile. The basis is declared once for the series, because a
reconstruction is uniformly reconstructed — per-hour bases would suggest a mixture the algorithms
cannot produce:

| Field | Type | Requirement |
|---|---|---|
| `quantity` | string | Quantity token ([§6](#6-stage-a--conditions)) |
| `unit` | string | Canonical unit token |
| `basis` | string | `Observed` or `Reconstructed`; a series is never `EngineEchoed` in v1 |
| `available` | boolean | `false` when the source cannot supply this quantity at all |
| `values` | array or null | Exactly 24 finite numbers, hour 0 to hour 23 local standard time; `null` when unavailable |

Partial series are invalid: 24 values or `null`. A quantity the source does not carry is
`available: false`, never zero-filled — a zero-filled solar profile is indistinguishable from a real
`clearness 0.0` heating day, which is exactly the defect this audit is meant to expose.

## 5. Provenance

Design-day provenance is stricter than benchmark provenance, because the recorded runs showed
`benchmark-<engine>.json` asserting a `samCommit` whose code was demonstrably not in the loaded
binaries. Every field below is required unless marked otherwise.

### 5.1 Source files and hashes

`sourceFiles` is an array, sorted by `role` then `identity`, of every file that contributed evidence:

| Field | Type | Meaning |
|---|---|---|
| `role` | string | `SourceModel`, `Weather`, `Tbd`, `Tsd`, `Eio`, `Sql`, `Idf`, `Osm` |
| `identity` | string | Portable identity — file name or station label, **never an absolute path** |
| `hash` | string | `sha256:` plus 64 lowercase hexadecimal characters, over the exact bytes read |

The model and weather entries must agree with the corresponding `benchmark-<engine>.json` hashes when
the two documents describe the same run.

### 5.2 Loaded assembly hashes

`assemblies` is an array, sorted by `name`, recording **what actually executed** rather than a claim
about a repository:

| Field | Type | Meaning |
|---|---|---|
| `name` | string | Assembly simple name, e.g. `SAM.Analytical` |
| `hash` | string | `sha256:` of the loaded assembly file bytes |
| `fileVersion` | string or null | File version; `1.0.0.0` for an unstamped local build |
| `informationalVersion` | string or null | Informational version when present |

This closes a known weakness. `samCommit` and `runnerCommit` are operator-supplied strings that no
producer can verify, and local SAM assemblies are unstamped `1.0.0.0`, so a commit field can name code
that is not in the binary. They may still be recorded for continuity, but **`assemblies` is the
authoritative record**, and a consumer comparing two audit documents must compare assembly hashes, not
commit strings.

### 5.3 Design-day identity and location

Per design day, in Stage A ([§6](#6-stage-a--conditions)) — listed here because it is provenance, not
measurement:

| Field | Type | Meaning |
|---|---|---|
| `key` | string | Stable **within-document** key; the join target for Stage B. Not comparable across documents |
| `alignmentKey` | string or null | **Cross-document** pairing key; see [§5.3.1](#531-cross-document-alignment) |
| `alignmentKeyVersion` | string | Semantic version of the derivation rules below; required whenever `alignmentKey` is non-null |
| `name` | string | Verbatim engine/model name, unmodified — e.g. `London_TRY ANN HTG 100% CONDS DB` |
| `sourceDesignDayName` | string or null | The SAM source design-day name the key was derived from; `null` when the day cannot be traced to one |
| `loadType` | string | `Heating` or `Cooling` |
| `dayType` | string or null | Engine day-type token, e.g. `WinterDesignDay` |
| `month`, `dayOfMonth` | number or null | Calendar position when declared |
| `origin` | string | `EmbeddedModel`, `Ddy` or `Unknown` — where the day came from |
| `location` | object | See below |

`location` is recorded per design day, not once per document, because a design day can carry a
location unrelated to the annual weather — the recorded HungaryHouse runs use **London** design days
with **Budapest** annual weather, and a document that stored one shared site would hide exactly that:

| Field | Type | Meaning |
|---|---|---|
| `name` | string or null | Location label as carried by the design day |
| `latitude`, `longitude` | number or null | Degrees; positive north and east |
| `timeZone` | number or null | Hours offset from UTC |
| `elevation` | object | See [§5.4](#54-elevations) |

#### 5.3.1 Cross-document alignment

`key` is stable only within one document, and `name` is deliberately the **unmodified engine label** — so
neither can pair a TAS day with an EnergyPlus day. Without a defined cross-document identity, both
Stage A comparisons and the Stage B interpretation that rests on them would be ambiguous. `alignmentKey`
supplies it.

**The common ancestor is the SAM model.** SAM `DesignDay` derives from `Weather.WeatherDay` and carries
**no GUID**, so the key is derived from its stable attributes rather than an identifier:

```text
alignmentKey = "<LoadType>|<NormalisedSourceName>"
```

where `LoadType` is `Heating` or `Cooling`, and `NormalisedSourceName` is `sourceDesignDayName`:

1. trimmed of leading and trailing white space;
2. every internal run of white space collapsed to one space (U+0020);
3. upper-cased with `ToUpperInvariant`.

Both producers must derive the key from the **SAM source design day**, never from a name the engine
assigned or rewrote. `alignmentKeyVersion` is `1.0.0` for these rules; a changed normalisation requires a
new version, so an algorithm change can never masquerade as a changed design day — the same discipline
`canonicalizationVersion` applies to model hashes.

Pairing rules, all normative:

- The comparator pairs **only** on exactly equal `alignmentKey`. There is no fuzzy, positional or
  nearest-name fallback.
- `alignmentKey` is `null` when the day cannot be traced to a SAM source day — which is the expected case
  for `origin` of `Ddy` or `Unknown`. A null-keyed day is **not pairable**: it is reported one-sided and
  never guessed into a pair.
- If two days in one document share an `alignmentKey`, **every** member of that group is excluded from
  pairing and reported as ambiguous. This mirrors the duplicate-GUID rule for space alignment in
  [METHODOLOGY.md](METHODOLOGY.md#space-identity-and-alignment) — first-match would silently pair the
  wrong day.
- An unpaired or ambiguous day is a **coverage** observation, never a numerical difference.

### 5.4 Elevations

Elevation is first-class, because site elevation feeds the design-day standard barometric pressure, and
the recorded runs carry an EnergyPlus warning of `Elevation difference=[416.00] percent, [104.00]
meters` between model and weather file:

| Field | Type | Meaning |
|---|---|---|
| `modelSiteElevation` | `AuditValue` | Elevation carried by the SAM model, `m` |
| `weatherFileElevation` | `AuditValue` | Elevation carried by the weather file, `m` |
| `absoluteDifference` | `AuditValue` | Absolute difference, `m` |
| `engineElevationUsed` | `AuditValue` | The elevation the engine states it used, `m` |
| `elevationAuthority` | string | `Model`, `WeatherFile`, `TranslatedSite` or `Unknown` |
| `barometricPressureUsed` | `AuditValue` | Pressure the engine states it used, `Pa` |
| `pressureBasis` | string | `ModelProfile`, `ElevationStandard`, `EngineDefault` or `Unknown` |

`elevationAuthority` **may be `Unknown`, and `Unknown` is the correct v1 answer** until it is
established which site is authoritative for each engine. The contract requires the question to be
recorded, not answered. No elevation discrepancy is to be "fixed" before this audit says which side
each engine honoured.

### 5.5 Sizing factors

This block exists to settle an open question the benchmark cannot currently answer — whether a
reported sizing load is pre- or post-sizing-factor. The recorded runs show EnergyPlus applying
`Sizing:Parameters` of 1.25 heating and 1.15 cooling, while TBD's treatment of `maxHeatingLoad` is
unknown:

| Field | Type | Meaning |
|---|---|---|
| `heatingSizingFactor` | `AuditValue` | Factor the engine applied, unit `dimensionless` |
| `coolingSizingFactor` | `AuditValue` | Factor the engine applied, unit `dimensionless` |
| `factorSource` | string | `EngineEcho`, `ModelSetting` or `Unknown` |
| `reportedLoadBasis` | string | `PreSizingFactor`, `PostSizingFactor` or `Unknown` |

`reportedLoadBasis` is **required and may be `Unknown`**. A producer that cannot establish whether its
own reported load is pre- or post-factor must say `Unknown` rather than guess. This is the field that
makes the question answerable: it is settled per engine by comparing a reported load against that
engine's own unfactored result, **not** by comparing the two engines with each other — cross-engine
ratios cannot separate a sizing margin from a physical difference when the two engines are not
evaluating equivalent conditions.

### 5.6 Run provenance

`engine` (kind/name/version/sdkVersion), `route`, `runTimestampUtc`, `durationSeconds`, `state`,
`warnings` and `notes` follow [SCHEMA.md](SCHEMA.md#provenance) unchanged. `samCommit` and
`runnerCommit` are optional here and, if present, are subordinate to `assemblies`
([§5.2](#52-loaded-assembly-hashes)).

## 6. Stage A — conditions

`stageA.designDays` is an array sorted by `key`. Each entry carries its identity and location
([§5.3](#53-design-day-identity-and-location)), a declared `representations` array, and whichever of
the two payloads it can supply.

`parametric` is present when the entry offers `ProcessedParametric`. Every field carries
`basis: EngineEchoed`, but **not every field is numeric**, so the block uses three shapes. All three
share the `available`/`basis` discipline of [§4](#4-auditvalue-and-hourlyseries); they differ only in the
type of `value`:

| Shape | `value` type | `unit` | Used for |
|---|---|---|---|
| `AuditValue` | number or null | required | measured quantities |
| `AuditToken` | string or null | **absent** | engine enumeration tokens |
| `AuditFlag` | boolean or null | **absent** | engine indicator flags |

`AuditToken` and `AuditFlag` carry no `unit`, because a token has no unit and a fabricated one would
invite meaningless comparison. Their values are compared for **exact ordinal equality** only, never
banded.

Numeric fields (`AuditValue`):

| Field | Unit | Notes |
|---|---|---|
| `maximumDryBulb` | `degC` | |
| `dailyDryBulbRange` | `deltaC` | A range of `0.00` means constant temperature for 24 hours |
| `humidityConditionValue` | `degC` or `kgWater/kgDryAir` | Unit follows `humidityConditionType`; the producer emits the unit that matches the echoed type |
| `barometricPressure` | `Pa` | |
| `windSpeed` | `m/s` | |
| `windDirection` | `deg` | |
| `skyClearness` | `dimensionless` | A clearness of `0.00` means zero solar for 24 hours |
| `ashraeTauB`, `ashraeTauD` | `dimensionless` | Optional; present for `ASHRAETau` models |
| `ashraeCoefficientA` | `W/m2` | Optional; apparent solar irradiation at air mass zero |
| `ashraeCoefficientB`, `ashraeCoefficientC` | `dimensionless` | Optional; echoed extinction and diffuse factors |

Token fields (`AuditToken`):

| Field | Example |
|---|---|
| `dryBulbRangeModifierType` | `DefaultMultipliers` |
| `humidityConditionType` | `Dewpoint` |
| `solarModel` | `ASHRAEClearSky` |

Flag fields (`AuditFlag`), all optional: `rainIndicator`, `snowIndicator`, `daylightSavingIndicator`.

`dimensionless` is a canonical unit token in this contract, used where a quantity is a genuine ratio. It
is never omitted and never left empty — an absent unit on an `AuditValue` is a validation error
([§9](#9-validation-rules)).

`series` is present when the entry offers `ObservedHourly` or `DerivedHourly`: an array of
`HourlySeries` ([§4](#4-auditvalue-and-hourlyseries)), sorted by `quantity`, drawn from these quantity
tokens:

| Quantity | Unit |
|---|---|
| `dryBulbTemperature` | `degC` |
| `relativeHumidity` | `percent` |
| `dewPointTemperature` | `degC` |
| `humidityRatio` | `kgWater/kgDryAir` |
| `globalHorizontalRadiation` | `W/m2` |
| `diffuseHorizontalRadiation` | `W/m2` |
| `directNormalRadiation` | `W/m2` |
| `windSpeed` | `m/s` |
| `windDirection` | `deg` |
| `cloudCover` | `tenths` |
| `barometricPressure` | `Pa` |

An entry may offer both `ProcessedParametric` and `DerivedHourly` — that is the expected EnergyPlus
shape, and the pair is what makes the reconstruction auditable.

### Reconstruction declaration

Any document containing a `Reconstructed` series must carry `stageA.reconstruction`:

| Field | Type | Meaning |
|---|---|---|
| `algorithm` | string | Named algorithm, e.g. `EnergyPlus-DefaultMultipliers-ASHRAEClearSky` |
| `algorithmVersion` | string | Semantic version of the producer's implementation |
| `inputs` | array of strings | The `parametric` field names consumed |
| `limitations` | array of strings | Known fidelity limits, deterministically ordered |

A reconstruction with no declared algorithm is invalid. The declaration is what lets a later reader
decide whether a difference is physics or reconstruction error.

## 7. Stage B — sizing response

Space identity and alignment follow
[METHODOLOGY.md](METHODOLOGY.md#space-identity-and-alignment) — GUID first, name only as a declared
fallback.

`stageB.spaces` is an array sorted by the **total** key `(guid, name, designDayKey, loadType)`, every
component ordinal, with `null` `designDayKey` sorting **last**. One space produces a separate record per
design day and load type, so GUID and name alone leave ties, and tied records would serialize in
producer-enumeration order — which is not deterministic and would break artefact diffing. The lesson is
already paid for in this programme: **never claim deterministic ordering from a non-total comparator**.

| Field | Type | Unit | Meaning |
|---|---|---|---|
| `guid`, `name` | string | — | Space identity |
| `designDayKey` | string or null | — | The Stage A day that drove this result; `null` when the engine does not attribute it |
| `loadType` | string | — | `Heating` or `Cooling` |
| `designLoad` | `AuditValue` | `W` | Sizing load, on the basis declared by `reportedLoadBasis` |
| `unfactoredDesignLoad` | `AuditValue` | `W` | Optional; the same load before any sizing factor, when the engine exposes both |
| `designFlow` | `AuditValue` | `m3/s` | Optional |
| `peakHour` | `AuditValue` | `hourOfDay` | `0..23` — a design-day hour, **not** an hour of year. Unavailable on the TAS side until the `GetPeakZoneGains` index convention is pinned (see below) |
| `peakMinute` | `AuditValue` | `min` | Optional; EnergyPlus reports sub-hourly peak times |
| `zoneTemperatureAtPeak` | `AuditValue` | `degC` | |
| `outdoorTemperatureAtPeak` | `AuditValue` | `degC` | |
| `zoneHumidityRatioAtPeak` | `AuditValue` | `kgWater/kgDryAir` | Optional |
| `conditioned` | boolean or null | — | Engine's classification; `null` when not exposed |
| `setpointState` | string or null | — | Engine's setpoint/thermostat state at peak, verbatim |
| `series` | array or null | — | Optional `HourlySeries` per space, drawn from the Stage B quantity tokens below |

`peakHour` is an hour **of the design day**, deliberately a different unit token from the benchmark's
`hourOfYear`, so the two can never be silently compared.

### Stage B quantity tokens

`HourlySeries.quantity` is drawn from a **stage-specific** token list. Stage A quantities describe
outdoor conditions ([§6](#6-stage-a--conditions)); Stage B quantities describe the zone response, and a
Stage A series may never use a Stage B token or the reverse:

| Quantity | Unit |
|---|---|
| `zoneSensibleLoad` | `W` |
| `zoneLatentLoad` | `W` |
| `zoneTemperature` | `degC` |
| `zoneHumidityRatio` | `kgWater/kgDryAir` |
| `zoneSetpointTemperature` | `degC` |

`zoneLatentLoad`, `zoneHumidityRatio` and `zoneSetpointTemperature` are optional; a producer that cannot
supply one emits `available: false` rather than omitting the entry silently or zero-filling it.

### The two sides are asymmetric, and that is recorded not hidden

| Side | Scalars | 24-hour series |
|---|---|---|
| TAS | from TSD `HeatingDesignData` / `CoolingDesignData` | **available** — TSD carries hourly design-day data |
| EnergyPlus | from SQL `ZoneSizes` and `.eio` `<Zone Sizing Information>` | **not available in v1** — would need added `Output:Variable` requests and a re-run |

So Stage B in v1 compares scalars cross-engine, and carries hourly series on the TAS side only. A
report must not present a TAS 24-hour profile beside a reconstructed outdoor series as though both were
engine responses.

### What the TAS side already provides, and what it does not

`SAM.Analytical.Tas.Query.DesignDataDictionary` already yields, per zone GUID, the peak load, an index,
and the owning `HeatingDesignData` / `CoolingDesignData` — via `MaxValueDictionary` over
`GetPeakZoneGains`. So the Stage B **scalars are available with no new TAS traversal**, and
`designDayKey` is attributable from the owning design-data object.

Three constraints follow, and the producer must respect all three:

1. **`MaxValueDictionary` collapses the per-design-day dimension**: it keeps only the maximum across all
   design days per zone. The audit needs a value *per design day*, so the existing helper cannot be
   reused as-is for Stage B — iterate the design-data collections directly and keep each day.
2. **The index returned by `GetPeakZoneGains` has an unverified base and span.** Nothing in the
   repository documents whether it is 0- or 1-based, or whether it indexes hours of the design day.
   `peakHour` must not be populated from it until that is pinned against a known case; until then it is
   `available: false`, and the raw index is carried as a producer diagnostic.
3. **`SAM_Tas/.../Query/YearlyValues.cs` must not be reused** for the 24-hour series: it always
   allocates 8760 values and loops 365 days via `GetDailyZoneResult` even when the receiver is
   `HeatingDesignData`. A dedicated 24-hour extractor is required.

### Values carried verbatim, unmapped

`ZoneSizes.PeakTemp` is carried as `outdoorTemperatureAtPeak` **only if** the audit establishes it is an
outdoor value. Until then it is carried verbatim under a producer-diagnostic field and mapped to
nothing: EnergyPlus documents the zone sizing peak temperature as a *zone* value
(`ZoneTempAtHeatPeak`), yet every row of the recorded run carried a value bit-identical to the design
day's *outdoor* maximum dry bulb. Settling that is a purpose of this audit, not an assumption of it.

## 8. Determinism and serialization

Identical to the benchmark contract, and required for the same reason — a recorded artefact must be
diffable:

- System.Text.Json, invariant culture, deterministic property order, deterministic array order.
- Full finite `double` precision; no rounding of recorded values.
- No non-finite JSON numbers.
- LF endings, UTF-8 without BOM.
- Every array declares its sort key in this document; no array order is incidental.
- Committed artefacts are LF-pinned under `docs/benchmark/reports/**` in `.gitattributes`.

## 9. Validation rules

A document is invalid if any of the following holds:

1. `available` is `true` and `value` is `null`, or `available` is `false` and `value` is non-null.
2. `available` is `true` and `basis` is `null`, or `available` is `false` and `basis` is non-null.
3. A `HourlySeries` has `values` whose length is not exactly 24, or is `available: true` with `null`
   values.
4. A `HourlySeries` `quantity` token is absent, or is not in the token list **for its own stage** —
   Stage A quantities in Stage A, Stage B quantities in Stage B, never crossed.
5. An `AuditValue` has no `unit`, or its `unit` is not a token named in this document. An `AuditToken` or
   `AuditFlag` carries a `unit` at all.
6. An `AuditToken` value is not a string, or an `AuditFlag` value is not a boolean, or an `AuditValue`
   value is not a finite number.
7. A series carries `basis: EngineEchoed`.
8. A `Reconstructed` series exists without `stageA.reconstruction`.
9. A `stageB` record names a `designDayKey` absent from `stageA.designDays`.
10. `alignmentKey` is non-null and `alignmentKeyVersion` is absent, or `alignmentKey` is non-null while
    `sourceDesignDayName` is `null`, or `alignmentKey` does not equal the value the declared
    `alignmentKeyVersion` rules produce from `loadType` and `sourceDesignDayName`.
11. `reportedLoadBasis`, `pressureBasis`, `elevationAuthority` or `factorSource` is missing. `Unknown` is
    a valid value; omission is not.
12. A hash is not `sha256:` plus 64 lowercase hexadecimal characters.
13. An array is not ordered by the total sort key this document declares for it.
14. `stageA` and `stageB` content is interleaved in one object.

Rules 2, 5, 6, 7, 8 and 11 exist to make the central failure mode — an unlabelled, mistyped or
over-claimed value — a validation error rather than a reporting judgement. Rule 10 makes the
cross-document pairing key self-checking, so a producer cannot hand-write a key that its own declared
rules would not produce. Rule 13 makes non-determinism a contract error rather than something a reader
discovers from a noisy diff.

A duplicated `alignmentKey` is deliberately **not** an error: it is a legitimate model condition, handled
by excluding the whole group from pairing ([§5.3.1](#531-cross-document-alignment)) and reporting it as
ambiguous.

## 10. Deliberately excluded from v1

| Excluded | Why |
|---|---|
| EnergyPlus hourly sizing-period weather from the engine itself | Needs added `Output:Variable` requests and a re-run; would change converter output |
| EnergyPlus per-space 24-hour sizing profiles | Same reason |
| System and plant sizing (`SystemSizes`, `ComponentSizes`) | Zone-level first; these are available with no re-run and can follow |
| Any gate contribution | This contract is evidence, not a verdict |
| Tolerance bands for design-day quantities | Cannot be set before the first audit shows the real spread |

Adding the first two requires an explicit **benchmark audit mode**, agreed separately, and only if the
reconstruction leaves material ambiguity.

## 11. Open questions for agreement

These need a decision before implementation, and are the reason this document is a draft:

1. **One document per engine, or one per engine per design day?** This draft assumes one document per
   engine carrying all its design days, mirroring `benchmark-<engine>.json`.
2. **Is `design-day-audit-v1` versioned independently of the benchmark schema?** This draft assumes yes,
   with its own `auditSchemaVersion`.
3. **Should `unfactoredDesignLoad` be required on the EnergyPlus side?** It is available
   (`ZoneSizes.CalcDesLoad` alongside `UserDesLoad`) and would settle `reportedLoadBasis` on that side
   immediately. Making it required would force the TAS side to declare `Unknown` explicitly, which may
   be the more useful outcome.
4. **Does the audit live beside the benchmark reports** in `docs/benchmark/reports/<date>-…/`, or in its
   own `docs/benchmark/design-day-audit/` tree?
5. **Does `ObservedHourly` need a parametric summary field**, so collapse loss can be computed without
   the comparator re-deriving max/range/mean from the series each time?
6. **Is `loadType` + normalised name a strong enough `alignmentKey`?** SAM `DesignDay` has no GUID, so
   the key rests on the name. Adding `month` and `dayOfMonth` would harden it against two same-named
   days, at the cost of failing to pair when one route drops the calendar position. The recorded
   fixture does not exercise the collision — its days are distinctly named — so this is a judgement
   about models not yet seen.

## 12. Glossary additions

Terms added to [GLOSSARY.md](GLOSSARY.md) by this contract: *Alignment key*, *Collapse loss*,
*Derived hourly*, *Observed hourly*, *Processed parametric*, *Reported load basis*, *Value basis*.
