# Review

## Scope

- Base: `cb7243a` (`Record save validation re-review`)
- Reviewed head: `b0bb30e3a6a1e52d3529a9fa6dc528ce900a9406`
- Branch: `codex/save-ux-reliability`
- Mode: report-only focused re-review
- Plan: `docs/plans/2026-07-22-001-feat-save-ux-reliability-plan.md` (`ce-unified-plan/v1`, implementation-ready)
- Review run: `C:\Users\User\AppData\Local\Temp\compound-engineering-codex\ce-code-review\20260723-130521-365d9130`

## Findings

No actionable findings.

Two P1 candidates entered independent validation and were rejected:

| Candidate | Disposition | Verification |
|---|---|---|
| Unity save validation no longer matches console | Rejected | The previous review explicitly retained Unity threshold acceptance as a pre-existing residual risk outside the focused console fix. The explicit plan does not require byte-identical cross-runtime value validation. |
| Version-gated save cases remain unproven | Rejected as a blocker; retained as testing gaps | `SaveService` mechanically restricts the historical score shape to version 0 and requires the modern core for versions 1 and 2. Existing positive, omission, Title, Continue, and byte-preservation cases prove the core behavior; missing cross-product cases are non-blocking coverage gaps. |

## Previous finding disposition

| Previous finding | Status | Evidence |
|---|---|---|
| Console `Experience >= RequiredExperience(Level)` normalized to `Loaded` | Resolved | `SaveSystem.HasValidCoreValues` now uses the canonical player threshold before materialization. Direct-load and real Title-flow fixtures reject Level 1 / Experience 20 and preserve bytes. |
| Authentic Unity versionless save rejected by modern-only presence checks | Resolved | Version 0 accepts either modern fields or the original `score`-based field set. The authentic fixture loads, enables Continue, normalizes to Level 1 / Experience 0, enters Camp, and preserves its bytes. |
| Unity required-field omission hidden by `JsonUtility` defaults | Remains resolved | Sentinel-based presence checks still reject modern partial payloads before hydration. |
| Exact version-2 gate rejected v1/versionless saves | Remains resolved | Versions 0, 1, and 2 are supported; unknown versions remain unreadable; current writes remain version 2. |

## Requirements completeness

- Met: R1-R3 and AE3 for this focused rework.
- Met: U1 now enforces the console persisted-experience invariant before normalization.
- Met: U3 now handles the authentic version-0 schema while keeping versions 1 and 2 strict and writes at version 2.
- Met and unaffected: R4-R14, AE1-AE2, AE4-AE8, and implementation units U2, U4, and U5.

## Review coverage

- Lenses: correctness, project standards, testing, save/API contract, reliability, adversarial fallback, and repository learnings.
- Cross-model review was not run because no authenticated different-provider CLI was available; the in-process adversarial fallback ran instead.
- Mechanical merge: 2 P1 candidates, 0 malformed returns, and 0 confidence suppressions.
- Validator batch: 2 selected, 0 validated, 2 dropped, and 0 validation-degraded.
- Console verification at reviewed HEAD: 40 passed, 0 failed. `git diff --check cb7243a..HEAD` passed.
- Unity evidence at reviewed HEAD: focused 4 passed, 0 failed; full PlayMode 35 passed, 0 failed, 1 optional layout capture skipped.
- No source fixes were applied during review.

## Learnings and past solutions

- `docs/solutions/best-practices/unity-playmode-hud-contracts.md` supports reflection-based whole-scene PlayMode tests covering classification, Continue, normalized state, Camp entry, and persisted bytes.
- `docs/solutions/ui-bugs/unity-status-event-save-failure-contracts.md` supports keeping persistence outcomes typed and separate from display wording and incidental event order.

## Residual risks

- Unity `JsonUtility` handling for present required fields with the wrong JSON kind remains engine-specific and unverified.
- Unity accepts experience values at or above the current-level threshold; this is an explicitly retained pre-existing difference and should change only if the shared save-state invariant is intentionally expanded.
- Committed Unity XML evidence does not embed the reviewed commit SHA, so exact run-to-commit provenance also relies on the work artifact and repository history.

## Testing gaps

- Restore a direct modern-core versionless PlayMode case.
- Add score-only version-1 and version-2 rejection cases with Title state and byte preservation.
- Add missing-field cases for the historical version-0 predicate and wrong-JSON-kind fixtures for `JsonUtility`.
- Optionally add higher-level and strictly-above-threshold console experience boundary cases; the canonical shared helper already makes the implemented inequality mechanically correct.

## Result

`approved`
