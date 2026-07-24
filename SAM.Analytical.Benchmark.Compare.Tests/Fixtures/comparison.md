# Benchmark comparison: TAS vs OpenStudio

Independent comparison of two engine-neutral benchmark documents. Tolerance bands are
**provisional reporting buckets**, not validated thresholds.

## Summary

| Field | Value |
| --- | --- |
| Tolerance profile | default |
| Gate status | Fail |
| Metrics matched | 10 |
| Metrics warned | 4 |
| Metrics failed | 3 |
| Metrics not applicable | 31 |
| TAS schema version | 1.0.0 |
| OpenStudio schema version | 1.0.0 |

## Provenance

| Field | TAS | OpenStudio |
| --- | --- | --- |
| Engine kind | TAS | OpenStudio |
| Engine name | TAS | EnergyPlus |
| Engine version | 9.5.3 | 24.1.0 |
| SDK version |  | 3.10.0 |
| Route | Native-TAS | Native-OpenStudio |
| Weather | London-Gatwick.epw | London-Gatwick.epw |
| Design-day source | DDY | DDY |
| Run state | Success | Success |
| Source model | SingleBox | SingleBox |
| Source model GUID | 0123456789abcdef0123456789abcdef | 0123456789abcdef0123456789abcdef |
| SAM commit | aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa | aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa |

## Model metrics

| Metric | Unit | TAS | OpenStudio | Abs diff | Rel % | Band | Note |
| --- | --- | --- | --- | --- | --- | --- | --- |
| consumptionHeating | kWh | 1000 | 1000 | 0 | 0 | Match |  |
| consumptionCooling | kWh | 500 |  |  |  | N/A | unavailable (TasOnly) |
| peakHeatingLoad | kW | 10 | 30 | 20 | 66.66666666666666 | Fail |  |
| peakHeatingHour | hourOfYear | 200 | 205 | 5 |  | Warn | circular hour difference |
| peakCoolingLoad | kW | 8 | 8.8 | 0.8000000000000007 | 9.090909090909099 | Warn |  |
| peakCoolingHour | hourOfYear | 8759 | 0 | 1 |  | Match | circular hour difference |
| floorArea | m2 | 240 | 240 | 0 | 0 | Match |  |
| volume | m3 | 720 | 720 | 0 | 0 | Match |  |

## Model-total vs sum-of-spaces reconciliation

Additive quantities only; non-additive peak loads are not reconciled.

| Engine | Quantity | Unit | Model total | Sum of spaces | Abs diff | Band | Spaces (used/missing) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TAS | floorArea | m2 | 240 | 252 | 12 | Match | 3/0 |
| TAS | volume | m3 | 720 | 756 | 36 | Match | 3/0 |
| OpenStudio | floorArea | m2 | 240 | 262 | 22 | Warn | 3/0 |
| OpenStudio | volume | m3 | 720 | 786 | 66 | Warn | 3/0 |

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

| Space | Match | Metric | Unit | TAS | OpenStudio | Abs diff | Rel % | Band | Note |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | area | m2 | 200 | 200 | 0 | 0 | Match |  |
| 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | volume | m3 | 600 | 600 | 0 | 0 | Match |  |
| 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | heating.designLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | heating.peakLoad | W | 2000 | 2000 | 0 | 0 | Match |  |
| 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | heating.peakHour | hourOfYear | 205 | 205 | 0 |  | Match | circular hour difference |
| 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | heating.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | cooling.designLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | cooling.peakLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | cooling.peakHour | hourOfYear |  |  |  |  | N/A | unavailable (Neither) |
| 0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a0a | Guid | cooling.unmetHours | h |  |  |  |  | N/A | unavailable (Neither) |
| 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | area | m2 | 40 | 44 | 4 | 9.090909090909092 | Warn |  |
| 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | volume | m3 | 120 | 132 | 12 | 9.090909090909092 | Warn |  |
| 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | heating.designLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | heating.peakLoad | W | 2100 | 2600 | 500 | 19.230769230769234 | Fail |  |
| 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | heating.peakHour | hourOfYear | 205 | 240 | 35 |  | Fail | circular hour difference |
| 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | heating.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | cooling.designLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | cooling.peakLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | cooling.peakHour | hourOfYear |  |  |  |  | N/A | unavailable (Neither) |
| 0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b | Guid | cooling.unmetHours | h |  |  |  |  | N/A | unavailable (Neither) |
| 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | area | m2 | 12 |  |  |  | N/A | unavailable (TasOnly) |
| 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | volume | m3 | 36 |  |  |  | N/A | unavailable (TasOnly) |
| 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | heating.designLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | heating.peakLoad | W | 2000 |  |  |  | N/A | unavailable (TasOnly) |
| 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | heating.peakHour | hourOfYear | 205 |  |  |  | N/A | unavailable (TasOnly) |
| 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | heating.unmetHours | h | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | cooling.designLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | cooling.peakLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | cooling.peakHour | hourOfYear |  |  |  |  | N/A | unavailable (Neither) |
| 0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c0c | TasOnly | cooling.unmetHours | h |  |  |  |  | N/A | unavailable (Neither) |
| 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | area | m2 |  | 18 |  |  | N/A | unavailable (OpenStudioOnly) |
| 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | volume | m3 |  | 54 |  |  | N/A | unavailable (OpenStudioOnly) |
| 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | heating.designLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | heating.peakLoad | W |  | 2000 |  |  | N/A | unavailable (OpenStudioOnly) |
| 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | heating.peakHour | hourOfYear |  | 205 |  |  | N/A | unavailable (OpenStudioOnly) |
| 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | heating.unmetHours | h |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | cooling.designLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | cooling.peakLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | cooling.peakHour | hourOfYear |  |  |  |  | N/A | unavailable (Neither) |
| 0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d0d | OpenStudioOnly | cooling.unmetHours | h |  |  |  |  | N/A | unavailable (Neither) |

