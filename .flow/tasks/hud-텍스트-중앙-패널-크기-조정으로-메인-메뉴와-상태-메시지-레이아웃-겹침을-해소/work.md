# Work

## Result

Implemented the approved compact Unity layout and regenerated `SampleScene` from `ToilRelicSceneBootstrap`.

## Implementation

- U1: Added deterministic Play Mode contracts for the virtual `800×450` layout floor, Title/Camp separation, button geometry, readable typography, compact HUD semantics, and three-message status capacity.
- U2: Centralized top-region and three-action menu geometry in `ToilRelicSceneBootstrap`.
  - HUD: `344×94`, 16px text, 16px top-left margin.
  - GameStatus: `400×96`, 16px text, 16px top-right margin, 72px message body.
  - TitlePanel/CampPanel: shared `280×196` geometry at vertical offset `-4`.
  - Title/Camp buttons: `220×44`, centered at `+52`, `0`, and `-52`.
  - BattlePanel geometry and behavior were left unchanged.
- U2: Shortened HUD labels to `Lv`, `Part`, and `Wpn` while preserving HP, level progress, all inventory values, weapon identity, ATK, and DEF.
- U3: Regenerated `unity/Assets/Scenes/SampleScene.unity` through `ConfigureSampleScene`.
- U4: Added an opt-in RenderTexture-based Unity capture path and generated viewport evidence.

## Verification evidence

| Unit | Evidence |
|---|---|
| Baseline | Existing Play Mode suite: 16 passed, 0 failed (`work-baseline-results.xml`). |
| U1 red proof | New contracts produced 3 expected failures: HUD width 560 > 344, status preferred height 60 > rectangle 48, and Title overlap gap -89 < 16 (`work-red-results.xml`). |
| U2/U3 | Unity bootstrap exited successfully (`work-bootstrap.log`). |
| U1–U3 initial | Complete `ToilRelic.PlayModeTests`: 21 passed, 0 failed (`work-final-playmode-results.xml`). |
| Repository baseline | `dotnet build` in `src/ToilRelic`: succeeded with 0 warnings and 0 errors. |
| U4 | Capture test: 1 passed, 0 failed (`work-capture-results.xml`); six PNGs stored under `evidence/`. |
| Review rework | Production-shaped status strings required 70px rather than the initial 60px. The body was increased to 72px, Title/Camp moved down 4px, button/action contracts were strengthened, and the opt-in capture gate was made explicit. |
| Review final | Standard suite: 20 passed, 0 failed, 1 explicitly skipped opt-in capture test (`review-final-playmode-results.xml`). Capture invocation: 1 passed, 0 failed (`review-capture-results.xml`). |

## Visual observations

- 1280×720 Title and Camp: HUD, status, and the central panel are visibly separated; no clipping was observed.
- 800×600 Title and Camp: panels remain centered, controls remain readable, and the larger vertical space is preserved.
- 1280×720 production-shaped three-message stress case: wrapped victory loot, level-up, and save-failure messages all remain visible.
- 1280×720 Battle: battle information and all four controls remain usable. The pre-existing upper-right status/BattlePanel overlap remains visible and is recorded as separate follow-up work.

## Changed paths

- `unity/Assets/Editor/ToilRelicSceneBootstrap.cs`
- `unity/Assets/Scripts/UI/HudController.cs`
- `unity/Assets/Scenes/SampleScene.unity`
- `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`
- `.flow/tasks/hud-텍스트-중앙-패널-크기-조정으로-메인-메뉴와-상태-메시지-레이아웃-겹침을-해소/evidence/`

No implementation commit was created. Unrelated user-owned worktree changes were preserved.
