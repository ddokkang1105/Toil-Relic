# QA

## Result

`passed`

RW10-RW11 close both findings from the previous QA round. The longest quarry summary and all completed/replay summaries fit at both captured resolutions, the focused action command selects six tests, and the complete console and Unity regressions remain green. The task can advance to `close`.

## Framework and environment

- Personal Flow probe: OpenSpec CLI available without project artifacts; gstack available; Compound Engineering 3.21.4 available; OMX unavailable.
- QA route: repository-native console and Unity validation. Browser-oriented gstack QA does not apply to this console + Unity project.
- Unity: `6000.3.19f1`, matching `unity/ProjectSettings/ProjectVersion.txt`.
- Branch and QA head: `codex/purposeful-hunt-vertical-slice` at `9f18fa80331dba830325f9ef63ad2a256c24edd5`.
- Unity commands were launched as hidden `Start-Process -Wait` processes where a strict fresh-process boundary was required. The effective Unity command lines are recorded below.

## Commands executed

```powershell
dotnet build src/ToilRelic/ToilRelic.csproj --nologo
dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --nologo --filter 'FullyQualifiedName~PurposefulHuntContentTests|FullyQualifiedName~PurposefulHuntProjectTests|FullyQualifiedName~SaveSystemTests' --logger 'trx;LogFileName=qa-rw10-console-content-save.trx' --results-directory .flow/tasks/purposeful-hunt-vertical-slice
dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --nologo --filter 'FullyQualifiedName~GameHuntUxTests|FullyQualifiedName~GameEquipmentUxTests' --logger 'trx;LogFileName=qa-rw10-console-ux.trx' --results-directory .flow/tasks/purposeful-hunt-vertical-slice
dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --nologo --logger 'trx;LogFileName=qa-rw10-console-full.trx' --results-directory .flow/tasks/purposeful-hunt-vertical-slice
```

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform EditMode -assemblyNames ToilRelic.EditModeTests -testResults '.flow\tasks\purposeful-hunt-vertical-slice\qa-rw10-editmode-results.xml' -logFile '.flow\tasks\purposeful-hunt-vertical-slice\qa-rw10-editmode.log'
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testCategory PurposefulHuntActionContracts -testResults '.flow\tasks\purposeful-hunt-vertical-slice\qa-rw10-actions-r1b-results.xml' -logFile '.flow\tasks\purposeful-hunt-vertical-slice\qa-rw10-actions-r1b.log'
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testCategory PurposefulHuntActionContracts -testResults '.flow\tasks\purposeful-hunt-vertical-slice\qa-rw10-actions-r2b-results.xml' -logFile '.flow\tasks\purposeful-hunt-vertical-slice\qa-rw10-actions-r2b.log'
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testResults '.flow\tasks\purposeful-hunt-vertical-slice\qa-rw10-playmode-results.xml' -logFile '.flow\tasks\purposeful-hunt-vertical-slice\qa-rw10-playmode.log'
$env:TOIL_RELIC_LAYOUT_EVIDENCE_DIR = 'C:\Toil-Relic-main\.flow\tasks\purposeful-hunt-vertical-slice\qa-rw10-layout-evidence-final'
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testFilter 'ToilRelic.PlayModeTests.PurposefulHuntActionPlayModeTests.PurposefulHunt_CaptureLayoutEvidenceWhenRequested' -testResults '.flow\tasks\purposeful-hunt-vertical-slice\qa-rw10-capture-results.xml' -logFile '.flow\tasks\purposeful-hunt-vertical-slice\qa-rw10-capture.log'
```

The PNG verifier used the bundled Codex Python runtime with Pillow. It required exactly the six named states at both `1280x720` and `800x600`, exact image dimensions, non-zero file sizes, and more than one RGB color.

## Automated checks

| Gate | Result | Evidence |
|---|---|---|
| Console compile | Pass: 0 warnings, 0 errors | Command output from this QA run. |
| Console content/save filter | Pass: 63/63 | `qa-rw10-console-content-save.trx` |
| Console Hunt/equipment UX filter | Pass: 13/13 | `qa-rw10-console-ux.trx` |
| Full console regression | Pass: 86/86 | `qa-rw10-console-full.trx` |
| Unity Edit Mode | Pass: 2/2 | `qa-rw10-editmode-results.xml`, `qa-rw10-editmode.log` |
| Unity action category run 1 | Pass: total 6, 5 passed, 0 failed, 1 graphics-only skip | `qa-rw10-actions-r1b-results.xml`, `qa-rw10-actions-r1b.log` |
| Unity action category run 2, fresh process | Pass: total 6, 5 passed, 0 failed, 1 graphics-only skip | `qa-rw10-actions-r2b-results.xml`, `qa-rw10-actions-r2b.log` |
| Full Unity Play Mode | Pass: 103 passed, 0 failed, 2 conditional graphics skips out of 105 | `qa-rw10-playmode-results.xml`, `qa-rw10-playmode.log` |
| Graphics-enabled capture filter | Pass: 1/1 | `qa-rw10-capture-results.xml`, `qa-rw10-capture.log` |
| PNG validity | Pass: 12/12 expected files, exact dimensions, non-zero bytes, 1,268-3,186 unique RGB colors | `qa-rw10-layout-evidence-final/` |
| Tracked worktree integrity | Pass: no tracked file changed during test execution | `git status --short --untracked-files=no` |

The full console and Unity suites include both native readers for the shared content, migration, and pure-command vectors. This retains the mirrored gameplay contract while the rework changes only Unity row presentation, its Action Contract assertions, and setup documentation.

## Manual and production-shaped scenarios

| Scenario | Expected | Observed result |
|---|---|---|
| Focused category command | Select the real Purposeful Hunt action fixture and never pass with an empty selection | Pass: both fresh runs selected total 6; the capture-only case skipped without its evidence variable. |
| Hunt Contract open at 1280x720 and 800x600 | All three rows show quarry, danger, optional profile reward, and complete guaranteed contribution | Pass: `Rustheart Core` is fully visible in both `hunt-contract-open` captures. |
| Ready and forged rows at both resolutions | Every completed row shows the full `Completed - replay only` meaning inside its row | Pass: all three `replay only` lines remain inside their rows in four captures. |
| Six named states at both resolutions | No panel/HUD/status overlap or clipped actionable content | Pass: all 12 current images were directly inspected after pixel validation. |
| Hunt -> second quarry -> victory -> replay -> Cancel -> Ready -> Forge -> Keep -> Equip -> reload | Selection, progress, ownership, explicit equipment choice, focus, and persistence remain authoritative | Pass through the real serialized Action Contract in both fresh runs and the full suite. |
| Invalid Contract and forced pre-write save failure | No invalid gameplay mutation; actionable status remains visible | Pass in the full suite and in the inspected invalid/save-failure captures. |

## Findings

None.

### Previous Q1 disposition

`resolved`

The old 5px vertical inset provided approximately `42.01px` for a `47.5px` label. The 2px inset provides the required height, the regression checks every generated `Quarry_*` label with `preferredHeight <= rect.height`, and the fresh captures show the complete text at both resolutions.

### Previous Q2 disposition

`resolved`

`unity/UNITY_SETUP.md` and the source fixture now agree on `PurposefulHuntActionContracts`. Two fresh executions each selected six tests, so the previously possible successful `total=0` false positive is removed from the documented workflow.

## Known limits

- Graphics capture ran in Unity batch mode with graphics enabled. It exercised the live serialized scene and rendering, but it was not a free-form human play session.
- The production scene/bootstrap files were unchanged by RW10-RW11. This pass used the 2/2 Edit Mode committed/disposable equivalence checks and did not rerun the write-producing bootstrap command; the previous QA bootstrap pass remains applicable.
- The first attempted action run started immediately after Edit Mode shutdown and hit Unity's single-project lock before producing a result XML. It was excluded and replaced by two clean fresh-process runs (`r1b`, `r2b`).
- Unity logged an access-token refresh warning in each process, but licensing remained sufficient; every accepted process exited 0 and produced passing NUnit XML.
- Atomic replacement and prior-file durability during a true mid-write crash remain deferred to the Ironclad Save Envelope. This slice proves pre-write failure and later recovery only.
- QA evidence files remain local and untracked under this task directory; the workflow commit contains only `qa.md` and `state.yaml`.

## Disposition

Advance to `close`. All acceptance-critical console, Unity behavior, deterministic category, rendered layout, and documentation checks pass with no unresolved finding.
