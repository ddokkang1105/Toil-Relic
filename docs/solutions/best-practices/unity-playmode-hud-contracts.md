---
title: Unity Play Mode HUD contract testing without a runtime assembly reference
date: 2026-07-16
category: best-practices
module: Unity HUD and Play Mode tests
problem_type: best_practice
component: testing_framework
severity: low
applies_when:
  - "A Unity Play Mode test assembly intentionally has no runtime assembly references"
  - "A generated Unity HUD must be verified in batch mode"
tags: [unity, playmode, hud, ugui, scene-contract]
---

# Unity Play Mode HUD contract testing without a runtime assembly reference

## Context

The camp HUD needed four generated labels and event-driven player-state updates. The project test assembly deliberately has an empty `references` list, so directly importing the runtime `ToilRelic.Unity.Core` namespace from a test caused `CS0234`.

## Guidance

Use uGUI `Text` when the project does not provide TMP Essentials, wire the generated fields through the scene bootstrap, and test the public scene contract in Play Mode.

Keep test code assembly-neutral when the test asmdef has no runtime reference. Find the `GameManager` and `HudController` by type name, retrieve the current player, then invoke the runtime event through reflection. This validates that the HUD receives the event without changing the test assembly dependency graph.

```csharp
var player = GetPrivateField(gameManager, "player");
player.GetType().GetMethod("TakeDamage").Invoke(player, new object[] { 5 });
gameEventsType.GetMethod("RaisePlayerChanged").Invoke(null, new[] { player });
Assert.That(hpText.text, Is.EqualTo("HP 25/30"));
```

## Why This Matters

The bootstrap is the source of truth for the generated scene, while Play Mode verifies the serialized field wiring and runtime display contract together. Testing the `GameEvents.PlayerChanged` path catches a HUD that is present at startup but fails to refresh after gameplay changes.

## When to Apply

- Adding a scene-generated HUD to `SampleScene`.
- Verifying Unity UI in headless CI or batch mode.
- Extending `ToilRelic.PlayModeTests` without adding a reference to the runtime assembly.

## Examples

`HudController` subscribes to `GameEvents.PlayerChanged` and assigns HP, level, inventory, and weapon labels. `ToilRelicSceneBootstrap` creates the four upper-left uGUI labels and serializes them into that controller. The two HUD Play Mode tests verify initial values and an HP refresh after the event is raised.

## Related

- `unity/Assets/Scripts/UI/HudController.cs`
- `unity/Assets/Editor/ToilRelicSceneBootstrap.cs`
- `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`
