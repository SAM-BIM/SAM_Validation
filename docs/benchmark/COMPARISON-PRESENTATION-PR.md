# Comparison presentation polish (SAM_UI#238 companion)

Status: implemented on `claude/tas-openstudio-comparison`; follow-up to merged SAM_Validation#14. Not merged.
Base: `sow/2026-Q4` at `8fa79f5c8559c2db11e0ce2a26193e5ccad80e79` (merge of #14).
Tested content SHA: `a39e4a9fd11019de9400f62fe5e68179539a4661`. This record is the only change after it.

## Scope

Presentation only. No simulation, conversion, metric mapping, unit conversion, tolerance, gate or comparison arithmetic changed. `comparison.csv` and `comparison-summary.json` keep their existing schema, columns and full precision. `Format.Number` and `Format.Percent` remain the raw contract.

## What changed

- **`PresentationFormat`** (new public class in `SAM.Analytical.Benchmark.Compare`):
  - two-decimal engineering quantities, absolute differences and percentages;
  - hour-of-year values and hour differences remain integers;
  - null, NaN and infinity render as `n/a` (or empty for the UI) and are never zero;
  - informational rows carry the exact label `Informational — excluded from numerical status`;
  - unit mismatch remains distinct from informational;
  - space name/GUID and the input-manifest limitation are presentation helpers.
- **`ComparisonResult`** adds `ContributingFailCount` and `InformationalFailCount`, both derived from the comparator's existing `Query.IsInformationalForNumericalGate`. No gate logic changed.
- **`WriteMarkdown`**:
  - engineering values, absolute differences and normalized percentages use two decimals; hours/counts/identifiers stay integral;
  - the summary shows all Fail rows plus the contributing/informational split (the saved example: 10 all, 4 contributing, 6 informational);
  - a Difference definition section explains the unsigned normalized difference and the 5.00%/15.00% thresholds from the stored fractions;
  - `n/a` and `—` markers are explained;
  - provenance includes the `designDaySource=None` explanation and the input-manifest limitation;
  - engine warnings and notes are listed;
  - reconciliation has two-decimal quantities and a normalized difference column;
  - space metrics show the space name beside its GUID, including differing/missing names.
- Markdown goldens were refreshed for the presentation change. CSV and summary JSON goldens are unchanged and byte-identical.

## Review follow-up: fractional peak-hour thresholds

The Markdown tolerance-profile table cast `HourWarnAbsolute`/`HourFailAbsolute` to `int`, so a valid profile with 1.5 h / 24.5 h was reported as 1 h / 24 h while `comparison.csv` and `comparison-summary.json` kept the fractions. This was a presentation defect only.

- `WriteMarkdown.AppendToleranceProfile` now renders both thresholds with `Format.Number`, the same full-precision invariant format the CSV uses. Integer formatting stays where it belongs: hour-of-year indices and hour differences (`PresentationFormat.Number(..., MetricUnit.HourOfYear)`).
- New regression `ReportTests.MarkdownKeepsFractionalHourThresholdsAndAgreesWithTheRawProfile` compares the Markdown with the raw profile, the `toleranceProfileDetail.hourWarnAbsolute`/`hourFailAbsolute` values in the summary JSON, and the per-row CSV profile columns for a 1.5/24.5 profile. It also asserts that `1`/`24` are not emitted.
- No band, gate, tolerance, CSV, JSON or comparison arithmetic changed. With the default profile the Markdown still says 1 h / 24 h, so all goldens remain byte-identical.

Reproduce:

```
dotnet test SAM.Analytical.Benchmark.Compare.Tests -c Release
dotnet test SAM.Analytical.Benchmark.Compare.ApiTests -c Release
dotnet build SAM_Validation.sln -c Release
```

The saved `box-both` comparison regenerated with this build keeps 10 Fail bands (4 contributing, 6 informational); its `comparison.csv` (SHA-256 `8092B384DD9D9BA9289E1C10B77DB7C93BCD95E911B4A0F32CEE93373C0ECD3D`) and `comparison-summary.json` (SHA-256 `36B9D353B35CDFEB1FBAC70B78317BF36C4F393159F114FCF39716D825FF4A98`) are byte-identical to the saved run.

## Files changed

- `SAM.Analytical.Benchmark.Compare/Classes/PresentationFormat.cs` (new)
- `SAM.Analytical.Benchmark.Compare/Classes/ComparisonResult.cs`
- `SAM.Analytical.Benchmark.Compare/Modify/WriteMarkdown.cs`
- `SAM.Analytical.Benchmark.Compare.Tests/PresentationFormatTests.cs` (new)
- `SAM.Analytical.Benchmark.Compare.Tests/ReportTests.cs` (presentation assertions and the fractional-hour regression)
- `SAM.Analytical.Benchmark.Compare.Tests/ExtractionRegressionTests.cs`
- `SAM.Analytical.Benchmark.Compare.Tests/Fixtures/comparison.md`
- `SAM.Analytical.Benchmark.Compare.Tests/Fixtures/Regression/{fixtures,scenario,weather-mismatch}/comparison.md`

## Validation

| Check | Result |
|---|---|
| `dotnet test SAM.Analytical.Benchmark.Compare.Tests -c Release` | PASS 100/100 (includes the fractional-hour regression) |
| `dotnet test SAM.Analytical.Benchmark.Compare.ApiTests -c Release` | PASS 2/2 |
| `SAM_Validation.sln` Release rebuild | PASS, 0 errors (1 pre-existing warning) |
| Saved `box-both` documents | Markdown regenerated; CSV SHA-256 and summary JSON SHA-256 byte-identical to the saved run |
| Regression cases | CSV and summary JSON byte-identical; Markdown golden refreshed |
| Presentation tests | 2 decimals, integer hours, null/NaN/infinity, informational label, unit mismatch, name/GUID, count split, formula/thresholds |

The saved comparison remains 6 Match / 0 Warn / 10 Fail / 2 N/A; 4 Fail rows contribute to the numerical status and 6 are informational. Overall/Numerical Fail, Coverage Warn, Provenance/Reconciliation Pass are unchanged.

## Gaps / follow-up

- The UI grid and its 1180/820 screenshots belong to SAM_UI#238, which now uses fixed readable column widths and the grid's own horizontal scrolling. This repository has no UI layout code.
- Numerical discrepancy is not resolved here: TAS heating/cooling and peak values differ from OpenStudio (box example: heating 742.78 vs 2810.02 kWh, cooling 2156.92 vs 3832.50 kWh). The 871/872 heating-hour and cooling-peak discrepancies are separate engineering investigations. No tolerance or engine change is proposed.
- Per-engine input manifests remain unavailable (SAM_OpenStudio#30, SAM_UI#230).

## Next step

Owner review of this PR and SAM_UI#238. If both are approved, merge this presentation PR before SAM_UI#238 so the UI and Markdown use the same helpers, then rerun #238 CI. Do not merge this record automatically.
