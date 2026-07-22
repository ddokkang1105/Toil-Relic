# QA

## Automated checks

| Command | Result | Evidence |
|---|---|---|

## Manual scenarios

| Scenario | Expected | Result |
|---|---|---|

## Known limits
# QA: Main Menu Save Entry Flow

## Automated validation

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe" `
  -batchmode -nographics `
  -projectPath "C:\Toil-Relic-main\unity" `
  -runTests -testPlatform PlayMode `
  -assemblyNames "ToilRelic.PlayModeTests" `
  -testResults "C:\Toil-Relic-main\.flow\tasks\main-menu-save-entry-flow\main-menu-flow-results.xml" `
  -logFile "C:\Toil-Relic-main\.flow\tasks\main-menu-save-entry-flow\main-menu-flow.log"
```

Result: **7 passed, 0 failed** in Unity 6000.3.19f1.

Covered scenarios:

- SampleScene contains and wires the game, UI action, and data objects.
- Existing camp and battle action bindings remain available.
- Initial entry is `Title`, with the title panel active.
- Continue button enabled state matches the successfully loaded-save state.
- Continue and New Game button bindings are present.

## Known limits

The test avoids calling New Game because it intentionally deletes the real persistent save. Visual typography/layout and the desktop Quit action require a manual Unity Editor/standalone-player pass.
