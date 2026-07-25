# Benchmark metrics

## Contract rules

- Every metric carries an explicit canonical unit. A producer converts at emission time; a comparator never infers units from a SAM parameter name.
- Heating and cooling are separate, non-negative magnitudes. No signed heating/cooling convention is used.
- A negative magnitude is invalid unless the producer first applies a documented engine-specific sign conversion.
- A metric is available only when the route produced a meaningful value. Missing values use `available: false` and `value: null`.
- Peak hours use interval hour of year `0..8759`. SAM timestamp conversion uses `Core.Query.IntervalHourOfYear`.

## Whole-model metrics

| Schema field | Definition | Unit | OpenStudio SAM source | Tas SAM source |
|---|---|---|---|---|
| `consumptionHeating` | Annual delivered heating energy for conditioned spaces on the selected route | `kWh` | `AnalyticalModelSimulationResultParameter.ConsumptionHeating`, populated from `OpenStudioSimulationResultSet.TotalAnnualHeating` | `AnalyticalModelSimulationResultParameter.ConsumptionHeating`, populated by `Convert.ToSAM_AnalyticalModelSimulationResult` from the sum of `tsdBuildingArray.heatingProfile` |
| `consumptionCooling` | Annual delivered cooling energy for conditioned spaces on the selected route | `kWh` | `AnalyticalModelSimulationResultParameter.ConsumptionCooling`, populated from `OpenStudioSimulationResultSet.TotalAnnualCooling` | `AnalyticalModelSimulationResultParameter.ConsumptionCooling`, populated by `Convert.ToSAM_AnalyticalModelSimulationResult` from the sum of `tsdBuildingArray.coolingProfile` |
| `peakHeatingLoad` | Maximum whole-model heating-load magnitude reported by the route | `kW` | `AnalyticalModelSimulationResultParameter.PeakHeatingLoad`, from `PeakHeatingLoadTotal` | `AnalyticalModelSimulationResultParameter.PeakHeatingLoad`, maximum of `tsdBuildingArray.heatingProfile` |
| `peakHeatingHour` | Interval hour of year at the whole-model heating peak | `hourOfYear` | `AnalyticalModelSimulationResultParameter.PeakHeatingHour`, from `PeakHeatingHourTotal` | `AnalyticalModelSimulationResultParameter.PeakHeatingHour`, index of the maximum heating-profile value |
| `peakCoolingLoad` | Maximum whole-model cooling-load magnitude reported by the route | `kW` | `AnalyticalModelSimulationResultParameter.PeakCoolingLoad`, from `PeakCoolingLoadTotal` | `AnalyticalModelSimulationResultParameter.PeakCoolingLoad`, maximum of `tsdBuildingArray.coolingProfile` |
| `peakCoolingHour` | Interval hour of year at the whole-model cooling peak | `hourOfYear` | `AnalyticalModelSimulationResultParameter.PeakCoolingHour`, from `PeakCoolingHourTotal` | `AnalyticalModelSimulationResultParameter.PeakCoolingHour`, index of the maximum cooling-profile value |
| `floorArea` | Sum of model-space floor areas represented by the run | `m2` | `AnalyticalModelSimulationResultParameter.FloorArea`, summed from source SAM `SpaceParameter.Area` | `AnalyticalModelSimulationResultParameter.FloorArea`, read from Tas building data by `Core.Tas.Query.FloorArea` |
| `volume` | Sum of model-space volumes represented by the run | `m3` | `AnalyticalModelSimulationResultParameter.Volume`, summed from source SAM `SpaceParameter.Volume` | `AnalyticalModelSimulationResultParameter.Volume`, read from Tas building data by `Core.Tas.Query.Volume` |

### Annual-consumption open item

The canonical unit is `kWh`, but the stored SAM magnitude is not yet trusted:

- the SAM enum label and the OpenStudio result-set contract say kWh;
- OpenStudio copies its result-set value directly into the SAM parameter;
- existing legacy consumers, including a Tas Grasshopper building-results path, divide the stored value by 1000.

B1b must compare the raw EnergyPlus SQL value, the OpenStudio result-set value, and the emitted value on a live `SingleBox` run. Until that empirical pin is recorded, a producer must not guess from the enum name. The Tas conversion must then be verified against the same canonical unit. Any interim unavailable value is `null`, never zero.

### Whole-model peak caveat

The values are comparable reporting metrics but do not yet have proven identical semantics. OpenStudio reports its coincident-total result, while Tas takes the maximum of the Tas building profile. Differences can therefore reflect aggregation and reporting semantics as well as engine physics. These metrics remain informational until corpus evidence supports a stronger gate interpretation.

Concretely, `peakHeatingLoad` and `peakCoolingLoad` are **designated informational metrics**: the comparator bands, reports and marks them, but excludes them from the numerical status, so they cannot fail an experiment on aggregation semantics the contract has not yet settled. Only these two whole-model peaks are excluded — the per-space peak loads (`heating.peakLoad` / `cooling.peakLoad`) gate normally. See [Informational metrics](TOLERANCES.md#informational-metrics).

## Per-space metrics

Each space contains `area` and `volume` plus `heating` and `cooling` result groups. SAM results are selected by both space identity and `SpaceSimulationResultParameter.LoadType`.

| Schema field | Definition | Unit | OpenStudio SAM source | Tas SAM source |
|---|---|---|---|---|
| `area` | Floor area of the source SAM space represented by the result | `m2` | `SpaceSimulationResultParameter.Area`; where absent on the result, source `SpaceParameter.Area` may be copied with provenance | `SpaceSimulationResultParameter.Area`, populated from Tas zone floor area |
| `volume` | Volume of the source SAM space represented by the result | `m3` | `SpaceSimulationResultParameter.Volume`; where absent on the result, source `SpaceParameter.Volume` may be copied with provenance | `SpaceSimulationResultParameter.Volume`, populated from Tas zone volume |
| `heating.designLoad` / `cooling.designLoad` | Route sizing load for the space and load type | `W` | `SpaceSimulationResultParameter.DesignLoad` only when a sizing-derived result exists; the current primary mapping otherwise has only a peak proxy and must emit unavailable rather than silently relabel `Load` | `SpaceSimulationResultParameter.DesignLoad`, populated from TBD zone `maxHeatingLoad` / `maxCoolingLoad` by `UpdateDesignLoads` |
| `heating.peakLoad` / `cooling.peakLoad` | Maximum simulated space-load magnitude for the load type | `W` | `SpaceSimulationResultParameter.Load`; `ToSAM_SpaceSimulationResults` converts engine kW to W | `SpaceSimulationResultParameter.Load`, populated from the corresponding Tas zone load/profile result |
| `heating.peakHour` / `cooling.peakHour` | Interval hour of year at the space peak | `hourOfYear` | `SpaceSimulationResultParameter.LoadIndex` | `SpaceSimulationResultParameter.LoadIndex` |
| `heating.unmetHours` / `cooling.unmetHours` | Number of annual intervals in which the route did not meet the applicable setpoint | `h` | `SpaceSimulationResultParameter.UnmetHours`, mapped from the route's setpoint-not-met result | `SpaceSimulationResultParameter.UnmetHours`, calculated by `Query.UnmetHours` and copied onto the load-type result |

Design load and simulated peak load are distinct metrics. A producer must not populate both from one SAM `Load` value without an explicit future schema decision.

`OccupiedUnmetHours` and `UnmetHourFirstIndex` exist in SAM but are outside the v1 primary metric set. They may be retained as producer diagnostics; they must not be substituted for `unmetHours`.

## Availability matrix

| Metric | OpenStudio native | Tas native (gbXML) | Neutral schema |
|---|---|---|---|
| Model annual heating/cooling | Available, subject to B1b unit pin | Requires explicit building-results read | Nullable |
| Model peak heating/cooling load | Available; coincident-total semantic | Available; building-profile maximum | Nullable; semantic caveat |
| Model peak heating/cooling hour | Available | Available | Nullable; circular comparison |
| Model floor area / volume | Available | Available | Nullable |
| Space area / volume | Available | Available | Nullable |
| Space peak load and hour | Available | Available | Nullable |
| Space design load | Peak proxy only in the primary result mapping | Available from sizing | Nullable; do not conflate with peak load |
| Space unmet hours | Available when requested/extracted | Available when requested/calculated | Nullable |
| **Per-space annual energy** | **Excluded** | **Excluded** | **Not present** |
| **Monthly profiles** | **Excluded** | **Excluded for the native TBD/TSD route** | **Not present** |

## Excluded metrics

### Per-space annual energy

The v1 contract excludes per-space annual heating and cooling energy. `SpaceSimulationResultParameter` has no annual-energy parameter, and neither native producer attaches a common per-space annual-energy value to the SAM result graph. OpenStudio's internal extraction dictionaries do not create a shared SAM contract by themselves.

### Monthly profiles

The v1 contract excludes monthly energy and load profiles. The common native result routes provide scalar annual/peak values and hourly temperature-related results, not a shared monthly profile. Tas TPD exposes monthly plant-side data, but TPD is a later production-HVAC route and cannot supply a metric for the primary idealised comparison.

Adding either excluded metric requires a schema version change, sources from both primary routes, canonical units, and energy-modeller review.

## Review checklist

Before B0 merges, an energy modeller must confirm the definitions above, especially:

- annual delivered-energy scope;
- whole-model peak aggregation differences;
- sizing load versus simulated peak load;
- setpoint-not-met semantics; and
- the B1b consumption-unit validation procedure.
