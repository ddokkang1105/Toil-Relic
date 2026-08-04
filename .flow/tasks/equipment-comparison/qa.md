# QA

## Result

pass

The reviewed equipment-comparison snapshot passed repository-appropriate console and Unity QA with no new defects. No product source, test, scene, commit, or Git index entry was changed during QA.

## Environment and Routing

- Date: 2026-08-04
- Snapshot: a6cfbddb115cb523ee299d9d64cf04ec0a0dfa48
- Console runtime: .NET SDK 10.0.302, target net8.0
- Unity Editor: 6000.3.19f1
- Personal Flow probing found OpenSpec ready without project artifacts, gstack and Compound Engineering installed, and OMX unavailable.
- The active gstack QA skill targets browser-based web applications and requires a clean tree for atomic fixes. This repository is a console and Unity game, and the main worktree contains user-owned uncommitted work. QA therefore used Personal Flow's repository-appropriate fallback in a clean detached worktree at the reviewed snapshot. The temporary worktree was removed after evidence collection.

## Automated Checks

| Check | Command | Result | Evidence |
|---|---|---|---|
| Console build | `dotnet build src/ToilRelic/ToilRelic.csproj --nologo` | Pass: 0 warnings, 0 errors | Command output |
| Focused console equipment flow | `dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --nologo --filter GameEquipmentUxTests --logger "trx;LogFileName=qa-equipment-ux.trx" --results-directory C:\Toil-Relic-main\.flow\tasks\equipment-comparison` | Pass: 8/8 | `qa-equipment-ux.trx` |
| Full console regression | `dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --nologo --logger "trx;LogFileName=qa-console-full.trx" --results-directory C:\Toil-Relic-main\.flow\tasks\equipment-comparison` | Pass: 51/51 | `qa-console-full.trx` |
| Task diff hygiene | `git diff --check 8cfbd07e4643006e59e01e772849cd6ca0e01bc5 a6cfbddb115cb523ee299d9d64cf04ec0a0dfa48` | Pass | Command output |
| Unity scene regeneration | `C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe -batchmode -nographics -projectPath C:\Users\User\AppData\Local\Temp\equipment-comparison-qa-4d808af120ed4b76891ee56fee8ddab5\unity -runTests -testPlatform EditMode -assemblyNames ToilRelic.EditModeTests -testResults C:\Toil-Relic-main\.flow\tasks\equipment-comparison\qa-editmode-results.xml -logFile C:\Toil-Relic-main\.flow\tasks\equipment-comparison\qa-editmode.log` | Pass: 1/1 | `qa-editmode-results.xml` |
| Unity action stability run 1 | `C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe -batchmode -nographics -projectPath C:\Users\User\AppData\Local\Temp\equipment-comparison-qa-4d808af120ed4b76891ee56fee8ddab5\unity -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testCategory PlayModeActionContracts -testResults C:\Toil-Relic-main\.flow\tasks\equipment-comparison\qa-action-r1-results.xml -logFile C:\Toil-Relic-main\.flow\tasks\equipment-comparison\qa-action-r1.log` | Pass: 23/23 | `qa-action-r1-results.xml` |
| Unity action stability run 2 | `C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe -batchmode -nographics -projectPath C:\Users\User\AppData\Local\Temp\equipment-comparison-qa-4d808af120ed4b76891ee56fee8ddab5\unity -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testCategory PlayModeActionContracts -testResults C:\Toil-Relic-main\.flow\tasks\equipment-comparison\qa-action-r2-results.xml -logFile C:\Toil-Relic-main\.flow\tasks\equipment-comparison\qa-action-r2.log` | Pass: 23/23 | `qa-action-r2-results.xml` |
| Full Unity regression | `C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe -batchmode -nographics -projectPath C:\Users\User\AppData\Local\Temp\equipment-comparison-qa-4d808af120ed4b76891ee56fee8ddab5\unity -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testResults C:\Toil-Relic-main\.flow\tasks\equipment-comparison\qa-unity-full-results.xml -logFile C:\Toil-Relic-main\.flow\tasks\equipment-comparison\qa-unity-full.log` | Pass: 60 passed, 0 failed, 1 intentional capture skip | `qa-unity-full-results.xml` |
| Graphics capture | Set `TOIL_RELIC_LAYOUT_EVIDENCE_DIR=C:\Toil-Relic-main\.flow\tasks\equipment-comparison\qa-layout-evidence`, then run the filtered `P0_CaptureLayoutEvidenceWhenRequested` PlayMode test without `-nographics` | Pass: 1/1, 20 PNGs | `qa-layout-results.xml`, `qa-layout-evidence/` |
| PNG validity | Load every PNG with `System.Drawing.Bitmap`; verify expected viewport, non-zero bytes, and more than one sampled pixel color | Pass: 20/20 valid viewport, non-empty, and non-uniform | Command output |
| Unity fatal-pattern scan | Search all new Unity logs for `error CS`, `Compilation failed`, and `Test run failed` | Pass: 0 hits | `qa-editmode.log`, `qa-unity-full.log`, `qa-layout.log` |

The full PlayMode skip is `P0_CaptureLayoutEvidenceWhenRequested`. It is opt-in by design and passed separately in the graphics-enabled run.

## User Scenarios

| Scenario | Expected | Observed result |
|---|---|---|
| Console slot has no compatible candidate | Explain the empty state, keep Equip visible but disabled, accept only Back, and do not mutate or save | Pass. The focused integration suite verified the label, `Select (0-0)`, absent equip/unequip success, and byte-identical save data. |
| Console preview, confirm, cancel, equip, unequip, and persistence failure | Preview and cancel remain read-only; successful commands persist; save failure keeps the in-memory result and warning | Pass in the 8 focused equipment-flow tests and 51-test regression. |
| Unity opens Equipment from Camp and uses real pointer controls | Open, select slot and candidate, preview, confirm, unequip, and return focus without leaving the screen | Pass in full PlayMode and both Action Contract runs. |
| Invalid, same-item, stale, empty, primary-slot, and save-failure paths | Disable or reject the command with typed state, no unexpected events or save, and preserve authoritative failure UI | Pass in full PlayMode and both Action Contract runs. |
| All 12 slots and non-pointer navigation | Deterministic order; final Earring 2 row remains visible; reopening resets scroll | Pass. The final-slot capture shows Earring 2 fully visible at both viewports. |
| Empty equipment panel | No-candidate message and aggregate totals remain legible; Back remains available | Pass at 1280x720 and 800x600. |
| Long candidate name | Candidate and comparison text wrap without clipping or panel overlap | Pass at both viewports. |
| Same-item with no item stats | Show `No equipment stat change (+/-0)` and keep the layout stable | Pass at both viewports. |
| Successful equip and failed save | Refresh HUD and comparison in place; preserve success or failure status outside the panel | Pass at both viewports. |
| Title, Camp, and Battle adjacent screens | HUD, status messages, and actions remain readable and separate | Pass at both viewports. |

## Visual Inspection

All 20 newly generated PNGs were opened and inspected. The 1280x720 and 800x600 captures show:

- no equipment-panel overlap with the HUD or status region;
- no clipped fixed buttons or runtime-generated rows;
- readable empty, long-name, success, and save-failure states;
- visible signed deltas and the full same-item `+/-0` fallback;
- Earring 2 fully visible in the final slot row;
- stable Title, Camp, and Battle layouts adjacent to the feature.

## Findings

- Critical: 0
- High: 0
- Medium: 0
- Low: 0
- Fixes applied during QA: 0

## Known Limits

- The focused console no-candidate test scripts disabled choice 1 twice. Disabled Unequip choice 2 is protected by the same `ReadInt(0, 0)` boundary and was covered statically, but does not have a distinct scripted input.
- Unity interaction was exercised through EditMode/PlayMode automation and rendered captures, not a human-operated standalone build.
- The browser health-score rubric from gstack QA is not applicable to this console/Unity application.
- Unity import reported line-ending-only working-tree marks on six files inside the disposable worktree. `git diff` contained no content changes, nothing was copied back, and the temporary worktree was removed.

## Integrity

- Original Git index hash before and after QA: `769483aaf1d5390e5210d2f27af3bd45bd0b3cb3`.
- The detached QA worktree was clean before execution and contained only transient Unity import marks afterward.
- No commit, push, branch change, source fix, or tracked test change was made.

## Workflow Decision

QA passes. Advance the Personal Flow task to `close` for reusable learning capture and final follow-up disposition.
