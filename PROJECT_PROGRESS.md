# Project Progress - SAM_Validation (2026-Q4)

## Q4 comparison presentation PR15 closeout - 2026-10-17

- Status: SAM-BIM/SAM_Validation#15 merged into `sow/2026-Q4` as merge commit `912ae94ef68af20b437fc92ec7e8891fd1cb640f` (parents: base `8fa79f5c8559c2db11e0ce2a26193e5ccad80e79` and approved exact head `4a7da6b1821d303d183fe87d275b5d0aaf05460e`). Owner explicitly authorized this pin and order in the gated merge instruction. Immediate live recheck: not draft, MERGEABLE/CLEAN, Build/SPDX success, no submitted reviews or review threads; no branch protection/rules required an additional submitted approval. Merge used `gh pr merge 15 --repo SAM-BIM/SAM_Validation --merge --match-head-commit 4a7da6b1821d303d183fe87d275b5d0aaf05460e`; no auto-merge or administrative bypass.
- Completed: shared two-decimal presentation helpers, authoritative contributing/informational Fail counts, Markdown context and fractional-hour threshold correction. Files: comparator `Classes/PresentationFormat.cs`, `Classes/ComparisonResult.cs`, `Modify/WriteMarkdown.cs`; tests `PresentationFormatTests.cs`, `ReportTests.cs`, `ExtractionRegressionTests.cs`; four Markdown goldens; record `docs/benchmark/COMPARISON-PRESENTATION-PR.md`. CSV/JSON contracts, physics, units, tolerances, gates and comparison arithmetic unchanged. Tested content `a39e4a9fd11019de9400f62fe5e68179539a4661`; final candidate differs only by its committed record.
- Hosted checks inspected: final-head Build [39068752757](https://github.com/SAM-BIM/SAM_Validation/actions/runs/39068752757) and SPDX [39068752748](https://github.com/SAM-BIM/SAM_Validation/actions/runs/39068752748) PASS. Post-merge Q4 Build [39070909651](https://github.com/SAM-BIM/SAM_Validation/actions/runs/39070909651) PASS, including comparator 100/100 and public API 2/2, zero skipped. Existing SPDX workflow manually dispatched at the merge SHA because it is PR/dispatch-only: [39071240235](https://github.com/SAM-BIM/SAM_Validation/actions/runs/39071240235) PASS. Final-head and post-merge logs select SAM and SAM_Mollier `sow/2026-Q4`, SAM_IAPWS default branch. Reproduce tests: `dotnet test SAM.Analytical.Benchmark.Compare.Tests -c Release`; `dotnet test SAM.Analytical.Benchmark.Compare.ApiTests -c Release`. Hosted tests executed by CI; no local suite/build or real-engine simulation was rerun in this closeout session. Earlier local build/test results are inspected evidence in the PR record.
- Evidence audited locally: `C:\TasOut\pr238-ui\PR238-BOTH-REPORT.md` and its successful `box-both`/partial run artifacts (machine-local, must be copied/provisioned elsewhere). Saved box CSV SHA-256 `8092B384DD9D9BA9289E1C10B77DB7C93BCD95E911B4A0F32CEE93373C0ECD3D`; summary JSON `36B9D353B35CDFEB1FBAC70B78317BF36C4F393159F114FCF39716D825FF4A98`, independently rechecked. Overall/Numerical Fail, Coverage Warn, Provenance/Reconciliation Pass remain diagnostic findings, not engineering equivalence.
- Cleanup: task remote `claude/tas-openstudio-comparison` was unprotected, not default/quarterly/shared, exactly at the approved head, completely merged into remote Q4, with no open PR using it as head/base and no unmerged local work. Deleted with `--force-with-lease=refs/heads/claude/tas-openstudio-comparison:4a7da6b1821d303d183fe87d275b5d0aaf05460e`; verified remote ref absent. Clean primary checkout fast-forwarded to Q4, task local branch deleted with `git branch -d` after leaving it. All worktrees and frozen quarters preserved. This removes the same-name dependency shadow before downstream CI refresh.
- Remaining: numerical engine disagreement, HungaryHouse Tas crash, unavailable per-engine input manifests and producer deployment are unresolved follow-ups. UI#237 and #238 are held: G1 native cancellation/process cleanup, concurrent-edit isolation and Tas before/after Q4 baseline comparison lack evidence or explicit owner deferral. Existing box standalone/benchmark four-metric equality is evidenced; owner must decide whether it substitutes for the prescribed explicit-EPW fixture. Full WPF 2835/2835 alone does not establish the opt-in real-run test executed.
- Exact next step: resolve the UI acceptance decisions/evidence and obtain owner approval of documentation-corrected UI pins (#237 `b7483443f53dd53a4d16f159d6bc327c901a7770`, #238 `c7bb3d188002cda0fb8f95308eff1db142ffbc83`). Recheck final-head CI/dependency logs and all gates before #237, then #238. The originally approved UI pins have not been merged. This docs-only post-merge entry is the only base file change.
## Q4 agent instructions rollout closeout - 2026-10-12

- SAM-BIM/SAM_Validation#13 merged as `4184366b3660c51a428f1fd8f8662333e9b8c894` (merge commit pinned to approved head `3db112f64002098348b8dd5f17864bb13e19d8fe`; base `sow/2026-Q4` `b7d168dc7dfde93897935c527b24abdbfa4c750b`; validated content `5fa1906eb09fc2fb02e4c429b056bf5f8273ab2b`). Owner-authorized 2026-10-12 after an independent review (`AGENT_ROLLOUT_REVIEW_2026-10-12.md`, local to the owner's workspace, not committed).
- Docs only: AGENTS.md, CLAUDE.md, plus the rollout record `docs/Agent-Continuity-Rollout-Q4.md`. No product code, build, workflow, dependency pin or tool changed; this closeout is the only `PROJECT_PROGRESS.md` change.
- Validation: local documentation checks and commands are in `docs/Agent-Continuity-Rollout-Q4.md` (`git diff --check`, file scope, UTF-8/LF/no BOM, import). Hosted checks at the approved head: build success (run 38258121344); spdx success (run 38258121340). Dependency selection: dependency clones in the build log: SAM @ sow/2026-Q4, SAM_IAPWS @ default, SAM_Mollier @ sow/2026-Q4.
- Cleanup: remote docs/agent-instructions-rollout-q4 deleted with an expected-SHA lease on 3db112f64002098348b8dd5f17864bb13e19d8fe after confirming it is contained in sow/2026-Q4 and no open PR uses it as head or base; the local branch and its worktree are preserved (worktree in use). Other branches, worktrees and the frozen quarters were not touched.
- Remaining gaps: no agent-client behaviour or token benchmark has been run.
- Exact next step: Continue from the shared `AGENTS.md`. Open item carried from the pilots: verify on each installed agent client, in a fresh Q4 session, that it follows the instructions without loading the full progress history, and record the result.

## Branch

`sow/2026-Q4` - bootstrapped 2026-10-06 from `master` `662d7255`. Frozen Q3 record: `sow/2026-Q3` @ `31492d63` (not modified).

## Last updated

2026-10-06 (Q4 operational cleanup).

## Current status

Q4 branch cut from `master` `662d7255`, which is the exact commit pinned in SAM_Deploy's frozen Q3 baseline (`v20261006.1`). Bootstrap added only internal docs (this file, `AGENTS.md`). No product source changed. No Q4 product work has started.

## Q4 priorities

Not yet set by the owner. Record them here at the first Q4 planning pass. Known carry-over work is listed below.

## Known carry-over work

- None identified for this repository at bootstrap.

## Repository-specific next steps

- Await Q4 planning. Open PRs for Q4 work against `sow/2026-Q4`.
- Follow the continuity convention in `AGENTS.md` for every PR and closeout.

## Decisions / assumptions

- Q4 base is `master` `662d7255`; the internal files were recovered from `sow/2026-Q3` into this branch only, never onto `master`.
- Q4 history intentionally does not contain the Q3 branch history (the maintained `master` is the promoted Q3 line, which is not a descendant of `sow/2026-Q3`); the frozen `sow/2026-Q3` branch is the permanent record.
- Historical Q2/Q3 content below is kept as evidence; its branch names, SHAs and next steps describe Q3 and are not current instructions.

## Validation

- Bootstrap verified 2026-10-06: `sow/2026-Q4` was created at exactly `662d7255` and the push was a normal (non-forced) branch creation.

## Issues / blockers

- None at bootstrap.

## Next step

- Owner to set Q4 priorities; then start the first Q4 task from this branch.

## Q4 operational cleanup (2026-10-06)

- Reviewed every active Q2/Q3 reference in this repository on `sow/2026-Q4` (workflow branch filters, dependency-branch resolution, `.gitmodules`/validation, docs). Historical Q2/Q3 mentions (feature documentation records, the frozen Q3 section below) are intentionally unchanged.
- Changed (`6fe5191`): removed the dead `$candidates += 'sow/2026-Q2'` fallback from the dependency-branch resolution in `.github/workflows/build.yml`. No dependency repository has a `sow/2026-Q2` branch, so the entry never matched and resolution already fell through to the default branch; behaviour is unchanged (PR head ref, current sow ref, then the dependency's default branch) and no per-quarter edit is needed.
- Checked, no action: the `github.repository_owner == 'SAM-BIM'` build guard (intentional; its comment names HoareLea only to explain why the guard exists), CODEOWNERS (SAM-BIM owners), and workflow secrets (no HoareLea-named secret). The local `upstream` (HoareLea) remote is preserved.
- Full cross-repository record, migration table and owner decisions: `SAM_Deploy:sow/2026-Q4` `PROJECT_PROGRESS.md`.

---

# Historical record - 2026-Q3 (frozen)

Source: last revision of the file on `sow/2026-Q3`, commit `002ff0e` (the file was removed from the Q3 tip by `198a516`; `sow/2026-Q3` tip is `31492d63`). The Q3 file was an unpopulated template ("Not updated yet"); no Q3 work was recorded in this repository's progress file.
