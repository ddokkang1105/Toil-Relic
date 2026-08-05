# QA

## Outcome

`pass`

All nine acceptance criteria passed through semantic scene parity, targeted and full Unity tests, a fresh graphics-enabled render run, image inspection, and the console build. The task is ready for the Personal Flow close stage.

## Route and environment

- Personal Flow probe: OpenSpec CLI available but not active for this existing task; gstack and Compound Engineering available; OMX unavailable.
- QA route: repo-appropriate Unity and console validation. The active gstack `qa` skill targets browser applications and does not match this Unity desktop/console project.
- Branch/head: `codex/play-mode-qa-expansion` at `2bcb0c12fc32bd4a55ca91611ee4f0669bdc5008`.
- Unity: `6000.3.19f1 (7689f4515d75)` on Windows 11 x64.
- .NET SDK: `10.0.302`; console target remains `net8.0`.
- QA logs and fresh PNGs: `C:\Users\User\AppData\Local\Temp\toil-relic-battle-panel-qa-20260805\`.

## Automated checks

| Check | Result | Evidence |
|---|---|---|
| Official `ConfigureSampleScene` bootstrap | Unity exit 0. The serializer changed file IDs/YAML order as already documented; semantic parity is the authoritative signal. The transient scene output was restored after QA. | `bootstrap.log` in the temporary QA directory |
| Full Edit Mode assembly | 1 passed, 0 failed. Generated and committed scene contracts match for hierarchy, refs, actions, geometry, navigation, `maxLogLines=2`, and `ScaleWithScreenSize\|800,600\|0`. | `qa-editmode-results.xml` |
| Battle-targeted Play Mode | 4 passed, 0 failed. Layout floor, rendered text bounds, 2x2 actions/logs, and phase-driven action availability all passed. | `qa-battle-results.xml` |
| Full Play Mode assembly | 62 passed, 0 failed, 1 intentionally ignored opt-in graphics test. | `qa-playmode-results.xml` |
| Graphics-enabled capture | 1 passed, 0 failed; 22 non-placeholder frames written. The test used the real `EnterBattle()` path and asserted the requested live CanvasScaler/viewport/glyph contract before capture. | `qa-layout-results.xml` |
| Battle PNG integrity | All four requested images have exact dimensions and more than one sampled color. | Fresh PNG details below |
| Console build | 0 warnings, 0 errors. | `dotnet build src/ToilRelic/ToilRelic.csproj --nologo` |

### Exact commands

Unity commands used the installed executable below. On Windows they were awaited with `Start-Process -Wait -WindowStyle Hidden` so only one Unity process owned the project at a time.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Toil-Relic-main\unity' -executeMethod ToilRelic.Unity.Editor.ToilRelicSceneBootstrap.ConfigureSampleScene -logFile '<temp>\bootstrap.log'
```

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform EditMode -assemblyNames ToilRelic.EditModeTests -testResults '<task>\qa-editmode-results.xml' -logFile '<temp>\editmode.log'
```

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testFilter 'ToilRelic.PlayModeTests.SampleSceneP0PlayModeTests.P0_Battle' -testResults '<task>\qa-battle-results.xml' -logFile '<temp>\battle.log'
```

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testResults '<task>\qa-playmode-results.xml' -logFile '<temp>\playmode.log'
```

```powershell
$env:TOIL_RELIC_LAYOUT_EVIDENCE_DIR = '<temp>\layout-evidence'
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testFilter 'ToilRelic.PlayModeTests.SampleSceneP0PlayModeTests.P0_CaptureLayoutEvidenceWhenRequested' -testResults '<task>\qa-layout-results.xml' -logFile '<temp>\layout.log'
```

```powershell
dotnet build src/ToilRelic/ToilRelic.csproj --nologo
```

## Fresh render evidence

| Image | Dimensions | Bytes | Sample colors | SHA-256 prefix |
|---|---:|---:|---:|---|
| `battle-failure-1280x720.png` | 1280x720 | 114,630 | 51 | `7CACD3F124EFAA64` |
| `battle-failure-800x600.png` | 800x600 | 66,895 | 25 | `CDD8B37A71CE36C6` |
| `battle-normal-1280x720.png` | 1280x720 | 104,762 | 56 | `1B4381D8B9421B01` |
| `battle-normal-800x600.png` | 800x600 | 60,823 | 20 | `0C509C0C6BD19AF4` |

## Manual scenarios

| Scenario | Expected | Observed result |
|---|---|---|
| 1280x720 normal Battle | Single-line status stays above the BattlePanel; enemy, phase, two log lines, and actions remain readable. | Pass. No glyph overlap or clipping. The information order is clear and the panel stays inside the widescreen floor. |
| 1280x720 prior save-failure Battle | Two-line status keeps a protected gap above EnemyText without hiding the warning or Battle information. | Pass. Both status lines and all Battle rows remain fully visible. |
| 800x600 normal Battle | The right-side hierarchy remains stable with the extra vertical space. | Pass. Panel position, text rows, and button grid remain balanced and unobstructed. |
| 800x600 prior save-failure Battle | Status and BattlePanel remain separated at 4:3. | Pass. No clipping, overlap, or unexpected panel movement. |
| 2x2 action presentation | Four equal buttons use uniform positive row/column spacing and centered labels. | Pass in all four images. Persistent action, raycast, and explicit navigation behavior also passed the targeted test. |
| Phase-driven disabled actions | EnemyAction and Resolving disable actions without clearing information; PlayerAction re-enables them. | Pass through `P0_BattleUiShowsStateAndDisablesActionsOutsidePlayerPhase`. No separate disabled-state screenshot was requested. |

## Acceptance traceability

| Acceptance criterion | Status | Proof |
|---|---|---|
| AC1 status-to-BattlePanel separation at 1280x720 | Pass | Live viewport/glyph assertions plus normal and save-failure 1280x720 captures |
| AC2 long/multiline status and newest-two logs | Pass | Battle targeted tests and save-failure captures |
| AC3 readable 16px+ text without Best Fit | Pass | `P0_BattleTextUsesReadableNonoverlappingRenderedBounds` |
| AC4 2x2 actions, size, spacing, persistent methods, navigation | Pass | `P0_BattleActionsUseTwoByTwoSpatialNavigationAndNewestTwoLogs` |
| AC5 BattlePanel inside 800x450 floor | Pass | `P0_BattlePanelFitsBelowVisibleStatusAtWidescreenFloor` and graphics callback |
| AC6 800x600 hierarchy regression | Pass | Both 800x600 captures and full Play Mode assembly |
| AC7 bootstrap and Unity regression | Pass | Edit Mode 1/1, Battle 4/4, full Play Mode 62/0/1 |
| AC8 graphics-enabled real-pixel evidence | Pass | Graphics test 1/1; four requested images have valid dimensions and non-uniform pixels |
| AC9 console build | Pass | 0 warnings, 0 errors |

## Known limits

- The graphics capture remains an opt-in lane and is intentionally ignored by the headless full Play Mode run. It must continue to run separately for viewport-faithful protection.
- Unity's generated scene file IDs and YAML ordering are not byte-stable. The semantic Edit Mode contract is the accepted idempotence authority and passed; the QA-generated transient scene diff was removed without staging it.
- The fresh QA PNG directory is temporary. Durable work-stage Battle PNGs remain committed under `work-rework-layout-evidence-r1/`, while this QA stage commits the fresh NUnit XML results and exact visual observations.
- No long-duration play soak, standalone player build, or device-specific DPI pass was run; none is required by this task's acceptance criteria.
- Existing unrelated untracked `equipment-comparison/work-*` and intermediate non-Battle evidence were not modified, deleted, or staged.
