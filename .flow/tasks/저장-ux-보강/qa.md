# QA

## Scope

- Source head under test: `4c66d70` (`Approve final save contract re-review`)
- Runtime targets: .NET 8 console and Unity `6000.3.19f1`
- Profile: `standard`
- Result: `passed`
- Routing: Personal Flow used repo-appropriate validation. The installed gstack `qa` skill targets web applications, so it was not invoked for this console/Unity project.

## Automated checks

| Command | Result | Evidence |
|---|---|---|
| `dotnet build src/ToilRelic/ToilRelic.csproj --no-restore` | Passed: 0 warnings, 0 errors | Console project compiled at the QA source head |
| `dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --no-restore --logger "trx;LogFileName=qa-console-tests.trx" --results-directory ".flow/tasks/저장-ux-보강"` | Passed: 40/40, 0 failed, 0 skipped | `qa-console-tests.trx` |
| `Unity.exe -batchmode -nographics -projectPath C:\Toil-Relic-main\unity -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testResults ...\qa-unity-full-results.xml -logFile ...\qa-unity-full.log` | Passed: 35, failed: 0, skipped: 1 optional capture test | `qa-unity-full-results.xml`; the capture test is intentionally skipped without its environment variable |
| `$env:TOIL_RELIC_LAYOUT_EVIDENCE_DIR=...\qa-layout-evidence; Unity.exe -batchmode -projectPath C:\Toil-Relic-main\unity -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testFilter ToilRelic.PlayModeTests.SampleSceneP0PlayModeTests.P0_CaptureLayoutEvidenceWhenRequested -testResults ...\qa-unity-layout-results.xml -logFile ...\qa-unity-layout.log` | Passed: 1/1; generated 8 rendered PNGs | `qa-unity-layout-results.xml`, `qa-layout-evidence/` |
| `git diff --check` and tracked-source inspection after QA | Passed; no source or generated-scene drift | Only QA result files, screenshots, and this workflow state changed |

The first layout-capture attempt used `-nographics`. The test passed but direct image inspection showed uniform gray frames, so that output was rejected as invalid visual evidence and overwritten by the graphics-enabled batch run above.

## Acceptance and runtime scenarios

| Contract | Scenario and observed result | Evidence | Result |
|---|---|---|---|
| AE1 / R1-R2 | Missing save shows `Start a new game to begin.` and leaves Continue unavailable in both runtimes | Console `Run_MissingSave_ShowsDiagnosisAndSavesBeforeQuit`; Unity `P0_InitialEntryShowsTitleAndMatchesContinueAvailability`; Title screenshots | Passed |
| AE2 / R1-R2 | Valid current saves enable Continue and hydrate representative player values | Console `Run_ValidSave_OffersContinueAndUsesLoadedPlayer`; Unity `P0_ValidSaveEnablesContinueAndLoadsCurrentFormat` | Passed |
| AE3 / R1-R3 | Malformed, structurally invalid, unsupported-version, partial, and threshold-invalid saves remain unchanged; Continue stays unavailable and replacement failure returns to the unreadable Title state | Console unreadable/threshold/replacement tests; Unity unreadable/partial/invalid/version/replacement PlayMode tests | Passed |
| Compatibility rework | Console rejects Level 1 / Experience 20 before normalization. Unity authentic score-based version 0 loads as Level 1 / Experience 0, enables Continue, enters Camp, and keeps original bytes. Version 1 and current version 2 compatibility tests remain green | `qa-console-tests.trx`; `P0_AuthenticVersionlessScoreSaveLoadsAndNormalizes`, `P0_VersionOneSaveWithoutEquipmentFieldsLoadsAndNormalizes`, current-format tests | Passed |
| AE4 / R4-R5/R8-R9 | Camp action text remains primary and the auxiliary row moves directly to `Save: Saved just now` | `P0_CampSaveSuccessPreservesActionMessage`; Camp-success screenshots | Passed |
| AE5 / R5-R6 | Save failure preserves action or terminal outcome, adds the safe progress-loss warning, shows `Save: Failed`, and leaves play available | Rest/equip/terminal failure PlayMode tests; Camp-failure and Battle-failure screenshots | Passed |
| AE6 / R7 | A later successful progression save clears only the prior failure warning and shows the success row | `P0_SaveFeedbackPersistsFailureAndClearsContextualSuccess`; console failure-to-recovery test | Passed |
| AE7 / R9/R13-R14 | At 800x600 and 1280x720, Title hides the save row, Camp shows readable success/failure rows, Battle hides the row but keeps the warning, and the active menu/battle panel stays below the status region | 8 PNGs in `qa-layout-evidence/`; geometry PlayMode tests | Passed |
| AE8 / R10-R12 | Console and Unity use the approved short English copy, keep raw diagnostics out of player text, and console Quit still saves | Console public-flow tests and Unity status/title tests | Passed |

## Visual inspection

Direct inspection covered:

- Title at 1280x720 and 800x600: diagnosis readable, save row hidden, menu unobstructed.
- Camp success at both sizes: gameplay result remains above `Save: Saved just now`; no menu overlap.
- Camp failure at both sizes: action result, progress-loss warning, and `Save: Failed` remain readable; no menu overlap.
- Battle failure at both sizes: auxiliary row hidden, warning retained in primary text, battle panel fully below the status block.

## Known limits

- QA ran in the Unity Editor PlayMode runner, not a packaged standalone build or physical-device build.
- Tests intentionally use isolated temporary save paths and do not read or mutate the user's real persistent save.
- Direct cross-product coverage is still absent for modern-core version 0, score-only version 1/2 rejection, and per-field historical version-0 omissions. Review validation classified these as non-blocking testing gaps because the version predicate and existing positive/negative coverage prove the current contract.
- Present-but-wrong-kind Unity `JsonUtility` behavior remains engine-specific and is not covered beyond the existing deserialization boundary.
- Unity's acceptance of experience at or above the current-level threshold remains an explicitly scoped pre-existing runtime difference.

## Outcome

All R1-R14 requirements and AE1-AE8 acceptance examples have passing automated or visual evidence. No QA blocker was found; the task is ready for `close`.
