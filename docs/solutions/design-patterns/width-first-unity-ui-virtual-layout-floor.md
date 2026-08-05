---
title: Use a virtual layout floor for width-first Unity uGUI
date: 2026-08-04
category: design-patterns
module: Unity generated UI layout
problem_type: design_pattern
component: testing_framework
severity: medium
applies_when:
  - "A Screen Space uGUI Canvas uses Scale With Screen Size with width-first matching"
  - "Fixed top-corner information regions must remain separate from centered action panels"
  - "Generated scene geometry needs deterministic Play Mode coverage and rendered QA evidence"
related_components:
  - "development_workflow"
tags:
  - "unity"
  - "ugui"
  - "canvas-scaler"
  - "responsive-layout"
  - "virtual-viewport"
  - "playmode"
  - "text-capacity"
  - "visual-qa"
---

# Use a virtual layout floor for width-first Unity uGUI

## Context

`ToilRelicSceneBootstrap` configures the Canvas with an `800x600` reference resolution and `matchWidthOrHeight = 0`, so physical width determines the scale (`unity/Assets/Editor/ToilRelicSceneBootstrap.cs:370`). A `1280x720` viewport therefore exposes only 450 virtual height units:

```text
scale = physicalWidth / referenceWidth = 1280 / 800 = 1.6
virtualHeight = physicalHeight / scale = 720 / 1.6 = 450
```

At a width-first 16:9 floor, the upper-left HUD, upper-right status area, and centered Title/Camp actions compete for the same vertical space despite appearing separate at the 600-unit reference height. The HUD overlap task fixed this without changing the global scaling policy or hiding information. Its durable result is a layout method: treat the narrowest supported virtual viewport as an executable contract, then validate text capacity and rendered output as separate concerns.

## Guidance

### Derive a virtual design floor

For a width-first reference width `R` and a physical viewport `W x H`, derive:

```text
virtualWidth = R
virtualHeight = H / (W / R)
```

This repository uses `800x450` as its 16:9 floor (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:30`). Keep the Canvas policy stable and size important regions against that floor. Changing `matchWidthOrHeight` to make one screen fit would rescale every generated UI surface.

### Assert relationships instead of duplicating constants

For fixed anchors, convert a `RectTransform` into virtual bounds from its anchor, pivot, `anchoredPosition`, and `sizeDelta`. The Play Mode helper implements that calculation and rejects unsupported stretch-anchor assumptions (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:2726`).

Protect the top region with a semantic invariant:

```text
topRegionBottom = min(hudBounds.yMin, statusBounds.yMin)
required: topRegionBottom - activePanelBounds.yMax >= 16
```

The Title and current Camp action panels are checked against this boundary at the `800x450` floor (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:1928`). Their exact heights may differ as action counts evolve; the protected gap is the reusable contract.

Keep related contracts explicit rather than assuming that non-overlap proves usability:

- text stays at least 16px and Best Fit remains disabled;
- Title/Camp action buttons stay at least 44 virtual pixels high with positive visible gaps;
- each named button retains its exact persistent target and method, including Quit; and
- the authoritative bootstrap is changed before regenerating and testing `SampleScene`.

Current geometry constants and fixed upper-corner anchors live in `ToilRelicSceneBootstrap` (`unity/Assets/Editor/ToilRelicSceneBootstrap.cs:22`, `unity/Assets/Editor/ToilRelicSceneBootstrap.cs:515`). Keeping this authority in the generator prevents a scene-only adjustment from disappearing on regeneration.

### Test production-shaped text capacity

Rectangle separation does not prove that wrapped status text fits. Send bounded, representative strings through the real event and presenter path, force a layout update, then compare `Text.preferredHeight` with the allocated rectangle:

```csharp
Canvas.ForceUpdateCanvases();
Assert.That(messageText.preferredHeight,
    Is.LessThanOrEqualTo(messageText.rectTransform.rect.height + 0.01f));
```

The status-capacity contract uses a victory line with loot details, a level-up line, and a save-failure line at the same time (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:2149`). During review, short placeholders had allowed a 60px body to pass; production-shaped strings required 70px, so the body was raised to 72px. This is why representative wrapping input is part of the layout contract, not merely test data (`.flow/tasks/hud-텍스트-중앙-패널-크기-조정으로-메인-메뉴와-상태-메시지-레이아웃-겹침을-해소/review.md:14`).

Compact labels can reduce pressure without removing meaning. `HudController` uses `Lv`, `Part`, and `Wpn` while preserving HP, level progress, inventory counts, weapon identity, ATK, and DEF (`unity/Assets/Scripts/UI/HudController.cs:24`). Do not use automatic font shrinking as a substitute for capacity.

### Use rendered evidence as a complementary oracle

Deterministic geometry and typography tests are the default regression layer. A separate opt-in test renders required states and viewports through a `RenderTexture` when `TOIL_RELIC_LAYOUT_EVIDENCE_DIR` is set; without it, NUnit reports an explicit skip (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:2264`). The capture helper warms the layout, verifies the camera dimensions, writes the PNG, restores render state, and checks that the artifact is non-empty (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:3158`).

Those checks prove that the capture pipeline ran, not that the pixels are correct. Inspect the actual images for state, hierarchy, clipping, and overlap, and reject uniform or wrong-view output. Keep screenshots supplementary because pixel evidence alone cannot identify which geometry invariant regressed.

## Why This Matters

The three evidence layers answer different questions:

1. Virtual bounds prove that supported regions remain separated under the actual Canvas scaling policy.
2. Production-shaped `preferredHeight` checks prove that Unity's font metrics and wrapping fit the allocated space.
3. Rendered captures show visual hierarchy and clipping that numeric assertions cannot fully communicate.

Combining them prevents common false confidence: checking only the reference resolution, testing only short text, accepting an optional capture that produced nothing, or treating a non-empty PNG as visual proof.

The HUD task's final QA recorded 20 passing Play Mode tests with one intentional opt-in capture skip, a dedicated passing capture run, six inspected viewport images, and a clean console build (`.flow/tasks/hud-텍스트-중앙-패널-크기-조정으로-메인-메뉴와-상태-메시지-레이아웃-겹침을-해소/qa.md:9`).

## When to Apply

- Fixed-anchor Screen Space uGUI layouts with a stable Canvas scaling policy.
- Generated scenes where a bootstrap owns serialized layout geometry.
- Bounded status formats that can be represented by realistic worst cases.
- Batch Play Mode suites that need deterministic layout checks plus separately requested visual evidence.

Use adaptive layout groups, safe-area handling, scrolling, truncation policy, or breakpoint-specific contracts instead when localization, user-authored text, accessibility scaling, rotation, or platform error detail is unbounded. Do not apply one panel's dimensions to a structurally different surface: BattlePanel combines combat information with four actions and retains a separate 16:9 redesign follow-up.

## Examples

The original task used the `800x450` floor to compact the HUD/status regions and a three-action central panel while preserving a 16-unit gap. Later equipment and save-status changes gave Camp a distinct four-action geometry and added a separate save row. The current tests still apply the same gap, typography, capacity, and exact-action invariants, which keeps those tests applicable after dimensions change instead of encoding only the task-time constants.

## Related

- [Validate Unity Play Mode screenshot pixels before accepting visual evidence](../best-practices/unity-playmode-screenshot-evidence-validity.md)
- [Unity Play Mode HUD contract testing without a runtime assembly reference](../best-practices/unity-playmode-hud-contracts.md)
- [Deterministic Unity Play Mode action contracts through serialized UI](../best-practices/deterministic-unity-playmode-action-contracts.md)
- [Preserve Unity terminal outcomes through level-ups and save failures](../ui-bugs/unity-status-event-save-failure-contracts.md)
- [HUD overlap task QA](../../../.flow/tasks/hud-텍스트-중앙-패널-크기-조정으로-메인-메뉴와-상태-메시지-레이아웃-겹침을-해소/qa.md)
