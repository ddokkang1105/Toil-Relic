# QA

## Result

`passed`

Every acceptance criterion is covered by production-path console or Unity tests, exact cross-runtime boundary vectors, and rendered recovery-state evidence. No unresolved review or QA finding remains.

## Environment

- Branch and QA head: `codex/ironclad-save-envelope` at `06d7a42`.
- Console: .NET 8 project under `src/ToilRelic`.
- Unity: `6000.3.19f1`, PlayMode assembly `ToilRelic.PlayModeTests`.
- Unity category repetitions ran in separate hidden processes without `-quit`; result XML and logs are stored under this task directory.

## Commands executed

```powershell
dotnet test tests/ToilRelic.Tests/ToilRelic.Tests.csproj --no-restore --logger "trx;LogFileName=qa-console-full.trx" --results-directory .flow/tasks/ironclad-save-envelope
dotnet build src/ToilRelic/ToilRelic.csproj --no-restore
```

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -testCategory SaveEnvelopeContracts -testResults '.flow\tasks\ironclad-save-envelope\qa-unity-envelope-r1-results.xml' -logFile '.flow\tasks\ironclad-save-envelope\qa-unity-envelope-r1.log'
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -testCategory SaveEnvelopeContracts -testResults '.flow\tasks\ironclad-save-envelope\qa-unity-envelope-r2-results.xml' -logFile '.flow\tasks\ironclad-save-envelope\qa-unity-envelope-r2.log'
& 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Toil-Relic-main\unity' -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testResults '.flow\tasks\ironclad-save-envelope\qa-unity-playmode-full-results.xml' -logFile '.flow\tasks\ironclad-save-envelope\qa-unity-playmode-full.log'
```

## Automated checks

| Gate | Result | Evidence |
|---|---|---|
| Console full regression | Pass: 152/152 | `qa-console-full.trx` |
| Console compile | Pass: 0 warnings, 0 errors | Final build command output |
| Unity SaveEnvelopeContracts run 1 | Pass: 66/66 | `qa-unity-envelope-r1-results.xml`, `.log` |
| Unity SaveEnvelopeContracts run 2 | Pass: 66/66 | `qa-unity-envelope-r2-results.xml`, `.log` |
| Full Unity PlayMode | Pass: 176 passed, 0 failed, 3 conditional graphics skips out of 179 | `qa-unity-playmode-full-results.xml`, `.log` |
| Shared vector parity | Pass: console 33 IDs, Unity 33 IDs, no difference | Raw TRX/XML ID comparison |
| Unity error scan | Pass: 0 compiler, unhandled exception, serialization, or sentinel-leak lines | Three final Unity logs |
| Scene/Prefab integrity | Pass: 0 changed files from base | `git diff --name-only 206b4e3 -- unity/Assets/Scenes unity/Assets/**/*.prefab` |
| Patch hygiene | Pass | `git diff --check` |

The three skipped full-suite tests are opt-in graphics capture fixtures. Their required Ironclad evidence was run earlier with graphics enabled and produced four current screenshots.

## Production-shaped scenarios

| Scenario | Expected | Observed result |
|---|---|---|
| Interrupted stage/replace/recovery boundary | Current call fails conservatively; a fresh Load chooses only a complete validated authority. | Pass in both shared runners, including missing-live before/after promotion. |
| Invalid live plus valid LKG | Damaged live is quarantined, exact LKG bytes are promoted through stage, and Load returns Recovered with pending notice. | Pass in both runtimes. |
| Recovery notice lifecycle | Notice survives failed progress saves and clears after the first successful authoritative progress save. | Pass in console UX and Unity production-scene tests. |
| Failed New Game before data edge | At least one validated old payload remains recoverable and UI does not publish a false clear. | Pass in both shared New Game runners. |
| Failed New Game after data edge | No stale Continue authority remains; retry completes full invalidation. | Pass in Unity Title production path and both shared runners. |
| Title/Camp worst-case feedback at 800x600 and 1280x720 | Recovery and save-failure copy remain visible without clipping or covering primary actions. | Pass: four screenshots directly inspected; geometry and interactability assertions passed. |

## Visual evidence

- `work-u4-layout-evidence/recovery-title-failure-800x600.png`
- `work-u4-layout-evidence/recovery-title-failure-1280x720.png`
- `work-u4-layout-evidence/recovery-camp-failure-levelup-800x600.png`
- `work-u4-layout-evidence/recovery-camp-failure-levelup-1280x720.png`

## Known limits

- The durability guarantee remains limited to local-volume .NET 8 Windows console and Unity 6 Windows Editor PlayMode capability. A Unity Standalone development-player interruption/cold-restart harness is a separate follow-up.
- Deterministic checkpoints prove orchestration at modeled before/after boundaries; they do not simulate physical device loss after the operating system reports a flush or replace as successful.
- The first attempted final console test/build pair was launched concurrently and one command hit `CS2012` on the shared `obj` output. It was excluded and replaced by clean serial executions: 152/152 tests and a 0-warning/0-error build.
- Full PlayMode graphics-capture fixtures skip when their evidence environment variables are absent; the required four Ironclad images were already captured in graphics-enabled runs and directly inspected during this QA.

## Post-Deploy Monitoring & Validation

There is no centralized service deployment or telemetry for this local game. For the first release containing this change:

- Search Unity logs for `Save load failed`, `Save delete failed`, `operation=`, and unexpected repeated recovery notices.
- Healthy signal: normal saves keep Continue available, recovered saves show the exact notice once, and a later successful progress save clears it.
- Failure signal: repeated recovery on every launch, an unreadable save after a reported successful write, stale Continue after New Game, or diagnostics containing absolute paths/player payloads.
- Mitigation trigger: preserve the complete five-file envelope for diagnosis and roll back the release if any validated old payload becomes unrecoverable through a supported flow.
- Validation window and owner: first two local launch/save/restart cycles after packaging; release owner.
