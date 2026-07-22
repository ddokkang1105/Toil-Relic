# QA

## Automated checks

| Command | Result | Evidence |
|---|---|---|
| `Unity.exe -batchmode -nographics -quit -projectPath C:\Toil-Relic-main\unity -executeMethod ToilRelic.Unity.Editor.ToilRelicSceneBootstrap.ConfigureSampleScene` | Passed | `qa-bootstrap.log` ends with `Exiting batchmode successfully now!` and return code 0. |
| `Unity.exe -batchmode -nographics -projectPath C:\Toil-Relic-main\unity -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests` | Passed | `qa-playmode-results.xml`: 16 total, 16 passed, 0 failed. |

## Manual scenarios

| Scenario | Expected | Result |
|---|---|---|
| Title -> Camp -> Battle | Battle panel presents an enemy/player-action state and enables only valid action buttons. | Passed by the P0 battle UI contract. |
| Victory | Returns to Camp, retains `Win.` plus level-up text when applicable, and disables battle actions. | Passed by `P0_VictoryReturnsToCampWithVisibleOutcome`. |
| Defeat and successful flee | Return to Camp, retain their outcome, and disable battle actions. | Passed by `P0_DefeatReturnsToCampWithVisibleOutcome` and `P0_SuccessfulFleeReturnsToCampWithVisibleOutcome`. |
| Save failure after victory, Rest, or Equip | Keeps the relevant outcome/error visible after the real save attempt fails. | Passed by `P0_SaveFailurePreservesTerminalOutcome`, `P0_RestSaveFailureRemainsVisible`, and `P0_EquipSaveFailureRemainsVisible`. |

## Known limits

- Browser-oriented `gstack qa` is not applicable to this Unity project; Unity bootstrap and Play Mode validation were used as the repository-appropriate equivalent.
- No interactive Unity Editor screenshot was captured in this batch-mode QA run. The generated layout remains covered by bootstrap serialization and UI Play Mode contracts.
