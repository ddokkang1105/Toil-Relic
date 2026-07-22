# Plan: 전투 UI·상태 전환 정리

## Readiness

- Product contract: `docs/plans/2026-07-16-001-feat-battle-ui-state-transitions-plan.md`
- Design review outcome: use a persistent upper-right status area, leaving the upper-left HUD and centered action panels unobstructed.
- No gameplay-rule, save-format, or console-game change is required.

## Steps

1. [x] **Add focused uGUI presenters for game status and battle state.**
   - Add a persistent status presenter that listens to state and battle-log events and retains the latest outcome when Camp returns.
   - Convert the unused TMP-only battle presenter to dependency-safe uGUI `Text`, add a phase prompt, and bind battle buttons so their interactability follows the battle phase.
   - Keep panel visibility in `StatePanelController`; presentation controllers must not alter `GameManager` transitions.

2. [x] **Expose and wire the complete battle surface from the scene bootstrap.**
   - Update `ToilRelicSceneBootstrap` to create a readable state/status region at the Canvas upper-right.
   - Add enemy, phase, and log text to `BattlePanel`; reposition the existing buttons and add the existing Potion action.
   - Serialize all controller references, including the `GameManager` and action buttons, through the bootstrap.

3. [x] **Keep action availability synchronized with the state model.**
   - Add a read-only current-state surface if the UI requires it, without creating new `GameState` transitions.
   - Reflect Title/Camp/Battle visibility and `BattlePhase.PlayerAction` in button interactability; preserve `GameManager` guards as the authoritative safety layer.

4. [x] **Extend Play Mode contracts.**
   - Assert the new status/battle controllers and serialized uGUI references exist in `SampleScene`.
   - Cover Title → Camp → Battle entry and verify battle controls enable only during player action.
   - Cover a terminal Battle → Camp event so the outcome remains visible and battle controls are unavailable.

5. [x] **Regenerate and validate the scene.**
   - Run the bootstrap method, then the `ToilRelic.PlayModeTests` assembly in Unity batch Play Mode.
   - Inspect XML/log output and record the result in the task QA artifact.

6. [x] **Resolve re-review P2 terminal-contract gaps.**
   - Emit typed battle-outcome and level-up presentation events so the status UI does not infer behavior from display strings.
   - Make victory level-up, enemy-turn defeat, and successful flee scenarios deterministic enough for Play Mode regression coverage; restore the global random state after the flee probe.
   - Verification: `work-p2-resolved-results.xml` reports 13 passed, 0 failed.

7. [x] **Resolve second re-review P2 event-ownership gaps.**
   - Remove the duplicate level-up `BattleLog` so the status presenter has no next-event suppression contract.
   - Route save failures through a dedicated UI event; append it to a retained terminal outcome and keep regular gameplay logs able to replace stale status text.
   - Verification: `work-p2-final-results.xml` reports 14 passed, 0 failed, including `P0_SaveFailurePreservesTerminalOutcome`.

8. [x] **Exercise the actual terminal save-failure path.**
   - Add a test-only save-path override that redirects one Play Mode save attempt to a nonexistent directory and restores it in `finally`.
   - Drive public `Attack()` through victory, then assert Camp state, disabled battle action, retained win text, and appended save failure text.
   - Verification: `work-save-integration-final.xml` reports 14 passed, 0 failed.

9. [x] **Unify all save-failure presentation and narrow the test seam.**
   - Make `TrySave()` the only operation redirected by the test-only write-path override; load, delete, and existence checks keep their runtime path.
   - Publish Rest and Equip success logs before attempting their save so a save failure remains visible as the last status update.
   - Verification: `work-save-order-final.xml` reports 16 passed, 0 failed, including victory, Rest, and Equip save-failure scenarios.

## Affected paths

- `unity/Assets/Scripts/UI/BattlePanelController.cs`
- `unity/Assets/Scripts/UI/StatePanelController.cs`
- `unity/Assets/Scripts/UI/` (new persistent status presenter)
- `unity/Assets/Scripts/Core/GameManager.cs` (only if a read-only state property is necessary)
- `unity/Assets/Editor/ToilRelicSceneBootstrap.cs`
- `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`
- `docs/plans/2026-07-16-001-feat-battle-ui-state-transitions-plan.md`

## Validation

- Bootstrap command: `Unity.exe -batchmode -nographics -quit -projectPath C:\Toil-Relic-main\unity -executeMethod ToilRelic.Unity.Editor.ToilRelicSceneBootstrap.ConfigureSampleScene`.
- Play Mode command: `Unity.exe -batchmode -nographics -projectPath C:\Toil-Relic-main\unity -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests` (omit `-quit`).
- Manual Unity Editor check: confirm the upper-right status area does not overlap the upper-left HUD or the centered panels.

## Rollback or migration

- Revert the presenter/bootstrap/test changes together and rerun the existing bootstrap method to regenerate the prior scene layout.
- No save migration is required.
