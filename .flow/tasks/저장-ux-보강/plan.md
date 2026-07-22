# Plan

## Readiness

- Status: executable (`artifact_readiness: implementation-ready`, `execution: code`)
- Canonical plan: `docs/plans/2026-07-22-001-feat-save-ux-reliability-plan.md`
- Product Contract: unchanged; R1–R14, F1–F3, and AE1–AE8 are preserved.
- Open blockers: none.
- Confidence deepening: completed for implementation-unit traceability and save-data integrity boundaries.
- Headless document review: four planning fixes integrated; no actionable findings remain. Product, design, feasibility, and scope lenses completed; the coherence worker did not return, so equivalent ID/reference/terminology checks were completed inline.

## Steps

1. [x] **U1 — Console persistence outcomes and test foundation:** add typed load/save/delete outcomes, developer-only diagnostics, an explicit `net8.0` xUnit project, isolated-path tests, and current-format compatibility checks.
2. [x] **U2 — Console Title, autosave, recovery, and Quit UX:** inject `SaveSystem` into `Game`, retain production composition in `Program`, route Title by typed outcome, preserve unreadable bytes until New Game, and verify exact output/recovery/Quit under a serialized console test collection.
3. [x] **U3 — Unity typed load diagnosis and isolated persistence seam:** add semantic outcomes, install one scoped path override before scene load for existence/load/save/delete, keep mutation behind New Game authorization, and restore the fixture after every PlayMode scenario.
4. [x] **U4 — Unity save-feedback state and gameplay-message composition:** separate gameplay and save state, preserve failure until recovery, clear contextual success, keep existing autosave triggers, and verify ordinary/terminal/Battle transitions.
5. [x] **U5 — Unity bootstrap layout, generated scene, and viewport evidence:** grow the status region to 120px, add and wire the auxiliary row, regenerate the scene, and capture Title/Camp/Battle evidence at 800x600 and 1280x720.

## Affected paths

- `src/ToilRelic/Game.cs`
- `src/ToilRelic/Program.cs`
- `src/ToilRelic/Systems/SaveSystem.cs`
- `src/ToilRelic/Systems/` new semantic persistence result types
- `tests/ToilRelic.Tests/` new .NET 8 xUnit project, fixtures, and console save UX tests
- `unity/Assets/Scripts/Save/SaveService.cs` and new Unity-local semantic result types
- `unity/Assets/Scripts/Core/GameEvents.cs`
- `unity/Assets/Scripts/Core/GameManager.cs`
- `unity/Assets/Scripts/UI/GameStatusController.cs`
- `unity/Assets/Scripts/UI/TitleMenuController.cs`
- `unity/Assets/Editor/ToilRelicSceneBootstrap.cs`
- `unity/Assets/Scenes/SampleScene.unity`
- `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`

## Validation

- `dotnet build src/ToilRelic/ToilRelic.csproj`
- `dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj`
- Manual console coverage: missing, valid, unreadable, replacement failure, successful save, failure then recovery, and Quit.
- Unity 6000.3.19f1 batch scene regeneration followed by the full `ToilRelic.PlayModeTests` suite.
- Layout evidence through `TOIL_RELIC_LAYOUT_EVIDENCE_DIR` for Title, Camp, and Battle at 800x600 and 1280x720.
- Scene-diff inspection plus R1–R14 / AE1–AE8 parity audit.

## Rollback or migration

- No save format or version migration is introduced; existing console JSON and Unity `SaveEnvelope` version 2 remain compatibility fixtures.
- Roll back by reverting the five implementation units and regenerating `SampleScene.unity` from the prior bootstrap.
- All automated persistence tests use isolated temporary paths; no real save cleanup or repair should be necessary.
- Atomic replacement/last-known-good preservation remains a separate high-priority follow-up rather than an implicit scope expansion.
