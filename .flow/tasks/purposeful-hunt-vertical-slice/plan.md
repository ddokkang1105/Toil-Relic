# Plan

## Readiness

- Status: implementation-ready
- Canonical plan: `docs/plans/2026-08-06-001-feat-purposeful-hunt-vertical-slice-plan.md`
- Product requirements R1-R13, flows F1-F5, and acceptance examples AE1-AE8 remain the authority.
- Planning assumptions A1-A8 and technical decisions KTD1-KTD8 close all blocking design questions for the first slice.

## Steps

1. [x] U1 — Add mirrored native quarry, reward-profile, project, and equipment content plus shared parity vectors.
2. [x] U2 — Add strict raw save validation, console schema v1, Unity schema v3, and legacy migration tests.
3. [ ] U3 — Add pure atomic victory/Forge commands with deterministic reward rolls and typed outcomes.
4. [ ] U4 — Replace console random Hunt with the three-choice Contract flow and existing equipment handoff.
5. [ ] U5 — Add Unity manager authority, events, Camp-local controller behavior, and equipment handoff.
6. [ ] U6 — Generate and verify Unity data references, scene structure, navigation, and setup documentation.
7. [ ] U7 — Prove real serialized actions, two-viewport geometry, graphics captures, and full parity/regression behavior.

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
