# QA

## Scope

- Date: 2026-07-27
- Branch: `codex/play-mode-qa-expansion`
- Profile: `standard`
- Route: Personal Flow detected gstack, but its browser-oriented QA route does not fit a Unity Editor Play Mode task. The equivalent repository-appropriate Unity batch validation was used.
- Product scope: test-only changes in `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`

## Automated checks

| Command | Result | Evidence |
|---|---|---|
| `& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testCategory PlayModeActionContracts -testResults 'C:\Toil-Relic-main\.flow\tasks\play-mode-qa-확장\qa-targeted-results.xml' -logFile 'C:\Toil-Relic-main\.flow\tasks\play-mode-qa-확장\qa-targeted.log'` | Pass: 7 passed, 0 failed, 0 skipped | `qa-targeted-results.xml`, `qa-targeted.log` |
| `& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testResults 'C:\Toil-Relic-main\.flow\tasks\play-mode-qa-확장\qa-full-results.xml' -logFile 'C:\Toil-Relic-main\.flow\tasks\play-mode-qa-확장\qa-full.log'` | Pass: 42 passed, 0 failed, 1 expected opt-in capture skip | `qa-full-results.xml`, `qa-full.log` |
| `dotnet build src\ToilRelic\ToilRelic.csproj --nologo` | Pass: 0 warnings, 0 errors | Console output captured during QA |
| `git diff --check` | Pass | No whitespace errors |
| `git diff --name-only e781cdf03ee2437759b310d037476794b23aa33e --` | Pass: one tracked test file only | `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`; no production scripts, scene, bootstrap, or asmdef changes |

## Stability and isolation

- Three independent final targeted artifacts were re-read and each reports 7 passed, 0 failed, 0 skipped:
  - `work-rework-final-targeted-pass1-results.xml` at `2026-07-24 04:42:18Z`
  - `work-rework-final-targeted-pass2-results.xml` at `2026-07-24 04:42:56Z`
  - `work-rework-final-targeted-pass3-results.xml` at `2026-07-24 04:43:26Z`
- The three artifacts have distinct SHA-256 prefixes (`0F625A2BEC03`, `A2583A2A201A`, `9DDAD77AD1B6`), confirming separate result files.
- Fresh targeted and full logs contained no test-run failure, assertion exception, unhandled exception, or C# compiler-error signal.
- Every save assertion uses the fixture-only temporary `savePathOverride`; teardown restores the prior override, deletes the fixture directory, and destroys created fixture objects.
- Reflected event subscriptions and Unity random state use disposable scopes. Their restoration remains exercised indirectly by the clean repeated and full-suite runs.

## Manual scenarios

| Scenario | Expected | Result |
|---|---|---|
| Click visible New Game action after loading a valid save with non-default HP/level/EXP, all four nonzero inventory categories, and an equipped reward weapon | Enter Camp, show new-expedition feedback, delete the fixture save, reset inventory, and equip the starter weapon | Pass via `P0_NewGameButtonReplacesValidFixtureSave` |
| Click visible Craft action with exact required materials | Consume exact Junk/Relic Part cost, grant Treasure, show save feedback, and persist the result | Pass via `P0_CraftButtonConsumesExactCostAndPersistsReward` |
| Click visible Craft action with insufficient materials | Preserve inventory/reward totals, show requirement feedback, and persist the unchanged state | Pass via `P0_CraftButtonInsufficientMaterialsPersistsUnchangedState` |
| Click visible Potion action while damaged with one potion | Consume potion, heal, run the fixed enemy response, keep the encounter active, and return control | Pass via `P0_PotionButtonConsumesPotionAndCompletesEnemyResponse` |
| Click visible Potion action at full HP | Reject the action, preserve HP and potion, show guard feedback, avoid enemy response/save, and keep control | Pass via `P0_PotionButtonAtFullHpPreservesPotionAndControl` |
| Click visible Attack action against a durable deterministic enemy | Damage the enemy, receive the fixed enemy response, publish no terminal outcome, and return control | Pass via `P0_AttackButtonKeepsDurableEnemyAndReturnsControl` |
| Click visible Flee action with a deterministic failing seed | Show flee failure before enemy response, keep the encounter active, receive fixed damage, and return control | Pass via `P0_FleeButtonFailureKeepsEncounterAndReturnsControl` |

The scenarios were exercised through the serialized visible Unity buttons and scene `EventSystem` in Play Mode automation. No separate hands-on screenshot session was required because the implementation changes tests only and does not alter product UI appearance.

## Known limits

- Synthetic pointer dispatch verifies the serialized Button and persistent listener path, but does not prove `GraphicRaycaster` reachability or top-hit eligibility.
- Event unsubscription and Unity random-state restoration have no direct post-disposal assertion; repeated isolated runs provide indirect evidence.
- New Game verifies the equipped reward weapon before replacement and the starter weapon afterward, but does not separately assert that the owned-equipment collection contains only the starter weapon.
- `P0_CaptureLayoutEvidenceWhenRequested` was intentionally skipped because `TOIL_RELIC_LAYOUT_EVIDENCE_DIR` was unset. This is expected for the headless test-only diff and is not a product failure.
- The first targeted shell wrapper returned before the GUI-subsystem Unity process exited and briefly surfaced a stale nonzero status. Unity continued to completion; its own log records exit code 0 and the generated XML records 7/7 passing.

## Result

`pass`

All acceptance-critical action contracts, repeated-run stability, full-suite isolation, and console compilation passed. The task is ready for the Personal Flow `close` stage.
