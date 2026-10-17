# Comparison presentation polish (SAM_UI#238 companion)

Status: implemented on `claude/tas-openstudio-comparison`; follow-up to merged SAM_Validation#14. Not merged.
Base: `sow/2026-Q4` at `8fa79f5c8559c2db11e0ce2a26193e5ccad80e79` (merge of #14).
Tested content SHA: `7fd54b3d14dfeef280f4448117d744f3ae44dba9`. This record is the only change after it.

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

## Files changed

- `SAM.Analytical.Benchmark.Compare/Classes/PresentationFormat.cs` (new)
- `SAM.Analytical.Benchmark.Compare/Classes/ComparisonResult.cs`
- `SAM.Analytical.Benchmark.Compare/Modify/WriteMarkdown.cs`
- `SAM.Analytical.Benchmark.Compare.Tests/PresentationFormatTests.cs` (new)
- `SAM.Analytical.Benchmark.Compare.Tests/ReportTests.cs`
- `SAM.Analytical.Benchmark.Compare.Tests/ExtractionRegressionTests.cs`
- `SAM.Analytical.Benchmark.Compare.Tests/Fixtures/comparison.md`
- `SAM.Analytical.Benchmark.Compare.Tests/Fixtures/Regression/{fixtures,scenario,weather-mismatch}/comparison.md`

## Validation

| Check | Result |
|---|---|
| `dotnet test SAM.Analytical.Benchmark.Compare.Tests -c Release` | PASS 99/99 |
| `dotnet test SAM.Analytical.Benchmark.Compare.ApiTests -c Release` | PASS 2/2 |
| `SAM_Validation.sln` Release rebuild | PASS, 0 errors |
| Saved `box-both` documents | Markdown regenerated; CSV SHA-256 and summary JSON SHA-256 byte-identical to the saved run |
| Regression cases | CSV and summary JSON byte-identical; Markdown golden refreshed |
| Presentation tests | 2 decimals, integer hours, null/NaN/infinity, informational label, unit mismatch, name/GUID, count split, formula/thresholds |

The saved comparison remains 6 Match / 0 Warn / 10 Fail / 2 N/A; 4 Fail rows contribute to the numerical status and 6 are informational. Overall/Numerical Fail, Coverage Warn, Provenance/Reconciliation Pass are unchanged.

## Gaps / follow-up

- The UI grid is dense at narrow widths; at the default width all columns and wrapped notes are visible, while the narrow layout preserves notes and action buttons but compresses the numeric columns.
- Numerical discrepancy is not resolved here: TAS heating/cooling and peak values differ from OpenStudio (box example: heating 742.78 vs 2810.02 kWh, cooling 2156.92 vs 3832.50 kWh). The 871/872 heating-hour and cooling-peak discrepancies are separate engineering investigations. No tolerance or engine change is proposed.
- Per-engine input manifests remain unavailable (SAM_OpenStudio#30, SAM_UI#230).

## Next step

Merge SAM_Validation presentation before SAM_UI#238 so the UI and Markdown use the same presentation helpers; then rerun #238 CI. Do not merge this record automatically.
