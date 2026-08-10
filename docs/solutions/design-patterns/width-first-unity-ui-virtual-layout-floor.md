---
title: Use a virtual layout floor for width-first Unity uGUI
date: 2026-08-05
last_updated: 2026-08-10
category: design-patterns
module: Unity generated UI layout
problem_type: design_pattern
component: testing_framework
severity: medium
applies_when:
  - "A Screen Space uGUI Canvas uses Scale With Screen Size with width-first matching"
  - "Fixed top-corner information regions must remain separate from centered action panels"
  - "Generated scene geometry needs deterministic Play Mode coverage and rendered QA evidence"
  - "Viewport-specific text assertions must describe the same live Canvas used by a requested RenderTexture capture"
related_components:
  - "development_workflow"
tags:
  - "unity"
  - "ugui"
  - "width-first-layout"
  - "virtual-viewport"
  - "playmode"
  - "text-capacity"
  - "glyph-bounds"
  - "visual-qa"
---

# Use a virtual layout floor for width-first Unity uGUI

## Context

`ToilRelicSceneBootstrap` configures the Canvas with an `800x600` reference resolution and `matchWidthOrHeight = 0`, so physical width determines the scale (`unity/Assets/Editor/ToilRelicSceneBootstrap.cs:369`). A `1280x720` viewport therefore exposes only 450 virtual height units:

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

This repository uses `800x450` as its 16:9 floor (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:30`). Keep the Canvas policy stable and size important regions against that floor. Changing `matchWidthOrHeight` to make one screen fit would rescale every generated UI surface. The bootstrap and committed scene preserve that policy as `ScaleWithScreenSize|800,600|0` (`unity/Assets/Tests/EditMode/ToilRelicSceneBootstrapEditModeTests.cs:175`).

### Assert relationships instead of duplicating constants

For fixed anchors, convert a `RectTransform` into virtual bounds from its anchor, pivot, `anchoredPosition`, and `sizeDelta`. The Play Mode helper implements that calculation and rejects unsupported stretch-anchor assumptions (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:3109`).

Protect the top region with a semantic invariant:

```text
topRegionBottom = min(hudBounds.yMin, statusBounds.yMin)
required: topRegionBottom - activePanelBounds.yMax >= 16
```

The Title and current Camp action panels are checked against this boundary at the `800x450` floor (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:1931`). BattlePanel applies the same relationship at both the `800x450` floor and `800x600` reference viewport (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:1949`). Their exact heights may differ as action counts evolve; the protected gap is the reusable contract.

Keep related contracts explicit rather than assuming that non-overlap proves usability:

- text stays at least 16px and Best Fit remains disabled;
- Title/Camp action buttons stay at least 44 virtual pixels high with positive visible gaps;
- each named button retains its exact persistent target and method, including Quit; and
- the authoritative bootstrap is changed before regenerating and testing `SampleScene`.

Current geometry constants and fixed upper-corner anchors live in `ToilRelicSceneBootstrap` (`unity/Assets/Editor/ToilRelicSceneBootstrap.cs:22`, `unity/Assets/Editor/ToilRelicSceneBootstrap.cs:521`, `unity/Assets/Editor/ToilRelicSceneBootstrap.cs:539`). Keeping this authority in the generator prevents a scene-only adjustment from disappearing on regeneration.

### Test production-shaped text capacity

Rectangle separation does not prove that wrapped status text fits. Send bounded, representative strings through the real event and presenter path, force a layout update, then compare `Text.preferredHeight` with the allocated rectangle:

```csharp
Canvas.ForceUpdateCanvases();
Assert.That(messageText.preferredHeight,
    Is.LessThanOrEqualTo(messageText.rectTransform.rect.height + 0.01f));
```

The status-capacity contract uses a victory line with loot details, a level-up line, and a save-failure line at the same time (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:2265`). During review, short placeholders had allowed a 60px body to pass; production-shaped strings required 70px, so the body was raised to 72px. This is why representative wrapping input is part of the layout contract, not merely test data (`.flow/tasks/hud-텍스트-중앙-패널-크기-조정으로-메인-메뉴와-상태-메시지-레이아웃-겹침을-해소/review.md:14`).

The Hunt Contract exposed the same failure one level deeper. Its panel satisfied the supported viewport margins and protected HUD/status gap, yet a wrapped child label still clipped. Production keeps each quarry row at 52px and stretches its `Text` child inside 12px horizontal and 2px vertical insets (`unity/Assets/Scripts/UI/HuntContractPanelController.cs:156`, `unity/Assets/Scripts/UI/HuntContractPanelController.cs:161`, `unity/Assets/Scripts/UI/HuntContractPanelController.cs:173`). The proof-first run measured the old 5px vertical inset at approximately 42.01px of available label height for 47.5px of preferred content. Reducing the inset to 2px restored roughly 48px without increasing the row or hiding another quarry (`.flow/tasks/purposeful-hunt-vertical-slice/plan.md:86`, `.flow/tasks/purposeful-hunt-vertical-slice/plan.md:87`).

Protect descendants explicitly, not just their containing panel. The Hunt Contract helper forces layout, enumerates all three generated `Quarry_*` rows, and requires each child label to satisfy `preferredHeight <= rect.height + 0.01f` (`unity/Assets/Tests/PlayMode/PurposefulHuntActionPlayModeTests.cs:370`, `unity/Assets/Tests/PlayMode/PurposefulHuntActionPlayModeTests.cs:377`, `unity/Assets/Tests/PlayMode/PurposefulHuntActionPlayModeTests.cs:382`). Run the same content-capacity check in every reachable state that changes the wrapped copy: serialized opening, open capture, Ready, and forged replay rows (`unity/Assets/Tests/PlayMode/PurposefulHuntActionPlayModeTests.cs:74`, `unity/Assets/Tests/PlayMode/PurposefulHuntActionPlayModeTests.cs:272`, `unity/Assets/Tests/PlayMode/PurposefulHuntActionPlayModeTests.cs:279`, `unity/Assets/Tests/PlayMode/PurposefulHuntActionPlayModeTests.cs:286`). The paired capture helper then renders each requested state at 1280x720 and 800x600 (`unity/Assets/Tests/PlayMode/PurposefulHuntActionPlayModeTests.cs:301`, `unity/Assets/Tests/PlayMode/PurposefulHuntActionPlayModeTests.cs:303`).

(session history) The initial Purposeful Hunt planning assigned panel geometry and graphics evidence to QA but did not name descendant text capacity as a separate invariant. The later clipping failure is why both the container contract and the content contract must remain explicit.

Compact labels can reduce pressure without removing meaning. `HudController` uses `Lv`, `Part`, and `Wpn` while preserving HP, level progress, inventory counts, weapon identity, ATK, and DEF (`unity/Assets/Scripts/UI/HudController.cs:33`). Do not use automatic font shrinking as a substitute for capacity.

### Use rendered evidence as a complementary oracle

Deterministic geometry and typography tests are the default regression layer. A separate opt-in test renders required states and viewports through a `RenderTexture` when `TOIL_RELIC_LAYOUT_EVIDENCE_DIR` is set; without it, NUnit reports an explicit skip (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:2381`). Run this lane with a graphics device, not `-nographics`.

For viewport-sensitive assertions, use a **Viewport-Faithful Render Contract**: attach the requested camera and `RenderTexture`, switch the Canvas to that camera, wait for layout stabilization, call `Canvas.ForceUpdateCanvases()`, and only then assert the camera dimensions, live CanvasScaler, derived Canvas rectangle, and visible glyph bounds. The capture helper invokes the assertion callback in that order before rendering pixels (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:3579`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:3588`).

Do not infer the virtual size from the filename alone. Read the live CanvasScaler and require the scaling premise before checking positions. The Battle callback verifies `ScaleWithScreenSize`, the `800x600` reference, width-first matching, and the derived virtual Canvas size (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:3012`). At `1280x720` that live rectangle must be `800x450`; at `800x600` it must be `800x600`.

RectTransform separation is still insufficient for rendered text. Populate the generated glyphs, convert their vertices into one common Canvas coordinate space, require every glyph rectangle to remain within its owning text rectangle, then compare semantic regions in that same space (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:2955`, `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:2998`). Battle evidence protects Message, Enemy, Phase, and Log content and asserts the status-to-enemy glyph gap before the first render (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:3037`).

Drive risky evidence through a real product transition. The accepted Battle captures enter through `EnterBattle()`, publish deterministic enemy and log content, and then capture both a retained save-failure warning and a normal status at `1280x720` and `800x600` with the same live callback (`unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs:2526`). This prevents a geometrically convenient but unreachable test state from certifying the layout.

The callback proves the requested viewport's layout contract, but it still does not prove that the saved pixels are useful. Separately verify NUnit XML, filenames, exact dimensions, non-uniform pixels, and direct visual inspection for state, hierarchy, clipping, and overlap. Keep screenshots supplementary because pixel evidence alone cannot identify which geometry invariant regressed. See [screenshot evidence validity](../best-practices/unity-playmode-screenshot-evidence-validity.md) for the artifact trust boundary.

## Why This Matters

The evidence layers answer different questions:

1. Edit Mode semantic parity proves that bootstrap generation and the committed scene preserve the scaling and layout authority.
2. Virtual bounds and production-shaped `preferredHeight` checks prove separation and text capacity cheaply.
3. The live capture callback proves that the requested physical viewport produces the expected virtual Canvas and glyph relationships.
4. PNG integrity and visual inspection prove that the saved pixels show the intended readable composition.

Combining them prevents common false confidence: checking only the reference resolution, testing only short text, measuring glyphs against the wrong Canvas, accepting an optional capture that produced nothing, or treating a non-empty PNG as visual proof.

The BattlePanel task's final QA recorded generated-scene parity, four targeted Battle passes, 62 full-suite passes with one intentional opt-in capture skip, a separate passing graphics run, four inspected Battle images, and a clean console build (`.flow/tasks/battle-panel-우측-상단-상태-메시지-16-9-중첩-재설계/qa.md:20`). Historical sessions had already shown why the layers must remain distinct: batch `ScreenCapture` was unreliable, `Screen.SetResolution` did not establish the requested viewport, and `-nographics` could produce a passing but uniform image. The live callback closes the remaining gap without treating those historical observations as current-tree proof.

## When to Apply

- Fixed-anchor Screen Space uGUI layouts with a stable Canvas scaling policy.
- Generated scenes where a bootstrap owns serialized layout geometry.
- Bounded status formats that can be represented by realistic worst cases.
- Batch Play Mode suites that need deterministic layout checks plus separately requested visual evidence.

Use adaptive layout groups, safe-area handling, scrolling, truncation policy, or breakpoint-specific contracts instead when localization, user-authored text, accessibility scaling, rotation, or platform error detail is unbounded. Do not apply one panel's dimensions to a structurally different surface. Reuse the virtual-floor and live-render relationships, then give each surface geometry that matches its information and actions.

## Examples

The original task used the `800x450` floor to compact the HUD/status regions and a three-action central panel while preserving a 16-unit gap. Later equipment and save-status changes gave Camp a distinct four-action geometry and added a separate save row.

BattlePanel then applied the same method to a different surface: combat information plus a `2x2` action grid. Its cheap tests cover the `800x450` and `800x600` virtual rectangles, readable generated text, spatial navigation, phase-driven availability, and the newest-two-log contract. Its graphics lane reasserts the live Canvas premise and glyph gap for normal and retained-save-failure states before saving each requested viewport image. The reusable part is the agreement between layout authority, live viewport, glyph bounds, and pixels—not the BattlePanel's exact coordinates.

Purposeful Hunt added a nested-row example. The outer Hunt Contract panel already passed its virtual viewport and HUD/status separation checks, while the longest guaranteed-contribution label still needed 47.5px inside a 42.01px rectangle. Preserving the 52px row and 14px font, reducing only the vertical inset, and asserting every generated label across open, Ready, and forged states fixed the content contract. Final QA passed the focused action lane twice, the full 103-test Play Mode regression, and a graphics-enabled 12-image capture set whose 1280x720 and 800x600 pixels were validated and directly inspected (`.flow/tasks/purposeful-hunt-vertical-slice/qa.md:46`, `.flow/tasks/purposeful-hunt-vertical-slice/qa.md:47`, `.flow/tasks/purposeful-hunt-vertical-slice/qa.md:48`, `.flow/tasks/purposeful-hunt-vertical-slice/qa.md:50`, `.flow/tasks/purposeful-hunt-vertical-slice/qa.md:60`, `.flow/tasks/purposeful-hunt-vertical-slice/qa.md:74`).

## Related

- [Validate Unity Play Mode screenshot pixels before accepting visual evidence](../best-practices/unity-playmode-screenshot-evidence-validity.md)
- [Unity Play Mode HUD contract testing without a runtime assembly reference](../best-practices/unity-playmode-hud-contracts.md)
- [Deterministic Unity Play Mode action contracts through serialized UI](../best-practices/deterministic-unity-playmode-action-contracts.md)
- [Preserve Unity terminal outcomes through level-ups and save failures](../ui-bugs/unity-status-event-save-failure-contracts.md)
- [HUD overlap task QA](../../../.flow/tasks/hud-텍스트-중앙-패널-크기-조정으로-메인-메뉴와-상태-메시지-레이아웃-겹침을-해소/qa.md)
- [BattlePanel widescreen task QA](../../../.flow/tasks/battle-panel-우측-상단-상태-메시지-16-9-중첩-재설계/qa.md)
- [Purposeful Hunt vertical slice QA](../../../.flow/tasks/purposeful-hunt-vertical-slice/qa.md)
