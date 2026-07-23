# Review

## Scope

- Base: `3cd3b96` (`Record save validation re-review`)
- Reviewed head: `33d07cddc68190374db31cede517e9162fae8f05`
- Branch: `codex/save-ux-reliability`
- Mode: report-only focused re-review
- Plan: `docs/plans/2026-07-22-001-feat-save-ux-reliability-plan.md` (`ce-unified-plan/v1`, implementation-ready)
- Review run: `C:\Users\User\AppData\Local\Temp\compound-engineering-codex\ce-code-review\20260723-105742-cfa9b66c`

## Findings

| # | Severity | Finding | Disposition | Verification |
|---|---|---|---|---|
| 1 | P1 | `SaveSystem.Load` still accepts `Experience >= RequiredExperience(Level)`. `Player.FromSaveData` silently clamps the value, returns `Loaded`, enables Continue, and permits a later save to replace the originally impossible bytes. | Reopen `work`. Share or expose the canonical required-experience calculation, reject an experience value at or above the current level threshold before materialization, and add direct-load plus real Title-flow byte-preservation fixtures. | The correctness reviewer traced Level 1 / Experience 20 through the new guard and existing clamp. A fresh validator confirmed that every production transition preserves `0 <= Experience < RequiredExperience(Level)`, so this is a persisted-state invariant rather than a new contract. |
| 2 | P1 | The advertised versionless compatibility test removes only the `version` field from a modern payload. The original Unity writer at `8b0be99` stored `score` and had no `level` or `experience`, so the unconditional modern presence check still classifies a genuine historical save as `Unreadable`. | Reopen `work`. Make version-0 presence and conversion handling explicit for the actual historical field set, keep versions 1 and 2 strict and current writes at version 2, and add a fixture derived from `8b0be99` that proves load/Continue/normalization and byte preservation. | The API-contract reviewer compared the current probe with the original `SaveEnvelope` and `PlayerState`. A fresh validator independently confirmed the authentic versionless shape is rejected at `SaveService.cs:97`. |

## Previous finding disposition

| Previous finding | Status | Evidence |
|---|---|---|
| Console impossible values normalized to `Loaded` | Partially resolved | HP, level, negative counters, invalid inventory keys, and negative amounts are now rejected and covered. Finding #1 identifies the remaining experience upper-bound normalization path. |
| Unity required-field omission hidden by `JsonUtility` defaults | Resolved for the modern v1/v2 core shape | Sentinel-based raw presence checks and per-field omission cases reject modern partial payloads before hydration. |
| Exact version-2 gate rejected v1/versionless saves | Partially resolved | Versions 0, 1, and 2 now pass the version gate and version 1 has a compatibility fixture, but finding #2 shows the fixture does not represent the original versionless schema. |

## Requirements completeness

- Partial: R1-R3 and AE3. Two parseable but unsupported states can still be classified incorrectly: an impossible console experience value becomes `Loaded`, and an authentic historical Unity save becomes `Unreadable`.
- Partial implementation units: U1 still needs the experience upper-bound invariant; U3 still needs genuine version-0 schema compatibility.
- Met and unaffected by this focused re-review: R4-R14, AE1-AE2, AE4-AE8, and implementation units U2, U4, and U5.

## Review coverage

- Lenses: correctness, project standards, testing, maintainability, reliability, save/API contract, adversarial fallback, and repository learnings.
- Cross-model review was not run because no authenticated different-provider CLI was available; the in-process adversarial fallback ran instead.
- Mechanical merge: 4 raw findings -> 4 candidates. One maintainability candidate was demoted to residual risk because the Unity fixture already exceeded 1,000 physical lines at the review base; one adversarial P2 was dropped after validation could not establish how this Unity version handles a present non-integer `version` token.
- Validator batch: 3 findings selected, 2 validated, 1 dropped, 0 validation-degraded.
- Project-standards, testing, and reliability reviewers found no additional actionable defect.
- No source fixes were applied during review.

## Learnings and past solutions

- `docs/solutions/ui-bugs/unity-status-event-save-failure-contracts.md` supports keeping persistence semantics separate from player copy and proving behavior through the real `GameManager` path.
- `docs/solutions/best-practices/unity-playmode-hud-contracts.md` supports the current reflection-based PlayMode boundary and retaining both direct load matrices and a real scene/Title flow.

## Residual risks

- The repository contains no authoritative historical version-1 writer, so the modern-core v1 fixture is the strongest available local compatibility evidence.
- `SampleSceneP0PlayModeTests.cs` was already 1,045 physical lines at the base and grew by 128 lines to 1,173; splitting save-contract coverage remains useful maintenance work but is not a release blocker for this rework.
- Unity accepts experience values at or above the current level threshold; that behavior predates this focused diff and should be resolved only if the shared save-state invariant is intentionally expanded to Unity.

## Testing gaps

- Add Unity wrong-JSON-kind fixtures for required scalar/list fields and `version` to establish this Editor version's `JsonUtility` behavior rather than relying on external serializer assumptions.

## Result

`rework required`
