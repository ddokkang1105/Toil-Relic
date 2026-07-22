# Review

| Severity | Finding | Disposition | Verification |
|---|---|---|---|
| P2 | The original terminal-transition test raised `BattleLog` and called private state methods directly, so it did not prove real victory, defeat, or successful-flee paths. | Resolved | The replacement scenarios invoke `GameManager.Attack()` or `Flee()` and assert Camp state, a retained outcome, and disabled battle controls. `work-terminal-results.xml`: 13 passed, 0 failed. |
| P2 | The defeat scenario reduces the player to zero HP by reflection, then calls `Attack()`. It does not prove the live enemy-turn damage path reaches Camp. | Rework required | Configure a lethal enemy attack and drive a normal player action so `ResolveEnemyTurn` produces defeat. |
| P2 | The victory scenario only asserts that the final message contains `Win.`; it does not deterministically exercise or verify the `Win. -> Level up!` retention contract. | Rework required | Set player EXP to one point below the level threshold, defeat the enemy, and assert both lines in their emitted order. |
| P2 | Successful flee probes up to 20 global Unity random seeds and does not restore `Random.state`; it can affect later tests and is coupled to PRNG behavior. | Rework required | Isolate and restore the random state at minimum; prefer a deterministic flee-result seam if it is feasible within task scope. |
| P2 | `GameStatusController` identifies the win/level-up pair by display strings, so localization or log wording changes can silently break outcome retention. | Rework required | Replace string-coupled pairing with an explicit presentation event/type or a narrow stateful contract, and cover it with the deterministic victory test. |
| P2 | The display-string pairing issue was replaced by typed `BattleOutcome` and `LevelUp` events. | Resolved | `work-p2-resolved-results.xml`: 13 passed, 0 failed; victory verifies `Win.` followed by `Level up!`, defeat uses `ResolveEnemyTurn`, and flee restores Unity random state. |
| P2 | `GameStatusController.suppressNextBattleLog` assumes every `LevelUp` event is immediately followed by its duplicate `BattleLog`. A future event ordering change can suppress an unrelated normal log. | Rework required | Do not publish the level-up content through both presentation channels, or add an explicit correlated event contract that requires no presenter-side suppression state. |
| P2 | A post-terminal `SaveProgress()` failure publishes a normal `BattleLog` and overwrites the visible victory/defeat outcome. | Rework required | Preserve the terminal outcome independently of routine log updates, then add a regression scenario for the save-failure path. |
| P2 | The first save-failure regression invoked UI events directly, so it did not prove the actual `GameManager` terminal save path. | Resolved | The Play Mode test redirects the save path to a nonexistent directory, drives public `Attack()` through victory, and verifies Camp state, disabled attack, retained `Win.`, and appended `Save failed:`. `work-save-integration-final.xml`: 14 passed, 0 failed. |
| P2 | `Rest()` and `Equip()` call `SaveProgress()` before publishing their normal `BattleLog`, so a save failure is immediately overwritten in the status area. | Rework required | Define one shared save-result ordering/retention contract and cover non-terminal save failures, including Rest and Equip. |
| P2 | The `savePathOverride` test seam redirects load/delete/existence checks as well as save writes, despite its comment claiming write-only scope. | Rework required | Narrow the override to `TrySave()` failure injection, or document and test its broader scope. |
| P2 | Rest and Equip save failures were immediately overwritten by a later normal log, and the path seam was broader than its stated write-failure purpose. | Resolved | Rest/Equip now publish their normal log before `SaveProgress()`. `saveWritePathOverride` is read only by `TrySave()`. Play Mode verifies victory, Rest, and Equip failure visibility in `work-save-order-final.xml`: 16 passed, 0 failed. |

## Clean areas

- Correctness review found no concrete regression in the presenter lifecycle, event order, or action guard alignment.
- Maintainability review found the responsibilities remain separated: `GameManager` owns transitions, `StatePanelController` owns panel visibility, and presenters render state only.
- Scene serialization contains all battle/status references. The rework Play Mode run passed 13/13, and terminal scenarios now exercise real game actions; the remaining findings concern typed-event ownership and save-failure outcome retention.

## Result

Pass. The final correctness, testing, and maintainability re-review found no new P0-P2 findings. The 16/16 Play Mode result verifies terminal outcomes, level-up retention, actual victory save failure, and Rest/Equip save failure visibility. Ready for QA.
