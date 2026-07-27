---
title: Deterministic Unity Play Mode action contracts through serialized UI
date: 2026-07-27
category: best-practices
module: Unity SampleScene Play Mode action contracts
problem_type: best_practice
component: testing_framework
severity: medium
applies_when:
  - "A Unity Play Mode contract must exercise a real serialized visible Button and the scene EventSystem instead of invoking the gameplay action directly"
  - "Action setup needs deterministic reflected player or enemy state while the action boundary must remain production-realistic"
  - "Tests interact with saves, static events, Unity random state, or temporary ScriptableObjects that must not leak between scenarios"
  - "A targeted action pack needs repeat-process stability evidence plus a full Play Mode assembly isolation check"
related_components:
  - "development_workflow"
tags:
  - "unity"
  - "playmode"
  - "ui-action-contracts"
  - "eventsystem"
  - "deterministic-tests"
  - "test-isolation"
  - "save-isolation"
  - "nunit"
---

# Deterministic Unity Play Mode action contracts through serialized UI

## Context

Listener-binding checks alone do not prove that a visible action crosses the serialized UI path and produces the correct state, feedback, resource, and persistence effects. Existing Attack and Flee coverage also emphasized terminal attacks and successful escape rather than ongoing combat branches (`docs/plans/2026-07-24-001-test-play-mode-qa-expansion-plan.md:37`, `docs/plans/2026-07-24-001-test-play-mode-qa-expansion-plan.md:52`).

Use an isolated action contract:

> Prepare one deliberately diagnostic state, dispatch one real serialized visible action through the scene `EventSystem`, and assert that action's state transition, ordered feedback, resource delta, persistence boundary, and return of control.

The Play Mode QA expansion applies this pattern to seven contracts: New Game; Craft success and failure; Potion success and full-HP guard; nonlethal Attack; and failed Flee. The implementation remains confined to the Play Mode test fixture; QA records no product script, scene, bootstrap, or test-assembly-definition changes (`.flow/tasks/play-mode-qa-확장/qa.md:9`, `.flow/tasks/play-mode-qa-확장/qa.md:19`).

## Guidance

Treat listener metadata and action dispatch as separate proofs:

1. Find the named scene `Button`.
2. Require it to be active and interactable.
3. Verify its persistent target is `GameActionBridge` and its method is the action under test.
4. Require the active scene `EventSystem`.
5. Send a left-button `PointerEventData` to the Button's `pointerClickHandler`.

The shared helper performs those checks before calling `ExecuteEvents.Execute` (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:1542`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:1560`). This reaches the serialized `Button -> GameActionBridge -> GameManager` chain; the bridge methods are thin delegates (`unity/Assets/Scripts/UI/GameActionBridge.cs:10`). Reflection or direct runtime calls may prepare fixture state, but should not trigger the player action being proved.

This dispatch is intentionally narrower than end-to-end hit testing. It targets the selected object directly and therefore does not prove `GraphicRaycaster` reachability, top-hit eligibility, or absence of an intercepting overlay (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:1568`, `.flow/tasks/play-mode-qa-확장/qa.md:48`). Add a raycast or top-hit assertion when spatial reachability is part of the risk.

Make preconditions hostile to the regression. A weak reset test can pass vacuously:

```text
default inventory/equipment -> click New Game -> assert defaults
```

Use this shape instead:

```text
seed non-default values -> save -> reload -> prove they survived
-> click New Game -> prove every selected dimension was replaced
```

The strengthened New Game contract seeds all four inventory categories plus a reward weapon, saves and reloads them, and proves the values survived before clicking (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:443`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:468`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:482`). It then asserts zero inventory and the starter weapon after replacement (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:501`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:533`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:539`). Review found that the earlier default-valued fixture could hide selective-reset regressions (`.flow/tasks/play-mode-qa-확장/review.md:29`).

Isolate each mutable boundary:

- Install a unique temporary `SaveService.savePathOverride` before scene loading because `GameManager.Awake` loads immediately (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:94`, `unity/Assets/Scripts/Core/GameManager.cs:32`). Teardown restores the previous override, deletes the fixture directory, and destroys generated fixture objects (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:1506`).
- Record reflected static `Action<string>` events in an `IDisposable` recorder whose disposal removes exactly its handler (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:37`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:59`).
- Capture and restore `UnityEngine.Random.state` in a disposable scope (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:71`). For failed Flee, find a seed whose first roll fails inside a nested scope, then reset to it immediately before the visible click (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:1608`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:823`).
- Create a high-HP enemy with fixed one-damage attacks, register its generated `EnemyData`, and destroy it during teardown (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:1630`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:1714`). High HP keeps Attack nonterminal; fixed damage makes the response exact.

For every click, assert the applicable semantic tuple:

- exact resource changes on success;
- complete preservation on guards and failures;
- exact feedback plus event count and order;
- same-enemy continuity and absence of terminal `BattleOutcome`;
- return to `Battle` and `PlayerAction` after an enemy response; and
- the action-specific persistence boundary.

Do not impose a uniform save rule. New Game deletes the previous save without immediately replacing it (`unity/Assets/Scripts/Core/GameManager.cs:69`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:516`). Craft calls `SaveProgress` after either result, so success and failure both persist their resulting state (`unity/Assets/Scripts/Core/GameManager.cs:209`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:587`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:642`). Nonterminal Potion, Attack, and failed Flee do not save, so their fixture file must remain absent.

Finally, run the category in separate Unity processes with unique artifacts, then run the full Play Mode assembly once. Separate processes test reproducibility from fresh global state; the full assembly tests coexistence with the rest of the fixture.

## Why This Matters

Each weaker oracle permits a different false positive:

- Listener inspection can pass while an action is inactive, non-interactable, or never dispatched.
- Default reset fixtures can pass when replacement logic does nothing.
- A final status label can hide missing or reversed earlier messages.
- Terminal-only combat tests can miss failure to return control after a nonlethal response.
- In-memory assertions can pass while an action writes the wrong state, writes when it should not, or touches the real save.
- Deterministic assertions can still contaminate later tests if event handlers, random state, generated objects, or save overrides leak.

The action-contract shape closes these gaps without changing product code. It also localizes failures: "Craft failure persisted a Treasure," "Potion guard triggered an enemy response," and "Attack did not return to PlayerAction" identify different runtime contracts instead of collapsing into one long end-to-end failure.

The task evidence includes a fresh targeted 7/7 run and a full assembly run with 42 passes, zero failures, and one expected opt-in capture skip (`.flow/tasks/play-mode-qa-확장/qa.md:15`, `.flow/tasks/play-mode-qa-확장/qa.md:16`). Three independently written final targeted artifacts also report 7/7 with distinct hashes (`.flow/tasks/play-mode-qa-확장/qa.md:23`, `.flow/tasks/play-mode-qa-확장/qa.md:27`). These are branch and task verification results, not a claim that the change has been merged or shipped.

## When to Apply

Apply this pattern when a serialized Unity action:

- changes several observables such as state, inventory, messages, phase, and persistence;
- has materially different success, guard, or failure branches;
- depends on random gameplay but needs a deterministic nonterminal scenario;
- publishes ordered static events or must prove the absence of a terminal event; or
- loads, deletes, writes, or deliberately does not write persistent state.

Only use a real product entry point as the action boundary. If none exists, defer the contract rather than fabricating a test-only Button or bypassing the UI-path requirement; equipment coverage was deferred for that reason (`docs/plans/2026-07-24-001-test-play-mode-qa-expansion-plan.md:46`, `docs/plans/2026-07-24-001-test-play-mode-qa-expansion-plan.md:69`).

Known limits:

- Direct target dispatch is not `GraphicRaycaster` or top-hit proof.
- Event unsubscription and random restoration have no direct post-disposal assertion; repeated and full-suite runs provide indirect evidence (`.flow/tasks/play-mode-qa-확장/review.md:43`).
- New Game proves reward-weapon equipment before replacement and starter-weapon equipment afterward, but does not separately prove that the owned-equipment collection is starter-only (`.flow/tasks/play-mode-qa-확장/qa.md:50`).
- This is behavioral automation, not visual evidence. The full-suite capture skip was expected because evidence capture was not requested (`.flow/tasks/play-mode-qa-확장/qa.md:51`).
- Reflection preserves the test assembly's dependency boundary but couples helpers to private names; keep helpers narrow and diagnostics action-specific.

## Examples

| Contract | Diagnostic precondition | Required semantic outcome | Fixture-save boundary |
|---|---|---|---|
| New Game | Reload a valid save with non-default stats, all inventory categories nonzero, and a reward weapon equipped | Enter Camp, show start feedback, reset selected player dimensions, equip starter weapon | Delete prior file; do not recreate it |
| Craft success | Exact five Junk and one Relic Part | Consume exact cost, grant one Treasure, show success and save feedback | Persist changed counts |
| Craft failure | Four Junk and no Relic Part | Preserve counts, show unmet-requirement and save feedback | Persist unchanged counts |
| Potion success | Damaged player, one potion, durable fixed-damage enemy | Consume potion; recovery precedes enemy hit; HP reflects heal then damage; return control | No write |
| Potion guard | Full-HP player with one potion | Preserve HP and potion; publish only guard reason; no enemy response; retain control | No write |
| Nonlethal Attack | Durable fixed-damage enemy | Damage enemy; player hit precedes enemy hit; no outcome; same encounter; return control | No write |
| Failed Flee | Seed with a failing first roll and fixed-damage enemy | Failure precedes enemy hit; no success or outcome; same encounter; return control | No write |

Potion success expects two ordered events, consumption, fixed enemy damage, and returned control (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:681`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:688`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:701`). The full-HP guard instead expects one event, no consumption, no damage, and no save (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:734`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:738`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:745`).

Attack and failed Flee observe both `BattleLog` and `BattleOutcome`: ordered logs prove the nonterminal response sequence, while an empty outcome recorder proves the encounter did not terminate (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:793`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:799`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:833`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:841`).

## Related

- The implementation rationale and action/persistence matrix are in `docs/plans/2026-07-24-001-test-play-mode-qa-expansion-plan.md:177` and `docs/plans/2026-07-24-001-test-play-mode-qa-expansion-plan.md:206`.
- [Unity Play Mode HUD contract testing without a runtime assembly reference](unity-playmode-hud-contracts.md)
- [Preserve Unity terminal outcomes through level-ups and save failures](../ui-bugs/unity-status-event-save-failure-contracts.md)
- [Validate Unity Play Mode screenshot pixels before accepting visual evidence](unity-playmode-screenshot-evidence-validity.md)
