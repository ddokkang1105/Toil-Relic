# QA

## Automated checks

| Command | Result | Evidence |
|---|---|---|
| `Unity.exe -batchmode -nographics -quit -projectPath C:\Toil-Relic-main\unity -executeMethod ToilRelic.Unity.Editor.ToilRelicSceneBootstrap.ConfigureSampleScene -logFile C:\Toil-Relic-main\.flow\tasks\camp-hud-completion\qa-bootstrap.log` | Passed | Bootstrap completed with exit code 0 and regenerated `SampleScene`. |
| `Unity.exe -batchmode -nographics -projectPath C:\Toil-Relic-main\unity -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testResults C:\Toil-Relic-main\.flow\tasks\camp-hud-completion\qa-playmode-results.xml -logFile C:\Toil-Relic-main\.flow\tasks\camp-hud-completion\qa-playmode.log` | Passed | 9 passed, 0 failed; test run exit code 0. |

## Manual scenarios

| Scenario | Expected | Result |
|---|---|---|
| Scene startup | HUD fields receive initial player-state text for HP, level, inventory, and weapon. | Passed by `P0_HudIsWiredAndDisplaysPlayerState`. |
| Player-state event | A player HP change followed by `GameEvents.RaisePlayerChanged` refreshes the HUD. | Passed by `P0_HudRefreshesWhenPlayerStateChanges` (`HP 25/30`). |
| Editor visual readability | Four upper-left HUD rows are legible and do not overlap on the target game view. | Not run: this environment executed Unity in headless batch mode only. |

## Known limits

- Automated QA verifies values, event-driven refresh, and serialized references; it does not produce a visual screenshot or assess final typography/readability in the Unity Editor.
- Unity's batch log contained licensing handshake warnings before the test runner initialized, but the test run completed normally with exit code 0 and no test failures.
