# QA: Unity Play Mode P0

**Status:** Passed.

## Command

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe" `
  -batchmode -nographics `
  -projectPath "C:\Toil-Relic-main\unity" `
  -runTests -testPlatform PlayMode `
  -assemblyNames "ToilRelic.PlayModeTests" `
  -testResults "C:\Toil-Relic-main\.flow\tasks\unity-p0-play-mode-qa\results\playmode-results.xml" `
  -logFile "C:\Toil-Relic-main\.flow\tasks\unity-p0-play-mode-qa\results\playmode-unity.log"
```

`-quit` is intentionally omitted: `-runTests` owns batch-mode termination, while `-quit` can end the editor before the Test Runner starts.

## Automated result

- Unity editor: `6000.3.19f1`.
- Result: **5 passed, 0 failed**, duration `1.476s`.
- Evidence: `results/playmode-results.xml` and `results/playmode-unity.log`.

| P0 check | Result |
| --- | --- |
| Scene contains GameManager | Passed |
| GameManager references EnemyDatabase and DropTable | Passed |
| Scene contains UIActions/GameActionBridge | Passed |
| GameActionBridge references GameManager | Passed |
| Hunt, Rest, Craft, Attack, Defend, Flee button bindings are present | Passed |

## Validation notes

- `SampleScene` was configured with `EnemyDatabase_Main`, three enemy assets, and `DropTable_Default`.
- The EventSystem uses `InputSystemUIInputModule`, matching the project’s Input System-only setting.
- This automated suite verifies runtime scene wiring and persistent UI bindings. Visual layout and balance still need a human Play Mode pass if they become acceptance criteria.
