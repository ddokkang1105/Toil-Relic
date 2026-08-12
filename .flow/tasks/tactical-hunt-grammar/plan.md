# Plan

## Readiness

Implementation-ready. Product behavior is fixed in `docs/plans/2026-08-11-002-feat-tactical-hunt-grammar-plan.md`; no blocking questions remain.

## Steps

1. [x] Add mirrored pure tactical intent rules and a shared cross-runtime contract fixture.
2. [x] Convert console combat to a player-stepped intent/action/response loop with tactical tests.
3. [x] Add Unity intent state, events, presentation, and consuming-action integration.
4. [x] Add targeted Unity contract/action coverage and preserve existing action ordering.
5. [x] Run review, targeted/full validation, record follow-ups and learning, and close.

## Affected paths

- `src/ToilRelic/Systems/CombatSystem.cs`
- `src/ToilRelic/Systems/TacticalCombatRules.cs`
- `tests/ToilRelic.Tests/`
- `unity/Assets/Scripts/Systems/`
- `unity/Assets/Scripts/Core/GameManager.cs`
- `unity/Assets/Scripts/Core/GameEvents.cs`
- `unity/Assets/Scripts/UI/BattlePanelController.cs`
- `unity/Assets/Editor/ToilRelicSceneBootstrap.cs`
- `unity/Assets/Scenes/SampleScene.unity`
- `unity/Assets/Tests/Fixtures/`
- `unity/Assets/Tests/PlayMode/`
- `CONCEPTS.md`
- `docs/plans/2026-08-11-002-feat-tactical-hunt-grammar-plan.md`
- `.flow/tasks/tactical-hunt-grammar/`

## Validation

- Targeted and full console tests plus console build.
- Targeted and full Unity PlayMode tests when the local Unity editor is available.
- Shared fixture parity, `git diff --check`, and scoped diff/status inspection.

## Rollback or migration

No save migration. Reverting the new intent rules, orchestration, events, and tests restores the prior combat loop; no persisted data cleanup is needed.
