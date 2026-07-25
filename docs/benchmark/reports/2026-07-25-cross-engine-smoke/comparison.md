# Benchmark comparison: TAS vs OpenStudio

Independent comparison of two engine-neutral benchmark documents. Tolerance bands are
**provisional reporting buckets**, not validated thresholds. The overall gate is the worst of
the numerical, coverage, provenance and reconciliation statuses. Peak-hour bands are informational
and are excluded from the numerical status; reconciliation is a separate within-document diagnostic
status that also contributes to the overall gate.

## Summary

| Field | Value |
| --- | --- |
| Tolerance profile | default |
| Gate status (overall) | Fail |
| Numerical status | Fail |
| Coverage status | Warn |
| Provenance status | Fail |
| Reconciliation status | Pass |
| Metrics matched | 48 |
| Metrics warned | 1 |
| Metrics failed | 33 |
| Metrics not applicable | 66 |
| Required metrics compared | 82 of 148 |
| Required metrics unavailable | 66 |
| TAS schema version | 1.0.0 |
| OpenStudio schema version | 1.0.0 |

## Coverage

How much of the two runs was actually compared. Required metrics are the whole-model metrics plus
the metrics of uniquely matched spaces; the metrics of one-sided, duplicated or ambiguous spaces are
not counted here because those spaces are reported under space alignment instead. An unavailable
metric is never a numerical failure, so it is recorded here — otherwise missing results could read as
a clean pass.

| Field | Value |
| --- | --- |
| Coverage status | Warn |
| Required metrics | 148 |
| Compared | 82 |
| Unavailable (not compared) | 66 |
| Unmatched spaces | 0 |
| Ambiguous (split) space names | 0 |
| Duplicated space identities | 0 |

**Coverage is INCOMPLETE**, so this comparison must not be described as a complete result:

- 66 of 148 required metrics were unavailable on at least one side (reported as N/A, never as a numerical failure).

## Provenance compatibility

**INCOMPATIBLE** — the two documents are not a valid comparison pair, so a numerical pass is not meaningful.

| Field | TAS | OpenStudio |
| --- | --- | --- |
| designDaySource | EmbeddedModel | None |

## Tolerance profile

Provisional reporting bands (not validated thresholds). Recorded so a changed profile is never hidden behind an unchanged name.

| Setting | Value |
| --- | --- |
| Name | default |
| Warn (relative) | 0.05 |
| Fail (relative) | 0.15 |
| Peak-hour warn (h) | 1 |
| Peak-hour fail (h) | 24 |
| Default near-zero floor | 0 |
| Near-zero floor (kWh) | 1 |
| Near-zero floor (Wh) | 1000 |
| Near-zero floor (kW) | 0.01 |
| Near-zero floor (W) | 10 |
| Near-zero floor (m2) | 0.01 |
| Near-zero floor (m3) | 0.01 |
| Near-zero floor (h) | 0.5 |

## Provenance

| Field | TAS | OpenStudio |
| --- | --- | --- |
| Engine kind | TAS | OpenStudio |
| Engine name | Tas | EnergyPlus |
| Engine version | 9.5.7.0 | 25.1.0 |
| SDK version |  | 3.10.0 |
| Route | Native-TAS | Native-OpenStudio |
| Weather | Budapest | Budapest |
| Design-day source | EmbeddedModel | None |
| Run state | Success | Success |
| Source model | 000000_SAM_AnalyticalModel | 000000_SAM_AnalyticalModel |
| Source model GUID | a527e890fceb465fbb50f4b6874f3a4c | a527e890fceb465fbb50f4b6874f3a4c |
| SAM commit | abcd617887f6792047023820f3efd2463fc5c6b3 | abcd617887f6792047023820f3efd2463fc5c6b3 |

## Model metrics

> **Caveats (apply even when values pass):** whole-model peak-load semantics differ between
> engines — OpenStudio reports a coincident total, TAS a building-profile maximum — so peak-load
> comparisons are informational, not equivalence claims. The primary conditioning pairing
> (OpenStudio Ideal Loads ↔ TAS thermostats + IZAMs + TBD sizing) is a **candidate** pending
> validation. A tolerance profile cannot remove these limitations.

| Metric | Unit | TAS | OpenStudio | Abs diff | Rel % | Band | Note |
| --- | --- | --- | --- | --- | --- | --- | --- |
| consumptionHeating | kWh | 4331.961588091359 | 1751.1764825992748 | 2580.785105492084 | 59.57543835537944 | Fail |  |
| consumptionCooling | kWh | 0 | 5.2499930426550705 | 5.2499930426550705 | 100 | Fail |  |
| peakHeatingLoad | kW | 3.93393212890625 | 5.720615732872154 | 1.7866836039659035 | 31.23236531513824 | Fail |  |
| peakHeatingHour | hourOfYear | 919 | 775 | 144 |  | Fail | circular hour difference |
| peakCoolingLoad | kW | 0 | 0.0402250938503556 | 0.0402250938503556 | 100 | Fail |  |
| peakCoolingHour | hourOfYear | 0 | 7974 | 786 |  | Fail | circular hour difference |
| floorArea | m2 | 167.95195245742798 | 167.95195207345205 | 3.839759301627055E-07 | 2.2862248669602975E-07 | Match |  |
| volume | m3 | 470.1821608543396 | 470.1821620538829 | 1.1995433055744797E-06 | 2.5512309959496334E-07 | Match |  |

## Model-total vs sum-of-spaces reconciliation

Additive quantities only; non-additive peak loads are not reconciled. Only uniquely-matched spaces are summed; unmatched or ambiguous spaces make a reconciliation incomplete.

| Engine | Quantity | Unit | Model total | Sum of spaces | Abs diff | Band | Used/Missing/Excluded | Complete |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| TAS | floorArea | m2 | 167.95195245742798 | 167.95195207345202 | 3.839759585844149E-07 | Match | 14/0/0 | yes |
| TAS | volume | m3 | 470.1821608543396 | 470.1821620538828 | 1.199543191887642E-06 | Match | 14/0/0 | yes |
| OpenStudio | floorArea | m2 | 167.95195207345205 | 167.95195207345202 | 2.842170943040401E-14 | Match | 14/0/0 | yes |
| OpenStudio | volume | m3 | 470.1821620538829 | 470.1821620538828 | 1.1368683772161603E-13 | Match | 14/0/0 | yes |

## Space alignment

| Field | Value |
| --- | --- |
| TAS spaces | 14 |
| OpenStudio spaces | 14 |
| Matched by GUID | 14 |
| Matched by name | 0 |

**Only in TAS:** none

**Only in OpenStudio:** none

**Duplicate TAS GUIDs:** none

**Duplicate OpenStudio GUIDs:** none

**Duplicate TAS names:** none

**Duplicate OpenStudio names:** none

**Ambiguous (split) name matches:** none

## Space metrics

| Space | Match | Metric | Unit | TAS | OpenStudio | Abs diff | Rel % | Band | Note |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 025eb80f65d745b799511b78ac2f82fb | Guid | area | m2 | 6.6920779999999995 | 6.6920779999999995 | 0 | 0 | Match |  |
| 025eb80f65d745b799511b78ac2f82fb | Guid | volume | m3 | 9.118059598283878 | 9.118059598283878 | 0 | 0 | Match |  |
| 025eb80f65d745b799511b78ac2f82fb | Guid | heating.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 025eb80f65d745b799511b78ac2f82fb | Guid | heating.peakLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 025eb80f65d745b799511b78ac2f82fb | Guid | heating.peakHour | hourOfYear |  |  |  |  | N/A | unavailable (Neither) |
| 025eb80f65d745b799511b78ac2f82fb | Guid | heating.unmetHours | h | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 025eb80f65d745b799511b78ac2f82fb | Guid | cooling.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 025eb80f65d745b799511b78ac2f82fb | Guid | cooling.peakLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 025eb80f65d745b799511b78ac2f82fb | Guid | cooling.peakHour | hourOfYear |  |  |  |  | N/A | unavailable (Neither) |
| 025eb80f65d745b799511b78ac2f82fb | Guid | cooling.unmetHours | h | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 0872cd39962f43c3b62a6e031a76c24a | Guid | area | m2 | 21.280010175465385 | 21.280010175465385 | 0 | 0 | Match |  |
| 0872cd39962f43c3b62a6e031a76c24a | Guid | volume | m3 | 57.33913699268221 | 57.33913699268221 | 0 | 0 | Match |  |
| 0872cd39962f43c3b62a6e031a76c24a | Guid | heating.designLoad | W | 574.0481567382812 |  |  |  | N/A | unavailable (TasOnly) |
| 0872cd39962f43c3b62a6e031a76c24a | Guid | heating.peakLoad | W | 565.50927734375 | 1185.0374713731348 | 619.5281940293848 | 52.279207113300885 | Fail |  |
| 0872cd39962f43c3b62a6e031a76c24a | Guid | heating.peakHour | hourOfYear | 1441 | 919 | 522 |  | Fail | circular hour difference |
| 0872cd39962f43c3b62a6e031a76c24a | Guid | heating.unmetHours | h | 65 | 0 | 65 | 100 | Fail |  |
| 0872cd39962f43c3b62a6e031a76c24a | Guid | cooling.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 0872cd39962f43c3b62a6e031a76c24a | Guid | cooling.peakLoad | W |  | 15.588918884437554 |  |  | N/A | unavailable (OpenStudioOnly) |
| 0872cd39962f43c3b62a6e031a76c24a | Guid | cooling.peakHour | hourOfYear |  | 7974 |  |  | N/A | unavailable (OpenStudioOnly) |
| 0872cd39962f43c3b62a6e031a76c24a | Guid | cooling.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| 13a0403ab3a543608613b0ce9bf52d92 | Guid | area | m2 | 3.647967799712404 | 3.647967799712404 | 0 | 0 | Match |  |
| 13a0403ab3a543608613b0ce9bf52d92 | Guid | volume | m3 | 10.396708229180378 | 10.396708229180378 | 0 | 0 | Match |  |
| 13a0403ab3a543608613b0ce9bf52d92 | Guid | heating.designLoad | W | 199.83349609375 |  |  |  | N/A | unavailable (TasOnly) |
| 13a0403ab3a543608613b0ce9bf52d92 | Guid | heating.peakLoad | W | 199.82528686523438 | 141.7703965184828 | 58.05489034675156 | 29.052824723782223 | Fail |  |
| 13a0403ab3a543608613b0ce9bf52d92 | Guid | heating.peakHour | hourOfYear | 1441 | 918 | 523 |  | Fail | circular hour difference |
| 13a0403ab3a543608613b0ce9bf52d92 | Guid | heating.unmetHours | h | 4761 | 0 | 4761 | 100 | Fail |  |
| 13a0403ab3a543608613b0ce9bf52d92 | Guid | cooling.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 13a0403ab3a543608613b0ce9bf52d92 | Guid | cooling.peakLoad | W |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 13a0403ab3a543608613b0ce9bf52d92 | Guid | cooling.peakHour | hourOfYear |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 13a0403ab3a543608613b0ce9bf52d92 | Guid | cooling.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| 21bdbb806b4f4e2fbad0632b19fdf14b | Guid | area | m2 | 2.9450000000000016 | 2.9450000000000016 | 0 | 0 | Match |  |
| 21bdbb806b4f4e2fbad0632b19fdf14b | Guid | volume | m3 | 5.746544632499992 | 5.746544632499992 | 0 | 0 | Match |  |
| 21bdbb806b4f4e2fbad0632b19fdf14b | Guid | heating.designLoad | W | 28.741456985473633 |  |  |  | N/A | unavailable (TasOnly) |
| 21bdbb806b4f4e2fbad0632b19fdf14b | Guid | heating.peakLoad | W |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 21bdbb806b4f4e2fbad0632b19fdf14b | Guid | heating.peakHour | hourOfYear |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 21bdbb806b4f4e2fbad0632b19fdf14b | Guid | heating.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| 21bdbb806b4f4e2fbad0632b19fdf14b | Guid | cooling.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 21bdbb806b4f4e2fbad0632b19fdf14b | Guid | cooling.peakLoad | W |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 21bdbb806b4f4e2fbad0632b19fdf14b | Guid | cooling.peakHour | hourOfYear |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 21bdbb806b4f4e2fbad0632b19fdf14b | Guid | cooling.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| 312fab2f1ba64756b47ccc3dcb21ac8a | Guid | area | m2 | 2.355387206192353 | 2.355387206192353 | 0 | 0 | Match |  |
| 312fab2f1ba64756b47ccc3dcb21ac8a | Guid | volume | m3 | 6.712853537648204 | 6.712853537648204 | 0 | 0 | Match |  |
| 312fab2f1ba64756b47ccc3dcb21ac8a | Guid | heating.designLoad | W | 69.60518646240234 |  |  |  | N/A | unavailable (TasOnly) |
| 312fab2f1ba64756b47ccc3dcb21ac8a | Guid | heating.peakLoad | W | 52.024513244628906 | 0 | 52.024513244628906 | 100 | Fail |  |
| 312fab2f1ba64756b47ccc3dcb21ac8a | Guid | heating.peakHour | hourOfYear | 1441 | 0 | 1441 |  | Fail | circular hour difference |
| 312fab2f1ba64756b47ccc3dcb21ac8a | Guid | heating.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| 312fab2f1ba64756b47ccc3dcb21ac8a | Guid | cooling.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 312fab2f1ba64756b47ccc3dcb21ac8a | Guid | cooling.peakLoad | W |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 312fab2f1ba64756b47ccc3dcb21ac8a | Guid | cooling.peakHour | hourOfYear |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 312fab2f1ba64756b47ccc3dcb21ac8a | Guid | cooling.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| 354b8786b4714f3c9941cec051a93361 | Guid | area | m2 | 3.0986077487352315 | 3.0986077487352315 | 0 | 0 | Match |  |
| 354b8786b4714f3c9941cec051a93361 | Guid | volume | m3 | 9.007679927026205 | 9.007679927026205 | 0 | 0 | Match |  |
| 354b8786b4714f3c9941cec051a93361 | Guid | heating.designLoad | W | 137.82980346679688 |  |  |  | N/A | unavailable (TasOnly) |
| 354b8786b4714f3c9941cec051a93361 | Guid | heating.peakLoad | W | 117.870849609375 | 262.9986126690514 | 145.1277630596764 | 55.18195004408647 | Fail |  |
| 354b8786b4714f3c9941cec051a93361 | Guid | heating.peakHour | hourOfYear | 1441 | 919 | 522 |  | Fail | circular hour difference |
| 354b8786b4714f3c9941cec051a93361 | Guid | heating.unmetHours | h | 67 | 0 | 67 | 100 | Fail |  |
| 354b8786b4714f3c9941cec051a93361 | Guid | cooling.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 354b8786b4714f3c9941cec051a93361 | Guid | cooling.peakLoad | W |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 354b8786b4714f3c9941cec051a93361 | Guid | cooling.peakHour | hourOfYear |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 354b8786b4714f3c9941cec051a93361 | Guid | cooling.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| 4377cbd6712349e68fdae8814ff9c5d9 | Guid | area | m2 | 5.03787628653 | 5.03787628653 | 0 | 0 | Match |  |
| 4377cbd6712349e68fdae8814ff9c5d9 | Guid | volume | m3 | 7.564547888993382 | 7.564547888993382 | 0 | 0 | Match |  |
| 4377cbd6712349e68fdae8814ff9c5d9 | Guid | heating.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 4377cbd6712349e68fdae8814ff9c5d9 | Guid | heating.peakLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 4377cbd6712349e68fdae8814ff9c5d9 | Guid | heating.peakHour | hourOfYear |  |  |  |  | N/A | unavailable (Neither) |
| 4377cbd6712349e68fdae8814ff9c5d9 | Guid | heating.unmetHours | h | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 4377cbd6712349e68fdae8814ff9c5d9 | Guid | cooling.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 4377cbd6712349e68fdae8814ff9c5d9 | Guid | cooling.peakLoad | W |  |  |  |  | N/A | unavailable (Neither) |
| 4377cbd6712349e68fdae8814ff9c5d9 | Guid | cooling.peakHour | hourOfYear |  |  |  |  | N/A | unavailable (Neither) |
| 4377cbd6712349e68fdae8814ff9c5d9 | Guid | cooling.unmetHours | h | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 6e7ef8487b714da3b730e0f226af927e | Guid | area | m2 | 23.55004650825489 | 23.55004650825489 | 0 | 0 | Match |  |
| 6e7ef8487b714da3b730e0f226af927e | Guid | volume | m3 | 58.340316505330264 | 58.340316505330264 | 0 | 0 | Match |  |
| 6e7ef8487b714da3b730e0f226af927e | Guid | heating.designLoad | W | 379.9020080566406 |  |  |  | N/A | unavailable (TasOnly) |
| 6e7ef8487b714da3b730e0f226af927e | Guid | heating.peakLoad | W | 132.86868286132812 | 255.3860902131586 | 122.51740735183049 | 47.97340655850561 | Fail |  |
| 6e7ef8487b714da3b730e0f226af927e | Guid | heating.peakHour | hourOfYear | 1442 | 919 | 523 |  | Fail | circular hour difference |
| 6e7ef8487b714da3b730e0f226af927e | Guid | heating.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| 6e7ef8487b714da3b730e0f226af927e | Guid | cooling.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 6e7ef8487b714da3b730e0f226af927e | Guid | cooling.peakLoad | W |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 6e7ef8487b714da3b730e0f226af927e | Guid | cooling.peakHour | hourOfYear |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 6e7ef8487b714da3b730e0f226af927e | Guid | cooling.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| 7ee49aac87b24671bb00ed1b291806a1 | Guid | area | m2 | 9.409999999999991 | 9.409999999999991 | 0 | 0 | Match |  |
| 7ee49aac87b24671bb00ed1b291806a1 | Guid | volume | m3 | 26.966204214472434 | 26.966204214472434 | 0 | 0 | Match |  |
| 7ee49aac87b24671bb00ed1b291806a1 | Guid | heating.designLoad | W | 366.3569641113281 |  |  |  | N/A | unavailable (TasOnly) |
| 7ee49aac87b24671bb00ed1b291806a1 | Guid | heating.peakLoad | W | 366.33624267578125 | 190.23285633880042 | 176.10338633698083 | 48.07151622528315 | Fail |  |
| 7ee49aac87b24671bb00ed1b291806a1 | Guid | heating.peakHour | hourOfYear | 1444 | 919 | 525 |  | Fail | circular hour difference |
| 7ee49aac87b24671bb00ed1b291806a1 | Guid | heating.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| 7ee49aac87b24671bb00ed1b291806a1 | Guid | cooling.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 7ee49aac87b24671bb00ed1b291806a1 | Guid | cooling.peakLoad | W |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 7ee49aac87b24671bb00ed1b291806a1 | Guid | cooling.peakHour | hourOfYear |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 7ee49aac87b24671bb00ed1b291806a1 | Guid | cooling.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| 8d2d2a0234584fe4b171e5a9373f1b4d | Guid | area | m2 | 4.634866891556 | 4.634866891556 | 0 | 0 | Match |  |
| 8d2d2a0234584fe4b171e5a9373f1b4d | Guid | volume | m3 | 28.16450759620351 | 28.16450759620351 | 0 | 0 | Match |  |
| 8d2d2a0234584fe4b171e5a9373f1b4d | Guid | heating.designLoad | W | 69.23214721679688 |  |  |  | N/A | unavailable (TasOnly) |
| 8d2d2a0234584fe4b171e5a9373f1b4d | Guid | heating.peakLoad | W | 11.025825500488281 | 39.29050142981706 | 28.26467592932878 | 71.93768188430163 | Fail |  |
| 8d2d2a0234584fe4b171e5a9373f1b4d | Guid | heating.peakHour | hourOfYear | 1452 | 774 | 678 |  | Fail | circular hour difference |
| 8d2d2a0234584fe4b171e5a9373f1b4d | Guid | heating.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| 8d2d2a0234584fe4b171e5a9373f1b4d | Guid | cooling.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| 8d2d2a0234584fe4b171e5a9373f1b4d | Guid | cooling.peakLoad | W |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 8d2d2a0234584fe4b171e5a9373f1b4d | Guid | cooling.peakHour | hourOfYear |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| 8d2d2a0234584fe4b171e5a9373f1b4d | Guid | cooling.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| affc43b0b5b3445f85d5cea8a7b88d4f | Guid | area | m2 | 6.955039759539971 | 6.955039759539971 | 0 | 0 | Match |  |
| affc43b0b5b3445f85d5cea8a7b88d4f | Guid | volume | m3 | 20.363019689898447 | 20.363019689898447 | 0 | 0 | Match |  |
| affc43b0b5b3445f85d5cea8a7b88d4f | Guid | heating.designLoad | W | 204.9483642578125 |  |  |  | N/A | unavailable (TasOnly) |
| affc43b0b5b3445f85d5cea8a7b88d4f | Guid | heating.peakLoad | W | 117.45973205566406 | 133.44180277588939 | 15.982070720225323 | 11.97680965616646 | Warn |  |
| affc43b0b5b3445f85d5cea8a7b88d4f | Guid | heating.peakHour | hourOfYear | 1447 | 774 | 673 |  | Fail | circular hour difference |
| affc43b0b5b3445f85d5cea8a7b88d4f | Guid | heating.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| affc43b0b5b3445f85d5cea8a7b88d4f | Guid | cooling.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| affc43b0b5b3445f85d5cea8a7b88d4f | Guid | cooling.peakLoad | W |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| affc43b0b5b3445f85d5cea8a7b88d4f | Guid | cooling.peakHour | hourOfYear |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| affc43b0b5b3445f85d5cea8a7b88d4f | Guid | cooling.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| c7df54b3c0064fc5a7c6e6012dbb9b8e | Guid | area | m2 | 38.98503142608865 | 38.98503142608865 | 0 | 0 | Match |  |
| c7df54b3c0064fc5a7c6e6012dbb9b8e | Guid | volume | m3 | 121.59536233412693 | 121.59536233412693 | 0 | 0 | Match |  |
| c7df54b3c0064fc5a7c6e6012dbb9b8e | Guid | heating.designLoad | W | 1065.076171875 |  |  |  | N/A | unavailable (TasOnly) |
| c7df54b3c0064fc5a7c6e6012dbb9b8e | Guid | heating.peakLoad | W | 1025.7105712890625 | 1789.9830426085152 | 764.2724713194527 | 42.69719059492821 | Fail |  |
| c7df54b3c0064fc5a7c6e6012dbb9b8e | Guid | heating.peakHour | hourOfYear | 1443 | 775 | 668 |  | Fail | circular hour difference |
| c7df54b3c0064fc5a7c6e6012dbb9b8e | Guid | heating.unmetHours | h | 114 | 0 | 114 | 100 | Fail |  |
| c7df54b3c0064fc5a7c6e6012dbb9b8e | Guid | cooling.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| c7df54b3c0064fc5a7c6e6012dbb9b8e | Guid | cooling.peakLoad | W |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| c7df54b3c0064fc5a7c6e6012dbb9b8e | Guid | cooling.peakHour | hourOfYear |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| c7df54b3c0064fc5a7c6e6012dbb9b8e | Guid | cooling.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| d8294f2a30b445ca9f6ba83c8dcf4903 | Guid | area | m2 | 18.080018143643976 | 18.080018143643976 | 0 | 0 | Match |  |
| d8294f2a30b445ca9f6ba83c8dcf4903 | Guid | volume | m3 | 51.52805170938531 | 51.52805170938531 | 0 | 0 | Match |  |
| d8294f2a30b445ca9f6ba83c8dcf4903 | Guid | heating.designLoad | W | 564.768798828125 |  |  |  | N/A | unavailable (TasOnly) |
| d8294f2a30b445ca9f6ba83c8dcf4903 | Guid | heating.peakLoad | W | 559.67822265625 | 936.5975380258114 | 376.91931536956145 | 40.243466384082474 | Fail |  |
| d8294f2a30b445ca9f6ba83c8dcf4903 | Guid | heating.peakHour | hourOfYear | 1441 | 727 | 714 |  | Fail | circular hour difference |
| d8294f2a30b445ca9f6ba83c8dcf4903 | Guid | heating.unmetHours | h | 69 | 0 | 69 | 100 | Fail |  |
| d8294f2a30b445ca9f6ba83c8dcf4903 | Guid | cooling.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| d8294f2a30b445ca9f6ba83c8dcf4903 | Guid | cooling.peakLoad | W |  | 28.70645886203079 |  |  | N/A | unavailable (OpenStudioOnly) |
| d8294f2a30b445ca9f6ba83c8dcf4903 | Guid | cooling.peakHour | hourOfYear |  | 7991 |  |  | N/A | unavailable (OpenStudioOnly) |
| d8294f2a30b445ca9f6ba83c8dcf4903 | Guid | cooling.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |
| d94f4dbee3d24ff1bcb341e2c5c6ec6d | Guid | area | m2 | 21.280022127733165 | 21.280022127733165 | 0 | 0 | Match |  |
| d94f4dbee3d24ff1bcb341e2c5c6ec6d | Guid | volume | m3 | 57.3391691981517 | 57.3391691981517 | 0 | 0 | Match |  |
| d94f4dbee3d24ff1bcb341e2c5c6ec6d | Guid | heating.designLoad | W | 549.6607055664062 |  |  |  | N/A | unavailable (TasOnly) |
| d94f4dbee3d24ff1bcb341e2c5c6ec6d | Guid | heating.peakLoad | W | 529.828857421875 | 989.0652526225581 | 459.2363952006831 | 46.431354653597815 | Fail |  |
| d94f4dbee3d24ff1bcb341e2c5c6ec6d | Guid | heating.peakHour | hourOfYear | 1457 | 919 | 538 |  | Fail | circular hour difference |
| d94f4dbee3d24ff1bcb341e2c5c6ec6d | Guid | heating.unmetHours | h | 29 | 0 | 29 | 100 | Fail |  |
| d94f4dbee3d24ff1bcb341e2c5c6ec6d | Guid | cooling.designLoad | W | 0 |  |  |  | N/A | unavailable (TasOnly) |
| d94f4dbee3d24ff1bcb341e2c5c6ec6d | Guid | cooling.peakLoad | W |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| d94f4dbee3d24ff1bcb341e2c5c6ec6d | Guid | cooling.peakHour | hourOfYear |  | 0 |  |  | N/A | unavailable (OpenStudioOnly) |
| d94f4dbee3d24ff1bcb341e2c5c6ec6d | Guid | cooling.unmetHours | h | 0 | 0 | 0 | 0 | Match |  |

