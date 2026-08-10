# Plan

## Readiness

- Status: implementation-ready
- Canonical plan: `docs/plans/2026-08-06-001-feat-purposeful-hunt-vertical-slice-plan.md`
- Product requirements R1-R13, flows F1-F5, and acceptance examples AE1-AE8 remain the authority.
- Planning assumptions A1-A8 and technical decisions KTD1-KTD8 close all blocking design questions for the first slice.

## Steps

1. [x] U1 — Add mirrored native quarry, reward-profile, project, and equipment content plus shared parity vectors.
2. [x] U2 — Add strict raw save validation, console schema v1, Unity schema v3, and legacy migration tests.
3. [x] U3 — Add pure atomic victory/Forge commands with deterministic reward rolls and typed outcomes.
4. [x] U4 — Replace console random Hunt with the three-choice Contract flow and existing equipment handoff.
5. [x] U5 — Add Unity manager authority, events, Camp-local controller behavior, and equipment handoff.
6. [x] U6 — Generate and verify Unity data references, scene structure, navigation, and setup documentation.
7. [x] U7 — Prove real serialized actions, two-viewport geometry, graphics captures, and full parity/regression behavior.

## Affected paths

- Console runtime: `src/ToilRelic/Models/`, `src/ToilRelic/Systems/`, `src/ToilRelic/Game.cs`, `src/ToilRelic/Util/ConsoleUI.cs`
- Console tests: `tests/ToilRelic.Tests/`
- Unity runtime/data/UI/save: `unity/Assets/Scripts/`
- Unity generated content and scene: `unity/Assets/Generated/`, `unity/Assets/Scenes/SampleScene.unity`, `unity/Assets/Editor/ToilRelicSceneBootstrap.cs`
- Unity tests and parity fixture: `unity/Assets/Tests/EditMode/`, `unity/Assets/Tests/PlayMode/`, `unity/Assets/Tests/Fixtures/`
- Documentation: `unity/UNITY_SETUP.md`, `CONCEPTS.md`

## Validation

- Build and run the complete console test suite, including focused content/save and Hunt/equipment UX filters.
- Regenerate the Unity scene, run full Edit Mode tests, run the dedicated action category twice in fresh processes, then run the full Play Mode suite.
- Capture and inspect all six named states at 1280x720 and 800x600; validate files are current, correctly sized, and non-uniform.
- Run both native readers against the shared content, migration, and pure-command vectors.
- Manually complete new/legacy load, three distinct victories, replay, Forge, Keep, Equip, reload, and save-failure recovery in isolated storage.

## Rollback or migration

- Console writes schema v1; Unity writes schema v3 only after a normal save-triggering action. Valid legacy loads initialize empty project state without rewriting source bytes at first Camp render.
- Invalid current or mixed-version payloads remain unreadable; normalization must not repair them before validation.
- Gameplay commands resolve completely before mutation. A domain precondition failure leaves state unchanged; a save failure after a successful mutation keeps in-memory state and reports failure last.
- If implementation must be rolled back, remove the Hunt/Forge production wiring and new generated assets together while preserving legacy equipment catalog compatibility and existing saves. Atomic file replacement remains deferred to Ironclad Save Envelope.

## Work evidence

- U1: Console content tests observed the expected compile-time red failure before implementation, then 4 focused tests and the 55-test console suite passed. Unity content tests observed missing native types before implementation, then 2 focused Play Mode tests, the catalog fixture regression, and the generated-data Edit Mode contract passed. Two bootstrap runs produced one contract, one profile database, and three profile assets without duplication.
- U2: Console project tests observed missing project-state APIs before implementation; all 63 console tests then passed with schema v1, legacy defaulting, canonical contribution order, and corrupt-current rejection. Unity tests observed missing project state and v2 behavior before implementation; 5 focused tests and the 68-test Play Mode suite passed with v3 round-trip, v0-v2 byte-preserving legacy load, and mixed/current rejection.
- U3: Shared command vectors cover below/at/above 35% rolls, replay, already-owned profile rewards, third-contribution readiness, non-victory, and invalid input. Console passed 65 tests; Unity passed the focused 6-test domain fixture and the full Play Mode suite. Reward and Forge commands construct post-state before committing, return typed facts, never grant the fixed Reward Weapon, and leave rejected/unready/repeated/conflicting inputs byte-equivalent with no save request.
- U4: Four deterministic console Hunt/Forge UX tests observed the missing orchestration seam before implementation, then passed with readable 35% profile odds, guaranteed first-win contributions, replay labels, selected-quarry identity retention, defeat with no rewards, and Ready-only Forge handoff into Necklace comparison. The full 69-test console suite and a temporary-directory production new-game/save/quit lifecycle passed; legacy Reward Weapon equipment tests remain green while production victory no longer grants it.
- U5: Three focused Unity runtime tests passed for confirmed second-quarry retention, immutable reward authority after live profile mutation, stale-confirm rejection, Camp-local controller open/select/cancel cleanup, Forge persistence, and no auto-equip. The six-test shared domain fixture also remained green. GameManager now owns validated snapshots and atomic reward/Forge commands, publishes typed Contract/project/equipment-focus events, removes automatic Reward Weapon victory grant, and keeps save failure as the final feedback boundary.
- U6: The bootstrap was regenerated twice without duplicate production assets. Two full Edit Mode runs passed (2/2) and proved committed/disposable equivalence for the new Hunt Contract hierarchy, initial Camp/Contract/Equipment exclusivity, GameManager contract/profile references, controller references, persistent Hunt/Craft/Back/Confirm/Forge targets, geometry, and explicit navigation. The committed scene now includes the project HUD line and generated Contract/Forge surface; UNITY_SETUP documents save v3, live profiles, focused checks, and evidence commands.
- U7: Dedicated serialized action tests passed twice in fresh Unity processes (2/2 each; the conditional graphics capture skipped without its evidence variable). The final full regressions passed with Console 69/69 and a warning-free build, Unity Edit Mode 2/2, and Unity Play Mode 73 passed/0 failed/2 conditional skips out of 75. A graphics-enabled capture produced and visually verified all 12 expected non-uniform PNGs at 1280x720 and 800x600; geometry assertions cover 800x450 and 800x600 protected HUD/status gaps, 44px controls, optional/guaranteed/replay labels, invalid-content, save-failure, forged, and relic-preview states.

## Review rework

1. [x] RW1 - Make rejected console victory content return a no-save outcome and prove source save bytes remain unchanged.
2. [x] RW2 - Enforce the fixed forged-relic and profile-equipment roles in both runtime content validators.
3. [x] RW3 - Make Unity malformed serialized content return typed unavailability and make v3 required-field checks object-scoped.
4. [x] RW4 - Add feature-boundary save-failure action coverage for victory and Forge, including later exact reload.
5. [x] RW5 - Complete the mirrored strict current-schema rejection matrix with byte-preservation assertions.
6. [x] RW6 - Rename the new console runtime private fields to the required `_camelCase` form.
7. [ ] RW7 - Reject delayed Hunt confirmation unless the submitted quarry and revision still belong to the actively presented Contract snapshot.
8. [ ] RW8 - Restore Hunt-entry focus after the serialized Cancel action despite the synchronous Contract-close event.
9. [ ] RW9 - Preserve third-victory Win, reward, Ready, and level-up facts through project publication and add focused visible-action regressions for RW7-RW9.

## Rework evidence

- RW1/RW2/RW6: the proof-first console Hunt/content run failed 2 of 10 tests before the fixes, then passed 10/10. Rejected stale victory content now returns no-save and preserves the existing save bytes; the content validator enforces the fixed Toilbound Relic/Necklace role and forbids reserved profile rewards; new runtime private fields use `_camelCase`.
- RW3/RW5: the focused Unity validation run failed 10 of 32 cases before the fixes (22 passed), then passed 32/32. Null serialized collections return typed unavailability, required v3 fields are checked inside the correct `player` and `relicProject` objects, and misleading-sibling, missing/null/wrong-kind, contribution, invariant, version, and mixed-legacy inputs remain unreadable without rewriting source text. The mirrored console migration suite passed 25/25.
- RW4: serialized victory and Forge actions now force write failure after successful mutation, prove the contribution/relic is applied exactly once and remains visible, then restore the path and prove exact reload. After simplification, the dedicated action category passed twice in fresh Unity processes with 4 passed, 0 failed, and 1 conditional graphics-capture skip per run.
- Simplification: `ce-simplify-code` applied 2 reuse improvements and 4 quality improvements, including canonical save generation, shared content IDs, dead-probe removal, contract-focused action tests, common save-failure helpers, and repository brace style. Three low-value/risky suggestions were skipped: moving the schema matrix into the cross-runtime content fixture, replacing its explicit case switch with test-data indirection, and rewriting the safety scanner for one-time-load micro-optimization.
- Final regression: console tests passed 86/86 and `dotnet build` completed with 0 warnings and 0 errors. Unity Edit Mode passed 2/2. Unity Play Mode passed 101 tests with 0 failures and 2 conditional graphics-capture skips out of 103.
- Rework commits: `d9d8862`, `e9720dc`, `88b4268`, and `4edf29f`.
