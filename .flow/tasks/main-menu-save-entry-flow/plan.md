# Plan: Main Menu Save Entry Flow

## Current-state review

The current Unity code already contains a title-state implementation, SaveService deletion support, a title panel, and persistent Continue/New Game/Quit bindings. The remaining gap is behavioral test coverage for the entry-state contract.

## Implementation units

1. Add Play Mode coverage that verifies the scene opens in `Title`, the title panel is active, and Camp/Battle panels are inactive.
2. Add Play Mode coverage that verifies the Continue button’s enabled state matches `GameManager.HasSavedGame` without deleting persistent data.
3. Run the full `ToilRelic.PlayModeTests` suite and review only the task-owned paths for regressions.

## Affected paths

- `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`
- Existing reviewed implementation: `unity/Assets/Scripts/Core/GameManager.cs`, `unity/Assets/Scripts/Save/SaveService.cs`, `unity/Assets/Scripts/UI/TitleMenuController.cs`, `unity/Assets/Scripts/UI/StatePanelController.cs`, `unity/Assets/Scripts/UI/GameActionBridge.cs`, `unity/Assets/Editor/ToilRelicSceneBootstrap.cs`, `unity/Assets/Scenes/SampleScene.unity`

## Verification

Run Unity 6000.3.19f1 Test Framework in Play Mode with `ToilRelic.PlayModeTests`; inspect XML for title-menu and pre-existing P0 checks.

## Rollback

Re-run `ToilRelicSceneBootstrap.ConfigureSampleScene` after source rollback to regenerate the scene bindings from the reverted scripts.
