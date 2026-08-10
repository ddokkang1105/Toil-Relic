# QA

## Result

`rework required`

All automated gameplay and regression checks passed. Visual inspection found one production label-clipping defect, and the documented focused-test command uses a stale category name. The task returns to `work` for RW10-RW11.

## Framework and environment

- Personal Flow probe: OpenSpec CLI available without project artifacts; gstack available; Compound Engineering 3.21.4 available; OMX unavailable.
- QA route: repository-native console and Unity validation. Browser-oriented gstack QA does not apply to this console + Unity project.
- Unity: `6000.3.19f1`, matching `unity/ProjectSettings/ProjectVersion.txt`.
- Branch and reviewed head at QA start: `codex/purposeful-hunt-vertical-slice` at `40fbdd60fe6d17ae100643640bed55f70127f7ed`.

## Automated checks

| Command or gate | Result | Evidence |
|---|---|---|
| `dotnet build src/ToilRelic/ToilRelic.csproj --nologo` | Pass: 0 warnings, 0 errors | Console output captured in this QA run. |
| Console content/save filter from the verification contract | Pass: 63/63 | `qa-console-content-save.trx` |
| Console Hunt/equipment UX filter from the verification contract | Pass: 13/13 | `qa-console-ux.trx` |
| Full console regression | Pass: 86/86 | `qa-console-full.trx` |
| Unity bootstrap `ToilRelicSceneBootstrap.ConfigureSampleScene` | Pass: exit 0; generated scene was semantically validated and the QA-only fileID rewrite was restored | `qa-bootstrap.log` |
| Unity Edit Mode `ToilRelic.EditModeTests` | Pass: 2/2 | `qa-editmode-results.xml`, `qa-editmode.log` |
| Unity action category run 1, `PurposefulHuntActionContracts` | Pass: 5 passed, 0 failed, 1 graphics-only skip | `qa-actions-r1-results.xml`, `qa-actions-r1.log` |
| Unity action category run 2, fresh process | Pass: 5 passed, 0 failed, 1 graphics-only skip | `qa-actions-r2-results.xml`, `qa-actions-r2.log` |
| Full Unity Play Mode `ToilRelic.PlayModeTests` | Pass: 103 passed, 0 failed, 2 conditional graphics skips out of 105 | `qa-playmode-results.xml`, `qa-playmode.log` |
| Graphics-enabled capture filter | Pass: 1/1 | `qa-capture-results.xml`, `qa-capture.log` |
| PNG validity | Pass: 12/12; exact 1280x720 or 800x600 dimensions, non-zero bytes, non-uniform RGB pixels | `qa-layout-evidence/` |

The full console and Unity suites include both native readers for the shared content, migration, and pure command vectors. This keeps the mirrored domain contract covered without adding a parallel runtime.

## Manual and production-shaped scenarios

| Scenario | Expected | Observed result |
|---|---|---|
| Present Contract -> Cancel -> delayed confirm from captured snapshot | Remain in Camp with no encounter, mutation, gameplay reward, or save | Pass in the focused runtime regression and full Play Mode suite. |
| Serialized `Back to CampButton` through the live `EventSystem` | Contract closes and focus returns to `Hunt ContractButton` | Pass in both fresh action-category runs. |
| Third distinct victory at the Ready boundary | Visible status retains Win, profile reward, `Rustheart Core`, Ready, and level-up facts; save status remains last | Pass in both fresh action-category runs and full Play Mode. |
| Hunt open -> second quarry -> victory -> replay -> Cancel -> Ready -> Forge -> Keep -> Equip -> reload | Selected quarry remains authoritative; project and relic progress once; Forge does not auto-equip; later Equip persists | Pass through real serialized actions. |
| Invalid Contract content and forced pre-write save failure | No invalid gameplay mutation; failure feedback remains visible and later successful save can persist in-memory progress | Pass in action and full Play Mode tests. Captures show the complete messages without HUD overlap. |
| Six named states at 1280x720 and 800x600 | No clipping or overlap; optional reward, guaranteed contribution, replay, Ready, forged, invalid, failure, and relic-preview facts remain readable | Partial fail: 10 images pass visual inspection. In both `hunt-contract-open` images, the `Rust Golem` row truncates `Rustheart Core`. |

## Findings

### Q1 - P2 - Long quarry row clips the guaranteed contribution

- Location: `unity/Assets/Scripts/UI/HuntContractPanelController.cs:170`
- Evidence: `qa-layout-evidence/hunt-contract-open-1280x720.png` and `qa-layout-evidence/hunt-contract-open-800x600.png`.
- Observed: `35% Rustguard Plate - Guaranteed Rustheart Core` wraps beyond the fixed 52px two-line row. Unity truncates the third line, so only `Guaranteed` remains visible.
- Impact: the overview hides which guaranteed contribution belongs to the medium quarry. This violates U7's production-long-label and no-clipping acceptance contract, even though selecting the row shows the full detail text.
- Required response: make the worst-case row label fit at the 800px virtual floor without losing the contribution name. Add a glyph/text-bounds regression and recapture both viewports.

### Q2 - P3 - Focused QA command selects zero tests

- Location: `unity/UNITY_SETUP.md:69`
- Evidence: the documented category `PurposefulHuntActions` produced a successful Unity run with `total=0`; the source category is `PurposefulHuntActionContracts`.
- Impact: a developer following the setup guide can incorrectly treat an empty run as focused-action coverage.
- Required response: update the documented category and keep the exact non-zero result expectation beside the command.

## Known limits

- Graphics capture ran in Unity batch mode with graphics enabled. It exercised live scene controls and rendering, but it was not a free-form human play session.
- Unity logged access-token refresh warnings, but licensing remained sufficient and all invoked test processes exited successfully.
- Atomic replacement and prior-file durability during a true mid-write crash remain deferred to the planned Ironclad Save Envelope. This slice proves pre-write failure and later recovery only.
- QA evidence files remain local under this task directory and are not staged with the workflow markdown commit.

## Disposition

Return to `work`. Complete RW10-RW11, rerun the focused visual/action checks, then repeat review before QA.
