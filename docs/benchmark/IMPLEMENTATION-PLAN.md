# SAM TAS ↔ OpenStudio Benchmark Programme — Implementation Plan (B0–B6)

> **Revision 3** — incorporates stakeholder corrections: (1) `feature/benchmark-*` branch names; (2) unavailable metrics carry `value: null`; (3) dual model hashes; (4) build-order verified to live in per-repo CI, not a SAM PR; (5) conditioning equivalence is a **candidate** in B0, frozen only after the first TAS SingleBox validation; (6) B5 GH host deferred to B5 kickoff; (7) **commit and push the feature branch after every stage** for multi-laptop continuity; plus a per-milestone **model-allocation** plan and a **mandatory Git rules** block for every implementation prompt.

## Context

We need a **defensible, repeatable benchmark** comparing two independent building-simulation engines — **TAS** (EDSL Tas; licensed, installed only on a separate laptop) and **OpenStudio/EnergyPlus** — over the same SAM `AnalyticalModel`s. Because TAS runs only on a licensed machine, the workflow is **two-stage and file-mediated**: each engine runs independently and emits the *same* engine-neutral benchmark JSON; a separate **headless comparator** later aligns model/space results, computes differences, assigns tolerance bands, and writes Markdown + CSV + summary-JSON. **No direct runtime coupling** between TAS and OpenStudio.

The **primary gate** harmonises *conditioning semantics* — OpenStudio **Ideal Loads** vs the equivalent idealised TAS route (thermostats + IZAMs + TBD sizing). This pairing is a **candidate** until the first TAS SingleBox run confirms meaningful conditioning, annual energy and loads (see D10). Production HVAC (TAS **TPD** plant) is deferred to a later, separately-labelled *informational* comparison. All work branches off **`sow/2026-Q3`** in each repo and merges back into `sow/2026-Q3`.

This plan is grounded in direct repository inspection (findings in §2). Three architecture forks were resolved with the stakeholder (§3).

---

## 1. Architecture & dependency diagram

```text
                         ┌───────────────────────────────┐
                         │  SAM (core repo)              │
                         │  SAM.Core (System.Text.Json,  │
                         │   ToJsonObject, TryGetDateTime),│
                         │  SAM.Analytical (result bags), │
                         │  SAM.Geometry, SAM.Weather     │
                         └───────────────┬───────────────┘
                     built DLLs via HintPath  (..\..\SAM\build\*.dll)
   ┌──────────────────┬───────────────┬──────────────────┬───────────────────────┐
   │                  │               │                  │                       │
┌──▼───────────┐ ┌────▼────────┐ ┌────▼──────┐  ┌────────▼────────┐   ┌──────────▼─────────────┐
│SAM_OpenStudio│ │  SAM_Tas    │ │ SAM_gbXML │  │  SAM_Validation │   │ SAM.Analytical.Benchmark│
│SAM.Analytical│ │SAM.Analytical│ │gbXML.ToSAM│  │ (MSTest exes)   │   │  (NEW, in SAM_Validation│
│  .OpenStudio │ │  .Tas       │ │ + export  │  │                 │   │   repo). netstandard2.0,│
│(netstd2.0)   │ │(Interop.T*) │ │           │  │                 │   │   STJ only, ZERO deps.   │
└──┬───────────┘ └────┬────────┘ └───────────┘  └────────┬────────┘   │  → SAM_Validation\build\ │
   │ ref (HintPath)   │ ref (HintPath)                   │ ProjectRef  └──────────┬─────────────┘
   │  ..\..\..\SAM_Validation\build\SAM.Analytical.Benchmark.dll                  │ (built FIRST)
   └───────────┬──────┴───────────────────────┬──────────┴───────────┬───────────┘
        producer writes DTO           producer writes DTO      comparator + reports read DTO
      (SAM_OpenStudio CLI)            (SAM_Tas CLI, TAS laptop) (SAM_Validation CLI, any laptop)
```

**Invariant:** `SAM.Analytical.Benchmark` has **no upstream SAM or engine dependencies** → builds first, referenced by all three producers/consumers with **no cycle**. Producers depend on it one-directionally; the comparator depends only on it, never on an engine runtime.

### Data flow (two-stage, file-mediated)

```text
[OpenStudio laptop]  SAM model ─native SAM→OS→ EnergyPlus ─eplusout.sql→ SAM results ─→ benchmark-OpenStudio.json
[TAS laptop]         SAM model ─WorkflowCalculator(gbXML)→ TAS (TBD→TSD) ─→ SAM results ─→ benchmark-TAS.json
[B2b optional]       shared gbXML ─OS GbXMLReverseTranslator→ EnergyPlus ─→ benchmark-OpenStudio-gbXML.json
[any laptop]         benchmark-*.json ─→ Comparator ─→ comparison.md / comparison.csv / comparison-summary.json
```

---

## 2. Findings from repository inspection (verified directly)

### 2.1 Dependency & build model
- **Nothing references `SAM_Validation`; SAM core references neither engine repo.** A schema lib in SAM_Validation creates **no cycle**.
- Cross-repo refs use committed built DLLs via `<Reference><HintPath>..\..\<repo>\build\*.dll</HintPath>`. No absolute paths; portability rests on the sibling-repo checkout under `SAM-BIM\`.
- **Build orchestration (verified):** the `SAM-BIM` root is **not** a git repo — it is a container of sibling repos. The root `BuildAll*.csproj`/`BuildAlls*.bat` files are **untracked, local-only** convenience. Real, tracked build order lives in **each repo's `.github/workflows/build.yml`** via a `$buildOrder` array that clones sibling deps at the matching branch and rebuilds them **before** the current repo. SAM_OpenStudio uses `@('SAM','SAM_SQLite', <repo>)`; SAM_Validation uses `@('SAM','SAM_Mollier','SAM_IAPWS', <repo>)`. SAM_Validation also has `test.yml` (`dotnet test`). **SAM_Validation currently produces no `build\` folder** (nothing references it yet).
- `SAM_Validation` today = 3 net8.0 **MSTest-runner exes** (`OutputType=Exe`, `EnableMSTestRunner`), referencing only `SAM.Core`/`SAM.Core.Mollier`. No JSON, no benchmark code, no `SAM.Analytical` ref. Folder idiom `Classes/ Modify/ Variables/ Tests/`. Already has `Modify.Report_RelativeError/AbsoluteError` (near-zero handled) + `Error.Relative=0.01`.

### 2.2 SAM core result model (metric source of truth)
- Results are **enum-keyed parameter bags** (`Result : SAMObject` with `Source`/`Reference`/`DateTime`) on the `AdjacencyCluster` graph. Read via `analyticalModel.GetAnalyticalModelSimulationResults()` and `adjacencyCluster.GetResults<SpaceSimulationResult>(space)`.
- **Whole-model `AnalyticalModelSimulationResult`** (`AnalyticalModelSimulationResultParameter`): `ConsumptionHeating`, `ConsumptionCooling` (label kWh), `PeakHeatingLoad`, `PeakCoolingLoad` (label kW), `PeakHeatingHour`, `PeakCoolingHour` (int hr), `FloorArea` (m²), `Volume` (m³).
- **Per-space `SpaceSimulationResult`** (one per `LoadType`): `Area`, `Volume`, `DesignLoad` (W), `Load` (W) + `LoadIndex` (peak hour-of-year 0–8760), `UnmetHours`/`OccupiedUnmetHours`/`UnmetHourFirstIndex`.
- **NOT available from either route** → explicit non-metrics: **per-space annual energy (kWh)** and **monthly profiles** (only hourly *temperatures* in the TM comfort family; Monthly exists only on the TAS **TPD** plant side).

### 2.3 Units & sign (critical)
- **Load magnitude is W** — confirmed: OS `ToSAM_SpaceSimulationResults` stores `Load = peakLoadKilowatts * 1000.0`.
- **Consumption magnitude ambiguous:** enum labels say kWh; OS `ToSAM` copies the result-set value (reported kWh) into `ConsumptionHeating`; but the legacy `Convert\ToDesignExplorer\File.cs` divides `ConsumptionHeating` by 1000 (implying Wh). **Pin empirically in B1**; store explicit unit tokens; never infer from names.
- **Sign:** no ± convention. Heating/cooling separated by distinct params (model) or `LoadType {Undefined, Cooling, Heating}` (space); magnitudes positive.

### 2.4 Space identity & alignment
- `Space : SAMObject` has stable, read-only `Guid` (round-trips through JSON) + settable `Name`.
- **OpenStudio** keys space results by `space.Guid.ToString("N")`; zone key = `OpenStudioName("IdealLoads", space.Name, space.Guid)`. **TAS** matches TSD `ZoneData`→SAM `Space` **by Name** but attaches to the same SAM `Space` objects (which carry Guids).
- **Both producers start from the same source SAM model** → each emits **Guid + Name** per space; comparator aligns **Guid-first, Name-fallback** (mirrors `AddResults.LookupSpace`). B2b (gbXML round-trip) may not preserve Guids → documented Name/geometry fallback.

### 2.5 Serialization & determinism primitives
- SAM uses **System.Text.Json** (`ToJsonObject()`/`FromJsonObject(JsonObject)` + `_type` discriminator; base in `ParameterizedSAMObject`). **No Newtonsoft.** Numeric wire-format helpers `FormatDecimal.cs`/`FormatFloatingPoint.cs`. TAS timing CSV already uses `CultureInfo.InvariantCulture`.
- `SAM.Core.Query.TryGetDateTime(...)` + `IntervalHourOfYear(DateTime)→0..8759` (EnergyPlus-verified) exist for peak-hour handling.
- `Variables\` const-holder convention established (`SAM.Analytical.HourOfYear.SummerStartIndex=2880`; `SAM.Validation.Error.Relative`).

### 2.6 OpenStudio route (native SAM→OS)
- Main lib **`SAM.Analytical.OpenStudio`** (netstandard2.0; OpenStudio 3.10.0, System.Data.SQLite.Core 1.0.119). **No CLI project.**
- **Headless + cancellable** `OpenStudioSimulationRunner.RunAsync(context, epwPath, outputDirectory, runOptions, execute, IProgress<OpenStudioSimulationProgress>, CancellationToken)`; convenience `AnalyticalModel.ToOpenStudio(...)`/`ToOpenStudioAsync(...)`. Cancellation tree-kills via a Windows Job Object.
- **Results:** `eplusout.sql` → `OpenStudioSimulationResultSet` → `Convert\ToSAM\SimulationResults.cs`: `ToSAM(model)` → `AnalyticalModelSimulationResult` (Reference = model Guid "N"; `TotalAnnualHeating/Cooling`, `PeakHeating/CoolingLoadTotal`, `PeakHeating/CoolingHourTotal`, FloorArea/Volume summed); `ToSAM_SpaceSimulationResults(...)` → per-space keyed by `space.Guid("N")`, `Load = kW×1000` (W), `LoadIndex`, `UnmetHours`. `Modify\AddResults.cs`: `adjacencyCluster.AddResults(sqlPath, out diagnostics)`. `Query.Source()` = provenance string.
- **Ideal Loads** (`Convert\ToOpenStudio\IdealLoads.cs`) gated by `options.AssignIdealLoads` + `space.IsConditioned()`.
- **Metadata:** `Core.OpenStudio.Query.OpenStudioVersion()`/`EnergyPlusVersion()`; weather path; design-day source diagnostics (DDY/embedded/none); `RuntimeSeconds`; warning/severe/fatal counts from `eplusout.err`.
- **Tests:** NUnit 4.2.2, folders `M0–M7`/`C1–C8` + `Fixtures\AnalyticalModelFixtures.cs` (`SingleBox()`). Live E+ tests `[Category("Simulation")]`. Weather repo-relative. **Offline synthetic-SQLite tests** (`C5\AddResultsBySqlTests.cs`) exercise the full mapping without EnergyPlus.

### 2.7 TAS route (native SAM→gbXML→TAS)
- Main lib **`SAM.Analytical.Tas`** (netstandard2.0). TAS interop = COM PIAs **committed** in `references_buildonly\Interop.T*.dll` (`EmbedInteropTypes`), found via registry `HKCU\Software\EDSL\TasManager`. → **Builds anywhere; runs only where TAS COM servers are registered.** Already refs `SAM.Analytical.gbXML`.
- **Reusable headless core = `WorkflowCalculator.Calculate(AnalyticalModel)`** (synchronous, GUI-free; events opt-in; **no CancellationToken**). Config via `WorkflowSettings { Path_gbXML, Path_TBD, WeatherData, DesignDays_*, AddIZAMs, Sizing, Simulate, UnmetHours, … }`. The GH `SAMAnalyticalWorkflowgbXML` and WinForms `Modify.RunWorkflow` are UI wrappers to **bypass**.
- Sequence: import gbXML→T3D → build TBD (weather, design days, IZAMs, adiabatic, zones) → `Sizing` → `Simulate`→TSD → `AddResults(path_TSD, adjacencyCluster)` → `UnmetHours` → `UpdateDesignLoads` (from `zone.maxHeating/CoolingLoad`).
- **Mapping:** `Modify.AddResults`→`Convert.ToSAM_Results` builds `SpaceSimulationResult`/`SurfaceSimulationResult`/`ZoneSimulationResult`; space matched by **Name**; design loads onto `Space` via `SpaceParameter.DesignHeating/CoolingLoad`.
- **⚠️ Model annual energy (`ConsumptionHeating/Cooling`) is NOT populated by the gbXML workflow** — only by `Convert\ToSAM\AnalyticalModelSimulationResult.cs` (invoked by GH `TasTSDAddBuildingResults`). **The producer must invoke this building-results read explicitly.**
- **Conditioning:** idealised via thermostats + **IZAMs** (`Modify.UpdateIZAMs`) + **TBD sizing** (`Query.Sizing`). Production HVAC = separate **TPD** path (later informational only).
- **Metadata:** duration via `Timings`/`.timing.csv`; weather/design-days from settings. **TAS engine version NOT available in code** → needs a helper (read TAS exe FileVersion from install dir) or a documented manual field. **Warnings minimal** (GH messages only) → capture `successful` + exceptions.
- **Tests: NONE exist** in SAM_Tas. Greenfield. Reading a `.tsd` needs the TAS COM server → TSD-based tests are TAS-laptop-only; only **pure-managed** tests (hand-built SAM results → DTO) are portable.

### 2.8 gbXML / B2b reality
- **`GbXMLReverseTranslator` is NOT used anywhere in SAM_OpenStudio** (only `EnergyPlusReverseTranslator` for DDY). B2b is **greenfield**.
- **OpenStudio 3.10 SDK exposes `GbXMLReverseTranslator` + `GbXMLForwardTranslator`** (confirmed via DLL symbols) — feasible but unproven here.
- **`SAM_gbXML`** has `gbXMLSerializer.gbXML.ToSAM() → AnalyticalModel` (reverse) + forward export; SAM_Tas already uses it. → the **same gbXML** can feed both engines, isolating pure engine differences.

### 2.9 Corpus assets
- **`SAM_Samples`** repo (own git, `sow/2026-Q3`): `Tas/Tas.zip`, `Revit/000000_SAM_Live.zip`. OS `AnalyticalModelFixtures` + repo-relative TMYx weather exist → reuse first.

### 2.10 Metric availability matrix

| Metric | OpenStudio native | TAS native (gbXML) | Neutral schema |
|---|---|---|---|
| Model annual heating/cooling | ✅ `ToSAM` | ⚠️ needs explicit building-results read | ✅ nullable |
| Model peak heating/cooling load | ✅ coincident-total | ✅ building-profile max | ✅ (semantic caveat) |
| Model peak heating/cooling hour | ✅ 0–8759 | ✅ 0–8759 | ✅ circular compare |
| Model floor area / volume | ✅ | ✅ | ✅ |
| Space area / volume | ✅ | ✅ | ✅ |
| Space peak load (W) + hour | ✅ `Load`/`LoadIndex` | ✅ | ✅ |
| Space design load (sizing, W) | ⚠️ peak proxy only | ✅ `DesignLoad` (sizing) | ✅ nullable (caveat) |
| Space unmet hours | ✅ | ✅ | ✅ nullable |
| **Per-space annual energy** | ❌ | ❌ | ❌ EXCLUDED |
| **Monthly profiles** | ❌ | ❌ (TPD only) | ❌ EXCLUDED |

---

## 3. Resolved architecture decisions

| # | Decision | Resolution | Rationale |
|---|----------|------------|-----------|
| D1 | Schema shape | **Independent, versioned DTOs** (`SAM.Analytical.Benchmark`, netstandard2.0, STJ-only, zero deps). | Result types are enum-keyed graph bags with misleading unit labels; comparator must not drag in `SAM.Analytical`/engines. Independent DTOs = clean isolation + explicit versioned wire contract. |
| D2 | Schema location | **Standalone lib in the `SAM_Validation` repo** (stakeholder-confirmed), output to `SAM_Validation\build\`. | Honours programme decision #1; versioned alongside its comparator. Build-order handled in per-repo CI (see D3-build below), **not** a SAM PR. |
| D3 | Invocation | **Three thin console CLIs** (`benchmark-openstudio`, `benchmark-tas`, `benchmark-compare`) over **one shared CLI host** in the schema lib; extensible. | A single cross-engine exe is impossible under isolation rules. Shared host = arg parsing, invariant-culture JSON I/O, stable exit codes, written once. |
| D3-build | Build order | B1a: schema project outputs to `SAM_Validation\build\` + is added to SAM_Validation's solution (its CI already builds the solution). B1b/B2: each engine repo's **own** `.github/workflows/build.yml` gains a step to clone `SAM_Validation` @ matching branch and build **only** `SAM.Analytical.Benchmark.csproj` → `SAM_Validation\build\`. Root `BuildAlls*` (untracked) updated locally only. | Verified: build order is tracked per-repo in CI, not in SAM nor the untracked root. Keeps each change inside the repo that needs it; no implicit cross-repo edits. |
| D4 | Canonical units | Schema stores **explicit unit tokens**; producers convert at emit; **pin consumption magnitude empirically in B1**. | Enum labels contradict the legacy consumer; requirement forbids name-based inference. |
| D5 | Space alignment | **Guid-first, Name-fallback**; explicit diagnostics for missing/duplicate/unmatched. | Matches `AddResults.LookupSpace`; producers share source-model Guids. |
| D6 | Primary metric set | Model: annual htg/clg, peak htg/clg load + hour, floor area, volume. Space: area, volume, design/peak load + hour, unmet hours. **Exclude** per-space annual energy & monthly profiles. | Only these present from both routes. |
| D7 | B2b | **Optional milestone, spike-gated** (SDK `GbXMLReverseTranslator`), after B3, results tagged `route=SharedGbXML-OpenStudio`. | Only this route isolates engine-vs-translator differences; greenfield → prove before build. |
| D8 | Tolerances | `±5%` (warn)/`±15%` (fail) as **configurable, clearly-labelled provisional bands** + near-zero absolute floor. Never presented as validated. | Requirement forbids scientific claims pre-corpus. |
| D9 | TAS cancellation | **Do not** add async/cancellation to SAM_Tas. Producer is synchronous; hard process timeout only. | Adding async across SAM_Tas is a large out-of-scope API change; batch benchmark tolerates synchronous runs. |
| D10 | Conditioning equivalence | In B0, document OS Ideal Loads ↔ TAS (thermostats + IZAMs + TBD sizing) as the **candidate** pairing. **Freeze it only after** the first TAS `SingleBox` run confirms meaningful conditioning, annual energy and loads (checkpoint in B2). | Freezing pre-validation risks encoding an invalid comparison into B1/B2. |
| D11 | B5 host | **Undetermined until B5 kickoff.** Do not pre-select the GH plugin/repo in an earlier milestone. Constraint: the host must not depend on any engine assembly. | Avoids premature, wrong coupling; decided with full B5 context. |

---

## 4. Final B0–B6 milestone table

All branches are created **from `sow/2026-Q3`** and PRs merge back **into `sow/2026-Q3`** (never `master`).

| MS | Objective | Repository | Feature branch | Depends |
|----|-----------|------------|----------------|---------|
| **B0** | Methodology & contracts doc | SAM_Validation | `feature/benchmark-b0-methodology` | — |
| **B1a** | Neutral schema + serializer + shared CLI host | SAM_Validation | `feature/benchmark-b1-schema` | B0 |
| **B1b** | OpenStudio producer CLI + empirical unit pin | SAM_OpenStudio | `feature/benchmark-b1-os-producer` | B1a |
| **B2** | TAS producer CLI (+ conditioning-equivalence checkpoint) | SAM_Tas | `feature/benchmark-b2-tas-producer` | B1a |
| **B3** | Headless comparator CLI + md/csv/summary reports | SAM_Validation | `feature/benchmark-b3-comparator` | B1a |
| **B4** | Benchmark corpus | SAM_Samples | `feature/benchmark-b4-corpus` | B1b, B3 |
| **B2b** | *Optional, spike-gated:* shared-gbXML → OpenStudio | SAM_OpenStudio | `feature/benchmark-b2b-shared-gbxml` | B1b, B3 |
| **B5** | Grasshopper presentation component | *selected GH repo (decided at B5 kickoff)* | `feature/benchmark-b5-gh-viewer` | B3 |
| **B6** | Documentation + first validated report | SAM_Validation | `feature/benchmark-b6-docs-report` | B2, B3, B4 |

If B1a's build-order verification finds the change must touch a repo not listed for that milestone, create a dedicated `feature/benchmark-b1-build-order` branch **in that repo** and open a separate PR — never edit another repo implicitly.

---

## 5. Detailed implementation steps per milestone

> Every implementation session must begin with the **Mandatory Git & repository rules** (§13) and verify that named files live in the stated repo before editing.

### B0 — Methodology & contracts (docs only)
- **Objective:** freeze the engine-neutral contract: metric definitions, canonical units, sign, tolerance bands, provenance fields (incl. dual hashes §7), missing-data semantics (`available`/`null` invariant), space-alignment rules, the four routes. Record the **candidate** conditioning pairing (D10) — explicitly *not yet frozen*.
- **Repo/branch:** `SAM_Validation` / `feature/benchmark-b0-methodology`. Files: `docs/benchmark/{METHODOLOGY,METRICS,SCHEMA,TOLERANCES,GLOSSARY}.md`.
- **Reused APIs:** none (doc).
- **Public API boundary:** none. Documents the contract B1 encodes.
- **JSON ownership/versioning:** declares `schemaVersion` semver policy (**v1.0.0**) + compatibility (equal major required; minor drift warns) and `canonicalizationVersion`.
- **Invocation/tests:** N/A (link-check). **Human review** by an energy modeller of metric semantics, the peak-load caveat, the consumption-unit open item, and the candidate conditioning pairing.
- **Acceptance:** every §2.10 metric has definition + canonical unit + both-route source; excluded metrics listed; near-zero + peak-hour circularity rules stated; conditioning pairing labelled *candidate, pending B2 validation*.
- **Non-goals:** no code; no frozen conditioning contract; no tolerance validation.
- **Commit boundaries:** (1) methodology+glossary; (2) metrics+units; (3) tolerances+schema-spec.
- **Rollback:** revert doc PR.
- **Risks:** semantic ambiguity → mark design-vs-peak load and consumption units *provisional, pinned in B1*.

### B1a — Neutral schema + serializer + shared CLI host
- **Objective:** encode the v1 schema + deterministic serializer + shared CLI host in `SAM.Analytical.Benchmark`, output to `SAM_Validation\build\`.
- **Repo/branch:** `SAM_Validation` / `feature/benchmark-b1-schema`. New `SAM_Validation\SAM.Analytical.Benchmark\SAM.Analytical.Benchmark.csproj` (netstandard2.0, `System.Text.Json` 8.0.5, **no SAM/engine refs**, OutputPath → `..\build\` mirroring SAM.Core convention), namespace `SAM.Analytical.Benchmark`.
  - `Classes\` DTOs (§7): `BenchmarkDocument`, `BenchmarkProvenance`, `EngineInfo`, `WeatherInfo`, `ModelResult`, `SpaceResult`, `LoadTypeResult`, `MetricValue` (**`double? Value`, `Unit`, `bool Available`**), enums `BenchmarkRoute`, `EngineKind`, `MetricKey`, `Unit`, `RunState`.
  - `Query\`: `Serializer` (STJ; sorted keys via explicit `JsonObject` build; `InvariantCulture`; full finite `double` precision without result rounding), `SchemaVersion` const, `Validate(BenchmarkDocument)` — enforces the availability invariant.
  - `Cli\`: `BenchmarkCli.Run(string[] args, Func<BenchmarkArgs,int> body)`, `BenchmarkArgs` (`--model/--weather/--out/--route/--tolerance-profile`), invariant I/O, exit codes (`0 ok / 2 usage / 3 io / 4 engine`).
  - `Variables\`: `Tolerances` (`Warn=0.05`, `Fail=0.15`, `NearZeroFloor`), `SchemaVersionInfo`.
  - Test project `SAM_Validation\SAM_Analytical_Benchmark_Tests\` (net8.0 MSTest): round-trip determinism, invariant culture, key ordering, **availability invariant (available⇒non-null; unavailable⇒null; real 0⇒0+available)**, schema-version gate.
  - **Solution + CI:** add the schema project (and its test project) to SAM_Validation's solution so `build.yml` builds it and the DLL lands in `SAM_Validation\build\`.
- **Reused APIs:** none upstream (isolation). Serializer mirrors SAM's invariant-culture wire-format intent.
- **Public API boundary:** DTOs + `Serializer` + `BenchmarkCli` (stable, versioned).
- **JSON ownership/versioning:** owns the schema (`schemaVersion="1.0.0"`).
- **Invocation:** `dotnet test` (MSTest).
- **Acceptance:** builds; DLL in `SAM_Validation\build\`; byte-identical re-serialize; availability invariant enforced by `Validate` + tests.
- **Non-goals:** no producers, comparator, reporting.
- **Dependencies:** B0.
- **Commit boundaries:** (1) DTOs + `MetricValue` invariant; (2) serializer + validation + tests; (3) CLI host; (4) solution/CI + build-output wiring.
- **Rollback:** self-contained new project; revert PR.
- **Risks:** netstandard2.0 STJ sorted-key determinism → build ordering explicitly, cover with re-serialize test.

### B1b — OpenStudio producer CLI
- **Objective:** emit `benchmark-OpenStudio.json` (`route="Native-OpenStudio"`) reusing the headless OS route; **empirically pin the consumption unit**.
- **Repo/branch:** `SAM_OpenStudio` / `feature/benchmark-b1-os-producer`. New `SAM_OpenStudio\benchmark\SAM.Analytical.OpenStudio.Benchmark\` (net8.0, `OutputType=Exe`), namespace `SAM.Analytical.OpenStudio.Benchmark`.
  - `Modify\ToBenchmark.cs`: `BenchmarkDocument ToBenchmark(this AnalyticalModel, OpenStudioBenchmarkContext)` — reads model + space results, maps to DTOs (Guid+Name per space).
  - `Classes\OpenStudioBenchmarkContext.cs`: engine/weather/design-day/route metadata (`OpenStudioVersion()`, `EnergyPlusVersion()`, epw hash, `RuntimeSeconds`, warning counts).
  - `Program.cs`: `BenchmarkCli.Run` → load model → `ToOpenStudio(epw, outDir, runOptions{AssignIdealLoads=true})` (or `RunAsync`+timeout) → `ToBenchmark` → write JSON.
  - HintPath ref `..\..\..\SAM_Validation\build\SAM.Analytical.Benchmark.dll` (three `..`: the producer project sits three levels deep, matching the repo's existing `..\..\..\SAM\build\SAM.Core.dll` references).
  - **CI (`build.yml`):** add a step to clone `SAM_Validation` @ matching branch and `msbuild` **only** `SAM.Analytical.Benchmark.csproj` → `SAM_Validation\build\` before building SAM_OpenStudio.
  - NUnit tests (`benchmark\` folder): **offline** producer test on a synthetic-SQLite model (reuse `C5`) → committed golden JSON; one `[Category("Simulation")]` live end-to-end on `SingleBox()` that also **captures the raw SQL consumption value to pin the unit**.
- **Reused APIs:** `ToOpenStudio/ToOpenStudioAsync`, `OpenStudioSimulationRunner`, `Convert.ToSAM`/`ToSAM_SpaceSimulationResults`, `AddResults`, `Query.Source()`, `OpenStudioVersion/EnergyPlusVersion`, `IntervalHourOfYear`.
- **Public API boundary:** `ToBenchmark` (OS-internal) + exe. No change to existing OS public APIs.
- **JSON ownership/versioning:** consumes B1a schema; stamps engine + `route`.
- **Invocation:** `benchmark-openstudio --model model.json --weather x.epw --out benchmark-OpenStudio.json`.
- **Acceptance:** offline golden passes anywhere; live `SingleBox()` emits valid v1 doc; **consumption unit pinned & documented**; C1–C7 green.
- **Non-goals:** no comparator/TAS/B2b/reporting.
- **Dependencies:** B1a.
- **Commit boundaries:** (1) CI clone+build-schema step; (2) `ToBenchmark`+context; (3) `Program.cs`+offline test+golden; (4) live sim test + unit pin.
- **Rollback:** additive exe; revert PR.
- **Risks:** consumption-unit ambiguity → pin is a gating commit; BuildAlls/CI ordering → clean full build gate.

### B2 — TAS producer
- **Objective:** emit `benchmark-TAS.json` (`route="Native-TAS"`) from the native TAS route; buildable on dev laptop, runnable on TAS laptop; **validate the candidate conditioning pairing (D10)**.
- **Repo/branch:** `SAM_Tas` / `feature/benchmark-b2-tas-producer`. New `SAM_Tas\benchmark\SAM.Analytical.Tas.Benchmark\` (net8.0-windows, `OutputType=Exe`), namespace `SAM.Analytical.Tas.Benchmark`.
  - `Modify\ToBenchmark.cs`: `BenchmarkDocument ToBenchmark(this AnalyticalModel, TasBenchmarkContext)`.
  - `Classes\TasBenchmarkContext.cs`: TAS provenance (`Query\TasVersion.cs` reading TAS exe FileVersion from the registry install dir; weather/design-day identity; `Timings` duration; `successful`/exception capture).
  - `Program.cs`: `BenchmarkCli.Run` → load model → ensure shared gbXML (via `SAM.Analytical.gbXML` export if absent) → `WorkflowSettings{ AddIZAMs=true, Sizing=true, Simulate=true, UnmetHours=true, … }` → `new WorkflowCalculator(settings).Calculate(model)` → **explicit building-results read** for annual energy → `ToBenchmark` → write JSON. Watchdog hard-timeout (no cancellation).
  - HintPath ref `..\..\..\SAM_Validation\build\SAM.Analytical.Benchmark.dll` (three `..`: the producer project sits three levels deep, matching the repo's existing `..\..\..\SAM\build\SAM.Core.dll` references).
  - **CI (`build.yml`):** same clone + build-only-schema step as B1b.
  - **Docs:** `SAM_Tas\benchmark\README.md` — TAS-laptop prerequisites (licensed EDSL install, registry keys), CLI, outputs.
- **Reused APIs:** `WorkflowCalculator.Calculate`, `WorkflowSettings`, `Modify.AddResults`, `Query.Sizing`, `Query.UnmetHours`, `Modify.UpdateDesignLoads`, `Convert.ToSAM` (building results), `SAM.Analytical.gbXML` export, `Query.Source()`.
- **Public API boundary:** `ToBenchmark` (TAS-internal) + exe. **No new async/cancellation; no changes to existing SAM_Tas public APIs.**
- **JSON ownership/versioning:** consumes B1a schema unchanged.
- **Invocation (TAS laptop only):** `benchmark-tas --model model.json --gbxml model.xml --weather x.epw --tbd out.tbd --out benchmark-TAS.json`.
- **Tests:** **Portable (no TAS):** MSTest/NUnit asserting `ToBenchmark` maps a **hand-built** `AnalyticalModel` (results set in-memory, no COM) to a golden JSON; build-smoke. **Human-validation (TAS laptop, marked):** run on fixture gbXML; confirm valid JSON, non-zero loads, populated provenance. **Conditioning checkpoint:** compare TAS `SingleBox` outputs against expectations to confirm the D10 pairing before it is frozen.
- **Acceptance:** builds on dev laptop; portable golden passes anywhere; documented TAS-laptop run produces schema-valid JSON with model annual energy present; D10 pairing validated & recorded; no new public API.
- **Non-goals:** no TPD/production HVAC; no async; no live TAS in CI.
- **Dependencies:** B1a schema.
- **Commit boundaries:** (1) CI clone+build-schema + project + build-smoke; (2) `ToBenchmark`+context+`TasVersion`; (3) `Program.cs`+building-results+timeout; (4) portable golden; (5) README + conditioning-checkpoint notes.
- **Rollback:** additive exe; revert PR.
- **Risks:** annual energy omitted by default → dedicated wiring + asserted golden; TAS version unavailable → helper + manual-override field; partial headlessness → call `Calculate` directly, `net8.0-windows`; space-by-Name upstream → emit source-model Guid.

### B3 — Comparator + reports
- **Objective:** headless comparator reading two benchmark JSONs; align model + spaces; abs/rel diffs; tolerance bands; emit `comparison.md`, `comparison.csv`, `comparison-summary.json` + gate status. **Independent of both engines.**
- **Repo/branch:** `SAM_Validation` / `feature/benchmark-b3-comparator`. New `SAM_Validation\SAM.Analytical.Benchmark.Compare\` (net8.0, `OutputType=Exe`; refs schema lib only), namespace `SAM.Analytical.Benchmark.Compare`.
  - `Classes\`: `ComparisonResult`, `MetricComparison` (abs, rel, band, availability), `SpaceComparison`, `SpaceMatchDiagnostics`, `ToleranceProfile`.
  - `Query\`: `Compare(tas, os, ToleranceProfile)`, `AlignSpaces(...)` (Guid-first, Name-fallback), `Band(value, other, profile)` (abs+rel, near-zero floor; **either side unavailable/null ⇒ "N/A", never fail**), `CircularHourDiff(a,b)`, `GateStatus(...)`.
  - `Modify\`: `WriteMarkdown`, `WriteCsv`, `WriteSummaryJson` (deterministic; invariant culture).
  - `Program.cs`: `BenchmarkCli.Run` → load both → schema-version check → `Compare` → write reports.
  - MSTest `SAM.Analytical.Benchmark.Compare.Tests\` — **fabricated** pairs: exact, within-warn, within-fail, beyond-fail, near-zero, **null/unavailable metric**, missing/duplicate/split space, peak-hour year-boundary, unit-mismatch guard, model-total vs sum-of-spaces reconciliation, schema-version drift.
- **Reused APIs:** schema `Serializer`/DTOs; extend `SAM.Validation.Modify.Report_*` band logic into non-assert computation; `SAM.Core.Query.Round`.
- **Public API boundary:** `Compare` + report writers + `ToleranceProfile`. Reports = external contract for B5/B6.
- **JSON ownership/versioning:** `comparison-summary.json` comparator-owned (`summarySchemaVersion="1.0.0"`).
- **Invocation:** `benchmark-compare --tas benchmark-TAS.json --openstudio benchmark-OpenStudio.json --out ./report --tolerance-profile default`.
- **Tests:** all portable; human review of one rendered `comparison.md`.
- **Acceptance:** byte-stable reports; missing/dup/split spaces diagnosed; unavailable⇒N/A; circular hours handled; totals reconciled; gate from configurable bands; **no engine assembly referenced** (CI ref-check).
- **Non-goals:** no simulation/GH; no threshold validation.
- **Dependencies:** B1a.
- **Commit boundaries:** (1) classes + `Compare`/`AlignSpaces`; (2) banding + circular-hour + reconciliation; (3) report writers; (4) `Program.cs`; (5) fabricated tests.
- **Rollback:** self-contained; revert PR.
- **Risks:** non-determinism → sorted keys + invariant culture + full finite `double` precision without result rounding + byte-stability test.

### B4 — Benchmark corpus
- **Objective:** graduated **engine-independent** source models (single→multi-zone→reviewed production), separate from generated artefacts.
- **Repo/branch:** `SAM_Samples` / `feature/benchmark-b4-corpus`. Layout: `benchmark/models/{01-single-zone,02-two-zone,03-multistorey,04-production}/model.json` + per-model `README.md`; `.gitignore` for `**/generated/**` (tbd/tsd/osm/sql/gbxml).
- **Reused assets:** export OS `AnalyticalModelFixtures.SingleBox()` to `01-single-zone/model.json`; evaluate `SAM_Samples\Revit\000000_SAM_Live.zip` + `Tas\Tas.zip` for the production model (human review).
- **Public API boundary:** none. Stable Space Guids authored once, reused by both engines.
- **JSON ownership:** source `model.json` = SAM `AnalyticalModel` serialization (not benchmark docs). Generated engine files never committed (goldens excepted).
- **Invocation:** producers point at `benchmark/models/**/model.json`.
- **Tests:** each synthetic model runs through the OS producer offline path in CI (schema-valid). Production model **human-reviewed**.
- **Acceptance:** ≥3 synthetic models with stable Guids; production model selected + reviewed; generated artefacts git-ignored; each documented.
- **Non-goals:** no tolerance calibration; no large committed generated files.
- **Dependencies:** B1b, B3.
- **Commit boundaries:** (1) single/two-zone; (2) multistorey; (3) production + review notes; (4) .gitignore + docs.
- **Rollback:** data-only; revert PR.
- **Risks:** licensing/PII → review+strip; large files → SAM_Samples LFS.

### B2b — Shared-gbXML → OpenStudio (optional, spike-gated)
- **Objective:** run OpenStudio from the **same gbXML** TAS consumes via SDK `GbXMLReverseTranslator`; results separately identified.
- **Repo/branch:** `SAM_OpenStudio` / `feature/benchmark-b2b-shared-gbxml`.
  - **Spike (gate):** `tests\…\B2b\GbXmlReverseSpikeTests.cs` `[Category("Simulation")]` — `new OpenStudio.GbXMLReverseTranslator().loadModel(path)`, confirm runnable `Model`, run via `OpenStudioSimulationRunner`, assert non-zero loads. **If it fails, stop** and record findings.
  - **On success:** `Convert\FromGbXML\Model.cs` (`FromSharedGbXML(path, out diagnostics)`); OS producer gains `--route shared-gbxml` → `benchmark-OpenStudio-gbXML.json` (`route="SharedGbXML-OpenStudio"`). Space alignment falls back to Name/geometry (Guids not preserved) — documented.
- **Reused APIs:** SDK `GbXMLReverseTranslator`, `OpenStudioSimulationRunner`, producer `ToBenchmark`.
- **Public API boundary:** additive route flag + `FromGbXML`. No change to native route.
- **JSON ownership/versioning:** same schema; distinct `route`.
- **Invocation:** `benchmark-openstudio --route shared-gbxml --gbxml shared.xml --weather x.epw --out benchmark-OpenStudio-gbXML.json`.
- **Tests:** spike (live, marked); offline mapping test if persistable; human review of first shared-gbXML comparison.
- **Acceptance:** spike documented; if built, docs schema-valid + clearly route-tagged.
- **Non-goals:** not a native-route replacement; no gbXML authoring.
- **Dependencies:** B1b, B3, corpus gbXML (B4).
- **Commit boundaries:** (1) spike+findings; (2) `FromGbXML`; (3) route flag+tests.
- **Rollback:** feature-flagged route; revert PR.
- **Risks:** reverse translator may drop constructions/loads → spike is the gate; strictly optional.

### B5 — Grasshopper presentation
- **Objective:** lightweight GH component that **loads completed benchmark/comparison JSON** and presents results; requires **neither engine**; does **not** re-implement comparison.
- **Repo/branch:** **host repo decided at B5 kickoff** (D11); branch `feature/benchmark-b5-gh-viewer`. Constraint: refs **only** the schema lib + B3 comparator lib, **never** an engine assembly.
  - Components: `BenchmarkLoad` (path → `BenchmarkDocument`), `BenchmarkCompareView` (two docs → `ComparisonResult` via B3 `Compare`), `BenchmarkReportOpen`. Reuse `GH_SAMVariableOutputParameterComponent`.
- **Reused APIs:** schema `Serializer`, B3 `Compare`/report writers.
- **Public API boundary:** GH components only.
- **Invocation:** interactive in Rhino/GH (**marked: requires Rhino/GH**).
- **Tests:** headless unit test of load/compare (portable); manual GH canvas smoke.
- **Acceptance:** loads a comparison with no engine installed; delegates comparison to B3.
- **Non-goals:** not the primary comparator; no simulation triggering.
- **Dependencies:** B3.
- **Commit boundaries:** (1) load; (2) compare-view; (3) report-open.
- **Rollback:** GH plugin addition; revert PR.
- **Risks:** accidental engine coupling → CI ref-check restricting references.

### B6 — Documentation + first validated report
- **Objective:** end-to-end docs + the first real comparison on the production model across both laptops.
- **Repo/branch:** `SAM_Validation` / `feature/benchmark-b6-docs-report`. `docs/benchmark/{INSTALL,RUN-OPENSTUDIO,RUN-TAS,TRANSFER,COMPARE,INTERPRET,SHARED-GBXML,REPRODUCIBILITY}.md`; `docs/benchmark/reports/first-validated/`.
- **Reused APIs:** all prior CLIs.
- **Invocation:** documented commands from B1b/B2/B3 (+ B2b if built).
- **Tests:** link-check; **human-validation** sign-off; reproducibility (same inputs → byte-identical benchmark docs; stable comparison).
- **Acceptance:** a technician can run both laptops + compare + interpret from docs alone; first report committed with full provenance; limitations stated (provisional tolerances, excluded metrics, peak-load/consumption caveats).
- **Non-goals:** no threshold finalisation; no CI TAS execution.
- **Commit boundaries:** (1) install+run; (2) transfer+compare+interpret; (3) first report+provenance; (4) reproducibility.
- **Rollback:** docs/data; revert PR.
- **Risks:** cross-laptop path drift → relative layouts + portable JSON; provenance mismatch → comparator surfaces it.

---

## 6. Proposed branch & PR sequence

All branches created from `sow/2026-Q3`; all PRs target `sow/2026-Q3`.

| Order | PR | Repo | Branch |
|---|---|---|---|
| 1 | B0 | SAM_Validation | `feature/benchmark-b0-methodology` |
| 2 | B1a | SAM_Validation | `feature/benchmark-b1-schema` |
| 3 | B1b | SAM_OpenStudio | `feature/benchmark-b1-os-producer` |
| 4 | B2 | SAM_Tas | `feature/benchmark-b2-tas-producer` |
| 5 | B3 | SAM_Validation | `feature/benchmark-b3-comparator` |
| 6 | B4 | SAM_Samples | `feature/benchmark-b4-corpus` |
| 7 | B2b (spike then build) | SAM_OpenStudio | `feature/benchmark-b2b-shared-gbxml` |
| 8 | B5 | *selected GH repo* | `feature/benchmark-b5-gh-viewer` |
| 9 | B6 | SAM_Validation | `feature/benchmark-b6-docs-report` |

Ordering rule: **B1a first**; B1b/B2/B3 parallelisable after B1a (B2 also gates the D10 conditioning freeze); B4 after B1b; B5/B6 after B3 (B6 also after B2+B4). **One milestone per session.**

---

## 7. Benchmark schema outline (v1.0.0)

`benchmark-<engine>[-<route>].json` — serialized by `SAM.Analytical.Benchmark.Query.Serializer` (System.Text.Json, invariant culture, sorted keys, full finite `double` precision without result rounding).

**Availability invariant (enforced by `Validate`):** `available=true ⇒ value` non-null; `available=false ⇒ value` **null**; a real measured zero is `value:0, available:true`. Unavailable metrics **never** carry `0`.

```jsonc
{
  "schemaVersion": "1.0.0",
  "provenance": {
    "sourceModelName": "…", "sourceModelGuid": "…",
    "sourceFileHash": "sha256:…",          // hash of the exact input model file
    "canonicalModelHash": "sha256:…",      // hash of the canonical SAM representation
    "canonicalizationVersion": "1.0.0",    // how the canonical hash was produced
    "samCommit": "…", "runnerCommit": "…",
    "engine": { "kind": "OpenStudio|TAS", "name": "EnergyPlus", "version": "24.x", "sdkVersion": "3.10.0" },
    "route": "Native-OpenStudio|Native-TAS|SharedGbXML-OpenStudio",
    "weather": { "identity": "USA_MA_Boston…TMYx", "hash": "sha256:…" },
    "designDaySource": "DDY|EmbeddedModel|None",
    "runTimestampUtc": "2026-07-21T…Z", "durationSeconds": 12.3,
    "state": "Success|Failure", "warnings": ["…"], "notes": ["consumption unit pinned = kWh"]
  },
  "model": {
    "consumptionHeating": { "value": 1234.5, "unit": "kWh", "available": true },
    "consumptionCooling": { "value":  678.9, "unit": "kWh", "available": true },
    "peakHeatingLoad":    { "value":   12.3, "unit": "kW",  "available": true },
    "peakHeatingHour":    { "value":  205,   "unit": "hourOfYear", "available": true },
    "peakCoolingLoad":    { "value":    9.8, "unit": "kW",  "available": true },
    "peakCoolingHour":    { "value":   4602, "unit": "hourOfYear", "available": true },
    "floorArea":          { "value":  240.0, "unit": "m2",  "available": true },
    "volume":             { "value":  720.0, "unit": "m3",  "available": true }
  },
  "spaces": [
    {
      "guid": "…32hex…", "name": "Office 1",
      "area":   { "value": 40.0,  "unit": "m2", "available": true },
      "volume": { "value": 120.0, "unit": "m3", "available": true },
      "heating": { "designLoad": {"value":2100,"unit":"W","available":true},
                   "peakLoad":   {"value":2000,"unit":"W","available":true},
                   "peakHour":   {"value":210,"unit":"hourOfYear","available":true},
                   "unmetHours": {"value":3,"unit":"h","available":true} },
      "cooling": { "designLoad": {"value":null,"unit":"W","available":false}, "…": "…" }
    }
  ]
}
```

- **Units enum:** `kWh, kW, W, Wh, m2, m3, hourOfYear, h`. Consumption unit pinned in B1b; loads confirmed W.
- **Excluded (documented):** per-space annual energy; monthly profiles.
- **`comparison-summary.json`** (comparator-owned, separate version): per-metric abs/rel/band, per-space alignment diagnostics, totals reconciliation, both inputs' provenance, overall gate.

---

## 8. Test matrix by machine / environment

| Test | Dev laptop (no engine) | OpenStudio laptop | TAS laptop | Rhino/GH | Human |
|---|---|---|---|---|---|
| Schema round-trip / determinism / **availability invariant** | ✅ | ✅ | ✅ | — | — |
| CLI host arg/exit-code | ✅ | ✅ | ✅ | — | — |
| OS producer offline (synthetic SQLite golden) | ✅ | ✅ | — | — | — |
| OS producer live end-to-end (`SingleBox`, `[Simulation]`) | — | ✅ | — | — | — |
| Consumption-unit empirical pin | — | ✅ | — | — | ✅ |
| TAS producer build-smoke | ✅ | ✅ | ✅ | — | — |
| TAS producer portable mapping golden (hand-built results) | ✅ | ✅ | ✅ | — | — |
| TAS producer live run + **D10 conditioning checkpoint** | — | — | ✅ | — | ✅ |
| Comparator fabricated pairs (all bands/edge cases + null) | ✅ | ✅ | ✅ | — | — |
| Report byte-stability | ✅ | ✅ | ✅ | — | — |
| B2b reverse-translate spike (`[Simulation]`) | — | ✅ | — | — | ✅ |
| GH viewer load/compare (headless unit) | ✅ | ✅ | — | — | — |
| GH viewer canvas smoke | — | — | — | ✅ | ✅ |
| First validated cross-laptop report | — | ✅ | ✅ | — | ✅ |
| Existing C1–C7 regression | ✅ | ✅ (Simulation subset) | — | — | — |

---

## 9. Risks & mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Consumption unit (Wh vs kWh) ambiguity | Wrong model-energy comparisons | Empirical pin in B1b; explicit unit tokens; provenance note; comparator refuses mismatched units. |
| Peak-load semantics differ (OS coincident-total vs TAS building-profile max) | False "fail" bands | Document in B0; label semantics; informational until corpus supports thresholds. |
| TAS model annual energy not populated by default | Missing model metric | Producer explicitly invokes building-results conversion; asserted in golden. |
| TAS: no engine version / weak warnings | Incomplete provenance | `TasVersion` helper + manual-override field; capture `successful`+exceptions; document limitation. |
| TAS not cancellable / partial headless | Runner hangs | Call `WorkflowCalculator.Calculate` directly; watchdog timeout; `net8.0-windows`. |
| Schema DLL must precede engines in CI | Broken CI builds | Per-repo `build.yml` clone+build-only-schema step (B1b/B2); root BuildAlls local-only; clean full-build gate. |
| Engine coupling leaking into comparator/GH | Violates isolation | Comparator/GH ref only schema (+comparator) lib; CI reference-check. |
| Unavailable metric encoded as 0 | Silent false comparison | `MetricValue` invariant (`available=false ⇒ value null`), enforced by `Validate` + tests. |
| B2b greenfield reverse translator | Wasted effort | Spike-gated; strictly optional; distinct route tag. |
| Report non-determinism | Non-reproducible gate | Sorted keys, invariant culture, full finite `double` precision without result rounding; byte-stability tests. |
| Tolerances mistaken as validated | Overclaiming | Label provisional; configurable profiles; corpus-driven revision later. |
| Conditioning pairing frozen too early | Invalid comparison baked into B1/B2 | D10: candidate in B0, frozen only after B2 SingleBox validation. |
| Cross-laptop path drift | Irreproducible | Portable JSON, relative layouts, no absolute paths committed. |

---

## 10. Completion checklist

- [ ] B0 docs merged & human-reviewed; conditioning pairing labelled *candidate*.
- [ ] B1a schema lib + serializer + CLI host + tests merged; DLL in `SAM_Validation\build\`; deterministic serialization + **availability invariant** proven.
- [ ] B1b OS producer merged; offline golden + live `SingleBox` pass; **consumption unit pinned**; CI clone+build-schema step added; C1–C7 green.
- [ ] B2 TAS producer merged; builds on dev laptop; portable golden passes; TAS-laptop run documented & validated; model annual energy present; **D10 pairing validated & frozen**; no new public API.
- [ ] B3 comparator + reports merged; all edge cases (incl. null/unavailable) pass; engine-independent (ref-checked); byte-stable.
- [ ] B4 corpus: ≥3 synthetic + 1 reviewed production model; generated artefacts git-ignored.
- [ ] B2b spike run & documented; producer route built only if spike passed; route-tagged.
- [ ] B5 host repo chosen at kickoff; viewer loads comparison with no engine installed; delegates to B3.
- [ ] B6 docs enable two-laptop run + compare; first validated report committed with provenance; limitations stated.
- [ ] No TAS deps in SAM_OpenStudio; no OpenStudio deps in SAM_Tas; comparator independent of both.
- [ ] Every milestone left repos buildable & testable; one milestone per PR; no oversized PRs.

---

## 11. Recommended first implementation prompt (B0 only)

> Prepend the **Mandatory Git & repository rules** (§13) to this prompt.
>
> **Implement milestone B0 (methodology & contracts) for the SAM TAS↔OpenStudio benchmark.** Repo `SAM_Validation`, branch `feature/benchmark-b0-methodology` created from `sow/2026-Q3`; PR targets `sow/2026-Q3`. **Docs only — no code.**
>
> Create under `SAM_Validation/docs/benchmark/`:
> - `METHODOLOGY.md` — the two-stage file-mediated workflow; the four routes (Native SAM→TAS, Native SAM→OpenStudio, shared-gbXML→both [optional], later TPD production [informational]); the **candidate** primary conditioning pairing **OpenStudio Ideal Loads ↔ TAS thermostats + IZAMs + TBD sizing** (state explicitly it is *not frozen* until the first TAS SingleBox run in B2 confirms meaningful conditioning, annual energy and loads); production HVAC is a separately-labelled later comparison.
> - `METRICS.md` — each metric's definition, canonical unit, sign convention, exact both-route SAM result-parameter source; include the §2.10 availability matrix; **explicitly list excluded metrics** (per-space annual energy; monthly profiles) with evidence; flag caveats: (a) peak-load definition differs (OpenStudio coincident-total vs TAS building-profile max); (b) model consumption stored-magnitude (Wh vs kWh) **unresolved, pinned empirically in B1b**.
> - `SCHEMA.md` — prose spec of the v1.0.0 schema in §7: provenance (**dual hashes `sourceFileHash` + `canonicalModelHash` + `canonicalizationVersion`**), model, spaces; every metric a `MetricValue` with explicit unit + `available` flag and the **availability invariant** (`available=false ⇒ value null`; real 0 ⇒ 0+available; **never 0 for unavailable**); `schemaVersion` semver policy + comparator compatibility (equal major required; minor drift warns).
> - `TOLERANCES.md` — provisional bands **±5% warn / ±15% fail** as **configurable reporting bands, NOT validated thresholds**; near-zero absolute-floor rule; unavailable⇒"N/A" (never fail); peak-hour circular/year-boundary comparison; model-total vs sum-of-spaces reconciliation.
> - `GLOSSARY.md` — engine, route, provenance, canonical unit, tolerance band, gate status, canonicalization.
>
> Constraints: no absolute/machine-specific paths; align with verified findings (System.Text.Json, Space Guid identity, `Query.Source()` provenance, `IntervalHourOfYear` peak hours). Keep each doc scannable. Commit in three boundaries: (1) methodology+glossary; (2) metrics+units; (3) tolerances+schema-spec. Open a PR to `sow/2026-Q3` titled "B0 — benchmark methodology & contracts". Do not modify any code or existing tests. Mark the energy-modeller review as a required human check before merge.

---

## 12. Model allocation & session discipline

Run **one milestone per session**; do not hand B1–B6 to a single model in one continuous run.

| Milestone | Recommended model |
|---|---|
| B0 documentation | **Codex 5.6-SOL High** |
| B1a schema, serializer, deterministic tests | **Codex 5.6-SOL High** |
| B1b OpenStudio producer | **Codex 5.6-SOL High** |
| B2 TAS producer | **Kimi**, followed by **Codex review** |
| B3 comparator & reports | **Codex 5.6-SOL High** |
| B4 corpus preparation | **Kimi** |
| B2b OpenStudio SDK spike | **Codex 5.6-SOL High** |
| B5 Grasshopper components | **Codex 5.6-SOL High** |
| B6 documentation & report assembly | **Kimi** |

Kimi fits B2 (well-specified orchestration around existing APIs) and B4/B6 (mechanical corpus/doc assembly). Deterministic-serialization, comparator, and SDK-spike work go to Codex 5.6-SOL High. **Immediate next step: B0 with Codex 5.6-SOL High; after B0 is reviewed and merged, start B1a in a fresh session.**

Because sessions run on different laptops, **each session ends by committing and pushing its feature branch(es)** so the next session resumes from remote — see the *Multi-laptop continuity* rule in §13.

---

## 13. Mandatory Git & repository rules (include in EVERY implementation prompt)

All implementation work starts from the current `sow/2026-Q3` branch in each affected repository. For every repository modified:

1. Confirm the working tree is clean.
2. Fetch current remote branches.
3. Check out `sow/2026-Q3`.
4. Fast-forward from `origin/sow/2026-Q3`.
5. Create the milestone-specific `feature/benchmark-*` branch.
6. Commit only files belonging to that repository and milestone.
7. Push the feature branch.
8. Open the PR against `sow/2026-Q3` — never `master` or another feature branch.

```bash
git status
git fetch origin
git checkout sow/2026-Q3
git pull --ff-only origin sow/2026-Q3
git checkout -b <milestone-feature-branch>
```

- If a repo lacks `sow/2026-Q3`, or it cannot fast-forward cleanly, **stop before modifying files** and report the exact repo and Git state.
- **Do not reuse one branch across repositories.** Each repo gets its own feature branch, commits and PR.
- **Before implementation, verify the files named in the plan actually live in the stated repository.** In particular, confirm the build-order change lives in each engine repo's own `.github/workflows/build.yml` (verified: the root `BuildAlls*` files are untracked and belong to no repo; do **not** rely on or commit them).

### Multi-laptop continuity (REQUIRED — Michal works across several laptops)

Michal moves between several laptops with different Windows user profiles (see [[two-laptop-workspace-paths]]). To avoid stranding work on one machine:

- **Commit and push the feature branch at the end of every B stage/session** — a stage is only "done for this session" once `git push` has succeeded. Never leave a milestone's work committed-but-unpushed, or uncommitted, on a single laptop.
- **Push work-in-progress commits too**, not only at PR time, so any laptop always has the latest state. Keep messages conventional (`docs:`/`feat:`/`fix:`/…) and end them with the `Co-Authored-By` trailer.
- **When resuming a stage on another laptop**, do NOT recreate the branch: `git fetch origin` → `git checkout feature/benchmark-<id>` → `git pull --ff-only origin feature/benchmark-<id>`, then continue. If the local branch is behind and cannot fast-forward, stop and report before editing.
- Because each repo has its own feature branch, **push every affected repo's branch** at stage end (a milestone that touches two repos must push both).
