# Decisions

## Confirmed

- The task uses the `standard` profile because it spans visible UI and the game-state transition flow.
- The implemented runtime flow is `Title → Camp → Battle → Camp`; `Hunt` and `Result` are declared in `GameState` but are not entered by `GameManager`.
- Battle actions are guarded by `GameManager.CanTakePlayerAction()`, which requires `Battle`, a live enemy, and `BattlePhase.PlayerAction`.
- The current panels only toggle for Title, Camp, and Battle. They do not render the active state or battle phase, and their buttons do not reflect action availability.
- `BattlePanelController` is TMP-based but the bootstrap creates no TMP fields and does not add the controller to `BattlePanel`; it is therefore not part of the active scene UI.
- The HUD task established a uGUI `Text` fallback because TMP Essential Resources are unavailable in this project. The battle UI should follow that dependency-safe approach unless TMP is intentionally configured first.
- Keep the existing immediate Camp return after victory, defeat, and successful flee. Surface the terminal result in the Camp UI instead of adding an acknowledgement state.
- Use a compact, text-first battle presentation: active-state title, enemy HP, phase prompt, action availability, and retained outcome message.

## Rejected options

- Do not make the unused `Hunt` and `Result` enum values the primary presentation model without first defining their gameplay lifecycle; the current state machine resolves flee, defeat, and victory directly to Camp.
- Do not add a separate result/acknowledgement screen in this task; it adds an extra transition without changing the established gameplay loop.

## Open questions

- None that block planning.

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
| Framework probe | Completed | `personal-flow/scripts/probe-frameworks.ps1`; OpenSpec CLI is installed but its `opsx` skill is not active in this session. |
| Discover | Completed | Direct source/scene inspection; no active OpenSpec skill was available. |
| Design review | fallback_direct | The active review requires Plan Mode/interactive review tooling unavailable in this session. Direct review chose upper-right persistent status, centered battle information/actions, and no overlap with the upper-left HUD. |
| Brainstorm | Completed | CE brainstorm guidance applied directly; no blocking-question tool is available in this session. Product contract: `docs/plans/2026-07-16-001-feat-battle-ui-state-transitions-plan.md`. |
| CE plan | fallback_direct | The active CE plan workflow requires an interactive handoff unavailable in this session. The implementation-ready plan was written directly to the product contract and task `plan.md`. |
| Work | fallback_direct | Added uGUI status/battle presenters, scene bootstrap wiring, Potion action, and Play Mode contracts. Bootstrap succeeded; Play Mode: 10 passed, 0 failed. |
| Review | CE code review | Correctness and maintainability lenses found no defect; testing lens found P2 terminal-transition coverage gap. Reopened work before QA. |
| P2 rework | fallback_direct | Added real GameManager-driven victory, defeat, and successful-flee Play Mode scenarios. Status now retains the win line when a subsequent level-up line is emitted. Play Mode: 13 passed, 0 failed. |
| Review recheck | CE code review | The first P2 was resolved, but re-review found four further P2 gaps: defeat does not use enemy-turn damage, win-plus-level-up retention is not deterministic, flee alters global random state, and retention is coupled to display strings. Reopened work before QA. |
| Re-review P2 rework | fallback_direct | Added typed `BattleOutcome` and `LevelUp` events for the status UI; victory now fixes level/EXP at the threshold, defeat uses a fixed-damage runtime enemy and `Defend -> ResolveEnemyTurn`, and flee restores `UnityEngine.Random.state`. Final Play Mode: 13 passed, 0 failed in `work-p2-resolved-results.xml`. |
| Second review recheck | CE code review | Real terminal paths and random-state isolation passed review, but two P2 gaps remain: the presenter suppresses a duplicate log through an implicit event-order contract, and a save-failure log can overwrite a terminal outcome. Reopened work before QA. |
| Second review P2 rework | fallback_direct | Removed the duplicate level-up `BattleLog`; added `SaveFailed` and let the status presenter append it to a typed terminal outcome. Normal `BattleLog` events clear the outcome-retention flag, so later Camp actions replace stale status text. Play Mode: 14 passed, 0 failed in `work-p2-final-results.xml`. |
| Save-failure integration rework | fallback_direct | Added a private `SaveService` test override for the save path. The P0 scenario redirects one victory save to a nonexistent directory, restores the override in `finally`, and verifies the real GameManager path retains the outcome with the save error. Play Mode: 14 passed, 0 failed in `work-save-integration-final.xml`. |
| Save-failure review recheck | CE code review | Victory save failure is covered, but Rest/Equip overwrite their save failure with a later normal log, and the path override affects reads/deletes as well as writes. Reopened work before QA. |
| Save-failure ordering rework | fallback_direct | Rest and Equip now emit their normal status log before `SaveProgress()`, leaving `SaveFailed` visible on failure. The test seam was renamed `saveWritePathOverride` and moved into `TrySave()` only. Victory, Rest, and Equip failure scenarios all pass; Play Mode: 16 passed, 0 failed in `work-save-order-final.xml`. |
| Final review recheck | CE code review | Correctness, testing, and maintainability lenses found no P0-P2 issues. The terminal outcome/event contract and `TrySave`-only failure seam are ready for QA; Play Mode: 16 passed, 0 failed. |
| QA | fallback_direct | Browser-only gstack QA is not applicable to Unity. Regenerated `SampleScene` in Unity batch mode, then ran `ToilRelic.PlayModeTests`: 16 passed, 0 failed. QA record: `qa.md`. |
| Close | lightweight direct compound | Documented the durable typed-status-event and actual-save-failure test pattern. Frontmatter and claim validation passed. Follow-up: optional interactive Unity Editor layout screenshot. |
