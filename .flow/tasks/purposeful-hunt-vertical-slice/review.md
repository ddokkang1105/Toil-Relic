# Review

## Scope

- Compared task branch `codex/purposeful-hunt-vertical-slice` from base `415ab5f` through implementation head `51b5fb6`.
- Reviewed 84 tracked files and the inferred canonical plan at `docs/plans/2026-08-06-001-feat-purposeful-hunt-vertical-slice-plan.md`.
- Ran correctness, project-standards, testing, maintainability, learnings, API-contract, reliability, and local adversarial review passes. A different-provider cross-model pass was unavailable because no supported peer CLI was installed.
- The review was report-only. No production code was changed in this stage.

## Primary findings

| # | Severity | Finding | Disposition | Verification |
|---|---|---|---|---|
| 1 | P1 | `src/ToilRelic/Game.cs:193` returns a save-worthy result after a rejected victory reward, so `Game.Run` still calls `SaveProgress` even though the command reports no state change. | Reopen work. Return a no-save outcome and add a stale-content byte-preservation regression. | Focused console Hunt UX test plus full console suite. |
| 2 | P1 | `src/ToilRelic/Models/HuntContract.cs:71` validates only catalog existence, not the fixed relic/profile equipment roles, allowing catalog-valid misconfiguration to violate the First Relic Project contract. | Reopen work. Enforce the Toilbound Relic/Necklace forged role and reject reserved relic/reward items from profiles in both runtimes. | Mirrored invalid-content and command tests in console and Unity. |
| 4 | P1 | `unity/Assets/Scripts/Data/HuntContractData.cs:78` dereferences serialized quarry/profile/enemy lists before proving they are non-null, turning malformed content into an exception instead of a typed unavailable result. | Reopen work. Add null-safe collection validation and database lookup behavior. | Focused Unity StartHunt malformed-asset tests. |
| 6 | P1 | `unity/Assets/Tests/PlayMode/PurposefulHuntActionPlayModeTests.cs:151` forces save failure through Rest, so victory/Forge mutation-first persistence is not exercised at the feature boundaries. | Reopen work. Force failure after serialized victory and Forge, then prove visible in-memory state and later exact reload. | Dedicated action category twice in fresh Unity processes. |
| 7 | P1 | `unity/Assets/Tests/PlayMode/PurposefulHuntDomainPlayModeTests.cs:160` covers only a small subset of the strict current-schema rejection matrix required by U2. | Reopen work. Add mirrored table-driven missing/null/wrong-kind/partial, contribution, invariant, version, and mixed-legacy cases with byte preservation. | Focused migration suites in both runtimes plus full regressions. |
| 8 | P2 | `src/ToilRelic/Systems/CombatSystem.cs:76` introduces `combat` and `loot` private fields instead of the repository-required `_camelCase` names. | Reopen work. Rename to `_combat` and `_loot`. | Console build. |
| 10 | P2 | `unity/Assets/Scripts/Save/SaveService.cs:166` searches the whole JSON document for `forged`, so an unrelated boolean can satisfy the nested `relicProject.forged` presence requirement. | Reopen work. Make required-field checks object-scoped and add a misleading-sibling regression. | Focused Unity v3 migration test plus full Play Mode suite. |

## Requirements completeness

| Requirement or unit | Status | Review evidence |
|---|---|---|
| R1-R7, R9-R10 | Met | The implemented content, selection, reward, replay, Forge gating, and semantic-feedback paths match the plan in reviewed production code and tests. |
| R8 | Partial | Finding #2 leaves the fixed forged-reward role insufficiently enforced for malformed but catalog-valid content. |
| R11 | Partial | Findings #7 and #10 leave required current-schema rejection behavior and proof incomplete. |
| R12-R13 | Partial | Findings #1, #2, and #4 leave invalid-content no-save behavior and console/Unity failure parity incomplete. |
| U1 | Partial | Findings #2 and #4. |
| U2 | Partial | Findings #7 and #10. |
| U3 | Met | Atomic domain command structure and deterministic outcome vectors are present. |
| U4 | Partial | Findings #1 and #8. |
| U5-U6 | Met | Runtime authority, generated scene/data wiring, and controller boundaries are present. |
| U7 | Partial | Finding #6 leaves feature-specific save-failure acceptance proof incomplete. |

## Triage order

1. Fix the console no-save rejection and equipment-role validation (#1, #2).
2. Harden Unity malformed-content and current-schema validation (#4, #10).
3. Complete mutation-first save-failure and migration-matrix evidence (#6, #7).
4. Apply the behavior-preserving naming cleanup (#8).

## Residual risks and testing gaps

- Console validation authority is split across save and command systems, and console Forge still reaches the static production profile set instead of the injected Hunt profile boundary. These are maintainability/testability risks, not independently validated release blockers.
- Unity still writes directly to the target save file. Prior-file survival during a mid-write failure remains explicitly deferred by the plan.
- The serialized action journey arranges remaining project contributions directly before Forge and does not replay a completed quarry through a second real victory. This is acceptable for the focused action contract but leaves broader end-to-end replay coverage as a follow-up gap.
- Rapid repeated Open/Confirm/Forge/Equip actions and intermediate zero/one/two/Ready/Keep reload milestones are not exercised.

## Past solutions applied

- `docs/solutions/best-practices/deterministic-unity-playmode-action-contracts.md`
- `docs/solutions/architecture-patterns/pure-equipment-preview-with-commit-revalidation.md`
- `docs/solutions/ui-bugs/unity-status-event-save-failure-contracts.md`
- `docs/solutions/workflow-issues/resume-blocked-personal-flow-qa-with-layered-environment-validation.md`
- `docs/solutions/design-patterns/width-first-unity-ui-virtual-layout-floor.md`

## Result

`rework required`

Seven validated findings remain: five P1 and two P2. Return the task to `work`; do not advance to QA until the fixes and focused regressions are complete.
