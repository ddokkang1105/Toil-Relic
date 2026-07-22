# Plan

## Readiness

## Steps

1. [ ]

## Affected paths

## Validation

## Rollback or migration
# Plan: Camp HUD Completion

## Goal

Provide a persistent, readable player-status HUD during the Unity title/camp/battle flow without changing gameplay rules.

## Design review outcome

- Place the HUD at the Canvas upper-left so it does not collide with the centered title/camp/battle panels.
- Use the existing TMP-based `HudController` and its four focused text fields rather than introduce a new state model.
- Keep the HUD visible across title, camp, and battle; the information remains useful when a player transitions between panels.

## Implementation units

1. **Create HUD UI and serialization wiring**
   - Modify `unity/Assets/Editor/ToilRelicSceneBootstrap.cs`.
- Create a `Hud` root with four uGUI `Text` labels: HP, level, inventory, and equipment.
   - Add `HudController` and assign all four private text fields through `SerializedObject`.
   - Use the built-in uGUI font and stable upper-left anchors.

2. **Make HUD initial state reliable**
   - Modify `unity/Assets/Scripts/UI/HudController.cs` only if required after runtime verification.
   - Ensure the first `GameEvents.PlayerChanged` publication initializes each label, and subsequent player changes retain the same display contract.

3. **Extend Play Mode scene-contract coverage**
   - Modify `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`.
   - Verify a `HudController` exists, its four serialized references are non-null, and the initial labels contain the expected player-state information.

4. **Test assembly dependency**
   - No additional assembly reference is required after the uGUI Text fallback.

## Verification

- Re-run `ToilRelicSceneBootstrap.ConfigureSampleScene` to regenerate `SampleScene` from the source configuration.
- Run Unity 6000.3.19f1 Play Mode tests for `ToilRelic.PlayModeTests`.
- Inspect test XML and Unity log for compilation errors and unexpected runtime exceptions.

## Rollback

- Revert the HUD bootstrap/controller/test changes and rerun the bootstrap method to restore the prior scene layout.
