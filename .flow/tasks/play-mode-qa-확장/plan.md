# Plan

## Readiness

Executable. The canonical CE plan is `docs/plans/2026-07-24-001-test-play-mode-qa-expansion-plan.md` with `artifact_readiness: implementation-ready`.

- Profile: Standard
- Product Contract preservation: current-runtime clarifications plus the confirmed equipment UI deferral
- Confidence check: passed; local grounding, sequencing, risks, scenarios, and verification are implementation-ready
- Headless document review: two fixes applied; one non-blocking proposed fix remains for optional user adjudication

## Steps

1. [x] U1 — Extend the existing Play Mode fixture with narrow visible-button dispatch, event observation, deterministic enemy, random-state, and category helpers.
2. [x] U2 — Add New Game and Craft success/failure UI contracts with action-specific fixture-save assertions.
3. [x] U3 — Add Potion success and full-HP guard UI contracts.
4. [x] U4 — Add nonlethal Attack and deterministic failed Flee UI contracts.
5. [x] Run the targeted category in three independent Unity processes with unique artifacts.
6. [x] Run the full Play Mode assembly once and record isolation and optional-screenshot results.

## Affected paths

- Required: `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`
- Must remain unchanged unless a blocker is surfaced:
  - `unity/Assets/Tests/PlayMode/ToilRelic.PlayModeTests.asmdef`
  - `unity/Assets/Editor/ToilRelicSceneBootstrap.cs`
  - `unity/Assets/Scenes/SampleScene.unity`
  - `unity/Assets/Scripts/`

## Validation

- `dotnet build src/ToilRelic/ToilRelic.csproj`
- Three separate `-batchmode -nographics` Play Mode runs for NUnit category `PlayModeActionContracts`, each with unique XML and log files
- One unfiltered `ToilRelic.PlayModeTests` assembly run
- Real-save isolation, event/random cleanup, and action-specific persistence audit
- Graphics-enabled capture, pixel validation, and direct visual inspection only if product UI appearance changes

## Rollback or migration

No migration is required. Revert the new Play Mode contract methods and their narrow private helpers together; no production, scene, save-format, or assembly-reference rollback should be needed.

## Non-blocking document review item

- Consider requiring the shared pointer helper to raycast an interior screen-space point and verify the target button is the top eligible hit before dispatch. This increases hit-testing fidelity but is not required for the current implementation-ready state.

## Work evidence (2026-07-24)

- U1-U4 implemented only in `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`.
- Added seven `PlayModeActionContracts` tests covering New Game, Craft success/failure, Potion success/full-HP guard, nonlethal Attack, and failed Flee through serialized buttons and the scene `EventSystem`.
- Targeted final-state runs: `work-simplify-results.xml`, `work-targeted-pass2-results.xml`, and `work-targeted-pass3-results.xml`; each reports 7 passed, 0 failed, 0 skipped.
- Full assembly: `work-full-results.xml` reports 42 passed, 0 failed, and 1 intentionally skipped opt-in layout capture.
- Console baseline: `dotnet build src/ToilRelic/ToilRelic.csproj` passed with 0 warnings and 0 errors.
- Isolation audit: production scripts, scene, bootstrap, asmdef, and ProjectSettings were unchanged; every save effect used `fixtureSavePath`, reflected event subscriptions and random state use disposable restoration scopes, and test-created `EnemyData` objects are destroyed in fixture teardown.
- Visual evidence was not required because the diff is test-only and changes no product UI appearance.

## Review rework evidence (2026-07-24)

- Resolved review P1 for R4 / F1 / AE1 / U2 by seeding the New Game fixture with nonzero Junk, Relic Part, Treasure, and Healing Potion plus an equipped reward weapon.
- Added pre-click assertions after scene reload so the fixture must retain every non-default inventory value and the reward weapon before New Game replaces it; the existing post-click zero-inventory and starter-weapon assertions remain the outcome proof.
- Characterization before rework: `work-rework-baseline-results.xml` passed the prior New Game contract at 1/1.
- Final focused proof: `work-rework-final-focused-results.xml` passed the strengthened New Game contract at 1/1.
- Final-state repetition: `work-rework-final-targeted-pass1-results.xml`, `work-rework-final-targeted-pass2-results.xml`, and `work-rework-final-targeted-pass3-results.xml` each report 7 passed, 0 failed, 0 skipped.
- Final-state full assembly: `work-rework-final-full-results.xml` reports 42 passed, 0 failed, and 1 expected opt-in layout-capture skip.
- Console baseline: `dotnet build src/ToilRelic/ToilRelic.csproj --nologo` passed with 0 warnings and 0 errors.
- Simplification pass: reuse 0 findings, quality 1 applied inventory-roster source-of-truth improvement, quality 1 skipped to preserve stage-specific assertion diagnostics, efficiency 0 findings; the complete final verification set passed afterward.
- Scope remained test-only in `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`; production UI appearance did not change, so screenshot evidence remained unnecessary.
