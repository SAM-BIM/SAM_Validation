# S06 - revision fields in installer builds (PR record)

This PR is a documentation-only part of roadmap S06 (`SAM/documentation/SIMULATION_ROADMAP_Q4_2026.md`).
- Branch: `docs/s06-installed-build-provenance`, targeting `sow/2026-Q4` (base `68eded00`).
- Related PRs: SAM-BIM/SAM_Deploy#66 (the S06 installed candidate) and SAM-BIM/SAM_OpenStudio#32.
- Merge needs owner approval. `PROJECT_PROGRESS.md` is not touched; it gets a closeout only after the merge.

## Status
Ready for owner review. The owner decided on 2026-10-20 to accept the installer-build provenance limitation as an
explicitly documented S06 exception. New repository stamping is not to be implemented.

## Work completed
`SCHEMA.md` now covers installer builds. Its provenance table and a new section, "Revision fields in installer builds",
state the following:
- In a SAM_Deploy installer build without overrides, `samCommit` and `runnerCommit` hold the **SAM_Deploy build
  revision**, because every assembly carries the SAM_Deploy commit as its InformationalVersion metadata.
- The field alone does not tell the two kinds of revision apart. The installer stamp does: the InformationalVersion
  `<year>.<quarter>.<run>.0+<revision>`. SAM_UI's run manifest records it under `provenance.application`:
  - the full stamp in `entryAssembly.informationalVersion` and each `components[].informationalVersion`;
  - the extracted SHA in `sourceRevision`.

  `sourceRevisionOf` is explanatory text only. (Corrected after review: an earlier head, `cb84fd9`, said the stamp was
  in `sourceRevisionOf`.)
- The actual revisions come from the pinned gitlinks: `git ls-tree <SAM_Deploy revision> SAM SAM_Tas SAM_OpenStudio`.
- Existing documents keep their meaning. Developer runs with `SAM_COMMIT` (S05 and earlier) hold repository revisions.
  No historical value is redefined.

## Evidence
On the S06 installed candidate (SAM_Deploy `717533c`, hosted installer run #227, genuine install), both producers reported
`samCommit` = `runnerCommit` = `717533c`. The gitlinks of `717533c` give SAM `df27563b`, SAM_Tas `e84aeff6` and
SAM_OpenStudio `1f4bf60c`. Evidence is in SAM_Deploy#66, `evidence/s06-20261020/runs/x3-installer-both-embedded/`.
The committed genuine-installer manifests (x1 `box.tas-run.json`, x2 `openstudio-run.json`, x3 `engine-comparison-run.json`)
record `provenance.application.sourceRevision` = `717533c`. Their `entryAssembly.informationalVersion` and every
`components[].informationalVersion` are `2026.4.227.0+717533c`. Their `sourceRevisionOf` is the explanatory sentence.

## Decisions and assumptions
- This is documentation only. The schema version, the validators and the producers are unchanged. A field naming the
  revision kind, or per-repository stamping, would be a separate schema or build change.
- The S06 provenance check is recorded as an **accepted limitation**, not as PASS.

## Files changed
- `docs/benchmark/SCHEMA.md`
- `docs/benchmark/S06-INSTALLED-PROVENANCE-PR.md` (this record)

## Validation
- The diff touches only these two documentation files.
- The `#revision-fields-in-installer-builds` and `#version-and-compatibility-policy` anchors resolve to headings in `SCHEMA.md`.
- The producers' resolution order was checked against `Program.cs` in SAM_Tas (`benchmark/SAM.Analytical.Tas.Benchmark.Cli`)
  and SAM_OpenStudio (`benchmark/SAM.Analytical.OpenStudio.Benchmark`).
- The stamp format was checked against SAM_Deploy `installer.yml` "Compute SAMVersion".
- The manifest field names and values were checked against the committed SAM_Deploy#66 manifests listed under Evidence.
- Hosted CI: `cb84fd9` passed Build (Windows) (SAM_Validation run 39398892361) and spdx. Later heads: see the PR description.

## Unresolved
None for this PR. SAM_Deploy's `SAM_Validation` gitlink stays at `68eded00`: this change ships no binary.

## Next step
Owner review and merge. Then the SAM_Validation `PROJECT_PROGRESS.md` closeout, a docs-only `[skip ci]` commit with the
merge SHA.
