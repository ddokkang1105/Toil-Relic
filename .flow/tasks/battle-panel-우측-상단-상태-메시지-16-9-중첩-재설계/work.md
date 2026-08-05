# Work

## Status

Completed on 2026-08-05. U1-U4 and T1-T4 are implemented and locally verified. The task is ready for the `review` stage.

## Changes

### U1 — Contract-first RED proof

- Extended Play Mode coverage for the `800x450` virtual widescreen floor and `800x600` viewport, active GameStatus-to-BattlePanel separation, generated glyph containment/gaps, row spacing, persistent button actions, explicit 2x2 navigation, phase-driven action availability, Camp cleanup, and newest-two-log eviction.
- Extended Edit Mode generated-scene parity for BattlePanel geometry, controller references, persistent actions, `maxLogLines=2`, and explicit navigation targets.
- Confirmed the pre-change implementation failed the new contract as intended: the log contract expected 2 logical lines but retained 10 (`work-u1-red-results.xml`).

### U2 — Compact BattlePanel implementation

- Changed the generated BattlePanel to `(180,-43)` with size `320x280` while preserving GameStatus, CanvasScaler, and combat behavior.
- Positioned the information rows at Enemy `(0,119)` / `280x24`, Phase `(0,91)` / `280x24`, and Log `(0,42)` / `280x66`.
- Replaced the vertical action stack with a `136x44` 2x2 grid: Attack/Defend on the first row and Flee/Potion on the second row.
- Added explicit spatial navigation with left/right wrapping within each row and up/down movement between columns.
- Reduced BattleLog retention to the newest two non-empty logical lines in chronological order. Embedded newlines, blank lines, CRLF, and trailing newlines are normalized without adding a scrollbar or changing public APIs.

### U3 — Generated scene parity

- Regenerated `unity/Assets/Scenes/SampleScene.unity` through `ToilRelicSceneBootstrap.ConfigureSampleScene`.
- Verified disposable and committed scene parity for hierarchy, references, geometry, persistent action methods, log capacity, and navigation.
- Ran the bootstrap twice and confirmed semantic idempotence through the Edit Mode parity contract.

### U4 — Render evidence and regression coverage

- Updated the graphics evidence flow to enter battle through the real `StartHunt` path for both normal and prior-save-failure states.
- Captured and visually inspected normal and failure Battle screens at `1280x720` and `800x600`. All four show separated status/enemy/phase/log glyphs, an aligned 2x2 action grid, and clear viewport margins.
- Preserved the full capture set in `work-u4-layout-evidence-r1`; the four Battle images are the task-specific evidence.

### Simplification pass

- Applied 3 behavior-preserving improvements: factored repeated explicit-navigation initialization, retained a reusable bounded logical-log queue so existing text is not reparsed on every event, and reused the already-calculated Enemy glyph bounds in the Play Mode assertion.
- Kept the plan-pinned `StringBuilder`, string-based parity contract, and conservative worst-case text fixture. A new navigation value type and replacement of the stress fixture with only reachable event combinations were rejected as extra abstraction or weaker boundary coverage.

### Changed product and test files

- `unity/Assets/Editor/ToilRelicSceneBootstrap.cs`
- `unity/Assets/Scenes/SampleScene.unity`
- `unity/Assets/Scripts/UI/BattlePanelController.cs`
- `unity/Assets/Tests/EditMode/ToilRelicSceneBootstrapEditModeTests.cs`
- `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`

## Verification

| Check | Result | Evidence |
|---|---|---|
| Contract RED proof | Expected failure: 2 retained lines vs previous 10 | `work-u1-red-results.xml` |
| Generated-scene Edit Mode parity | 1/1 passed | `work-u3-editmode-results.xml` |
| Focused 2x2 navigation/log Play Mode | 1/1 passed after correcting the test setup to use real battle entry | `work-u3-playmode-r2-results.xml` |
| Focused layout/glyph Play Mode | 2/2 passed | `work-u3-layout-results.xml` |
| Graphics capture | 1/1 passed; 22 PNG files written | `work-u4-layout-results.xml`, `work-u4-layout-evidence-r1/` |
| Full Edit Mode before simplification | 1/1 passed | `work-u4-editmode-results.xml` |
| Full Play Mode before simplification | 62 passed, 0 failed, 1 intentionally ignored capture test | `work-u4-playmode-results.xml` |
| Edit Mode after simplification | 1/1 passed | `work-simplify-editmode-results.xml` |
| Full Play Mode after simplification | 62 passed, 0 failed, 1 intentionally ignored capture test | `work-simplify-playmode-results.xml` |
| Console build | 0 warnings, 0 errors | `dotnet build src/ToilRelic/ToilRelic.csproj --nologo` |

Final Unity commands used:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform EditMode -assemblyNames 'ToilRelic.EditModeTests' -testResults '<task>\work-simplify-editmode-results.xml' -logFile '<task>\work-simplify-editmode.log'
```

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames 'ToilRelic.PlayModeTests' -testResults '<task>\work-simplify-playmode-results.xml' -logFile '<task>\work-simplify-playmode.log'
```

Render evidence integrity samples:

| Image | Size | Sample hash |
|---|---:|---|
| `battle-failure-1280x720.png` | 114,633 bytes | `50AB69F4B6952749` |
| `battle-failure-800x600.png` | 66,895 bytes | `CDD8B37A71CE36C6` |
| `battle-normal-1280x720.png` | 104,862 bytes | `0355B77722379741` |
| `battle-normal-800x600.png` | 60,823 bytes | `0C509C0C6BD19AF4` |

## Deviations

- The planned byte-identical second scene regeneration was not a valid success signal with this Unity serializer: regenerated object/file IDs and YAML ordering changed between runs. Semantic Edit Mode parity is the authoritative idempotence check and passed.
- The first focused navigation/log Play Mode run failed because the new test changed state directly and therefore bypassed the real battle-entry interactability path. The test was corrected to use `EnterBattle()`; the unchanged production implementation then passed.
- The generated scene has a large serializer-owned YAML diff and Unity's standard blank-value trailing spaces. It was kept as generated instead of hand-formatting the scene authority.
- No console gameplay implementation changed because this task only restructures Unity UI presentation and input navigation.

## Review rework - 2026-08-05

### Resolved findings

- **#1 viewport-faithful glyph guard:** the graphics capture helper now runs an optional Battle layout assertion while the requested `RenderTexture` and camera are attached. Each `1280x720` and `800x600` Battle capture verifies the serialized width-first `CanvasScaler`, the resulting `800x450` or `800x600` Canvas rect, status/enemy/phase/log glyph containment, and the 24px status-to-enemy glyph gap. Edit Mode generated-scene parity now also preserves `ScaleWithScreenSize | 800x600 | match=0`.
- **#3 multiline logical-log contract:** the existing Battle action test now raises one whitespace-padded payload containing three logical lines, blank segments, LF, CRLF, and a trailing newline through `GameEvents`. It asserts the exact newest two trimmed non-empty lines.

This rework changes test behavior only; no product/runtime files changed. The evidence strategy was characterization and contract strengthening rather than a red product test because the current rendered layout and parser behavior were expected to remain valid. The earlier passing work artifacts established the weaker baseline, and the strengthened tests now prove the missing branches directly.

### Framework route

- Personal Flow probe: OpenSpec CLI available but not active for this existing task; gstack and Compound Engineering available; OMX unavailable.
- Execution: active `ce-work` in return-to-caller mode, native inline strategy. Both findings touched the same Play Mode fixture, so no parallel worker lane was used.

### Verification

| Check | Result | Evidence |
|---|---|---|
| Generated-scene Edit Mode parity and CanvasScaler contract | 1/1 passed | `work-rework-editmode-results.xml` |
| Multiline log and 2x2 action contract | 1/1 passed | `work-rework-action-results.xml` |
| Graphics-enabled viewport/glyph capture contract | 1/1 passed | `work-rework-layout-results.xml`, `work-rework-layout-evidence-r1/` |
| Full Play Mode assembly | 62 passed, 0 failed, 1 intentionally ignored opt-in capture test | `work-rework-playmode-results.xml` |
| Console build | 0 warnings, 0 errors | `dotnet build src/ToilRelic/ToilRelic.csproj --nologo` |

The graphics run produced 22 PNGs. The four Battle images were checked for exact dimensions, non-uniform sampled pixels, and visual separation:

| Image | Size | Sample colors | Sample hash |
|---|---:|---:|---|
| `battle-failure-1280x720.png` | 114,571 bytes | 98 | `E9A1F71752CB201E` |
| `battle-failure-800x600.png` | 66,895 bytes | 42 | `CDD8B37A71CE36C6` |
| `battle-normal-1280x720.png` | 104,713 bytes | 94 | `CC89451AB7211133` |
| `battle-normal-800x600.png` | 60,823 bytes | 42 | `0C509C0C6BD19AF4` |

All four show clear separation between GameStatus and Enemy text, contained Enemy/Phase/log rows, and an aligned 2x2 action grid.

Final accepted commands omitted `-quit`; two earlier Edit Mode attempts that included `-quit` completed compilation but exited before the Unity Test Runner wrote XML, so they were not accepted as verification.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform EditMode -assemblyNames 'ToilRelic.EditModeTests' -testResults '<task>\work-rework-editmode-results.xml' -logFile '<task>\work-rework-editmode.log'
```

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames 'ToilRelic.PlayModeTests' -testFilter 'ToilRelic.PlayModeTests.SampleSceneP0PlayModeTests.P0_BattleActionsUseTwoByTwoSpatialNavigationAndNewestTwoLogs' -testResults '<task>\work-rework-action-results.xml' -logFile '<task>\work-rework-action.log'
```

```powershell
$env:TOIL_RELIC_LAYOUT_EVIDENCE_DIR = '<task>\work-rework-layout-evidence-r1'
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames 'ToilRelic.PlayModeTests' -testFilter 'ToilRelic.PlayModeTests.SampleSceneP0PlayModeTests.P0_CaptureLayoutEvidenceWhenRequested' -testResults '<task>\work-rework-layout-results.xml' -logFile '<task>\work-rework-layout.log'
```

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames 'ToilRelic.PlayModeTests' -testResults '<task>\work-rework-playmode-results.xml' -logFile '<task>\work-rework-playmode.log'
dotnet build src/ToilRelic/ToilRelic.csproj --nologo
```

### Rework status

Findings #1 and #3 are implemented and locally verified. R8 and U4 now have viewport-faithful automated evidence, and the task is ready to return to `review`.
