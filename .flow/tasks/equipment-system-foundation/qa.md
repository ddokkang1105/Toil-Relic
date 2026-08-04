# QA

## Result — 2026-07-28

`pass — proceed to close`

과거 P0 차단 원인이었던 실행 환경이 해소됐다. .NET SDK `10.0.302`와 프로젝트 버전에 맞는 Unity Editor `6000.3.19f1`에서 콘솔·Unity 런타임 검증을 완료했다.

## Framework routing

- `probe-frameworks.ps1`는 OpenSpec, gstack, Compound Engineering을 탐지했고 OMX는 탐지하지 못했다.
- gstack `qa`는 활성 상태지만 웹 브라우저 애플리케이션용 QA이므로 Unity 게임의 P0 시나리오에는 적용하지 않았다.
- Personal Flow의 저장소 적합 대체 경로로 .NET 빌드·격리 콘솔 실행·Unity Test Framework·렌더링 캡처·정적 계약 대조를 사용했다.
- 탐지 명령:
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\Users\User\.codex\skills\personal-flow\scripts\probe-frameworks.ps1 -ProjectRoot C:\Toil-Relic-main`

## Automated checks

| Command | Result | Evidence |
|---|---|---|
| `dotnet build C:\Toil-Relic-main\src\ToilRelic\ToilRelic.csproj --nologo` | Pass | 0 warnings, 0 errors. |
| 새 OS 임시 디렉터리에서 `@('1','5','0','1','','1','','5','1','2','','6') \| dotnet C:\Toil-Relic-main\src\ToilRelic\bin\Debug\net8.0\ToilRelic.dll` | Pass | Starter Weapon 시작, 첫 승리 Reward Weapon 지급, 두 번째 승리 중복 없음, Reward Weapon 장착 및 저장 확인. |
| 같은 격리 저장에서 `@('1','6') \| dotnet C:\Toil-Relic-main\src\ToilRelic\bin\Debug\net8.0\ToilRelic.dll` | Pass | Continue 후 `Reward Weapon +2`가 복원됨. |
| 장비 필드가 없는 격리 레거시 저장에서 `@('1','2','','6') \| dotnet C:\Toil-Relic-main\src\ToilRelic\bin\Debug\net8.0\ToilRelic.dll` | Pass | Starter Weapon 정규화, 인벤토리 보존, 정규화 상태 재저장 확인. |
| `Unity.exe -batchmode -nographics -projectPath C:\Toil-Relic-main\unity -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testResults C:\Toil-Relic-main\.flow\tasks\equipment-system-foundation\qa-unity-full-results.xml -logFile C:\Toil-Relic-main\.flow\tasks\equipment-system-foundation\qa-unity-full.log` | Pass | 42 passed, 0 failed, 1 expected opt-in capture skip. |
| `$env:TOIL_RELIC_LAYOUT_EVIDENCE_DIR='C:\Toil-Relic-main\.flow\tasks\equipment-system-foundation\qa-layout-evidence'; Unity.exe -batchmode -projectPath C:\Toil-Relic-main\unity -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testFilter ToilRelic.PlayModeTests.SampleSceneP0PlayModeTests.P0_CaptureLayoutEvidenceWhenRequested -testResults C:\Toil-Relic-main\.flow\tasks\equipment-system-foundation\qa-unity-layout-results.xml -logFile C:\Toil-Relic-main\.flow\tasks\equipment-system-foundation\qa-unity-layout.log` | Pass | 1 passed, 0 failed, 0 skipped; 8 PNG 생성. |
| 콘솔/Unity 장비 계약 PowerShell assertion | Pass | 두 카탈로그의 `starter-weapon`, `reward-weapon`, `AttackBonus +2`, 중복 방지, 첫 승리 지급, 공격 보너스 단일 적용, 직렬화 필드, Unity bridge 호출점이 일치함. |
| `git diff --check` | Pass | 공백 오류 없음. |

## Scenario results

| Scenario | Observed result |
|---|---|
| Console: new player | Starter Weapon만 보유·장착된 상태로 시작하고 장비 목록과 상태 줄에 표시됨. |
| Console: first and repeated victories | 첫 승리에서만 Reward Weapon을 지급하고 두 번째 승리에는 획득 로그와 소유 ID가 중복되지 않음. |
| Console: equip, save, reload | Reward Weapon을 PrimaryWeapon에 장착한 뒤 저장 JSON에 소유·장착 상태가 기록되고 재시작 후 `+2` 보너스까지 복원됨. |
| Console: pre-equipment save | 장비 필드가 없는 저장을 Starter Weapon 보유·장착으로 정규화하고 Junk 2, RelicPart 1, Treasure 4, HealingPotion 1을 그대로 유지함. |
| Unity: current and historical saves | 현재 저장 왕복, 장비 필드 없는 version 2, version 1, versionless historical 저장 테스트가 통과함. 원본 historical bytes는 자동 덮어쓰지 않음. |
| Unity: new game and equipment persistence | 비기본 Reward Weapon 장착 상태를 저장·재로드한 뒤 New Game 버튼으로 Starter Weapon 초기 상태를 복원하는 계약이 통과함. |
| Unity: HUD and action integration | 장비 HUD가 `Wpn Starter Weapon \| ATK +0 \| DEF +0`를 표시하고 상태 갱신·장비 저장 실패 메시지 계약이 통과함. |
| Unity: gameplay regressions | 전투 승리/패배/도주, 공격, 제작 성공/실패, 물약 사용/가드, 휴식, 저장 성공/실패 테스트가 모두 통과함. |
| Unity: visual inspection | 1280×720과 800×600 Camp 캡처에서 장비 텍스트가 읽을 수 있고 상태 메시지·중앙 메뉴와 겹치지 않음. |

## Acceptance coverage

- 콘솔·Unity 장비 타입/ID/능력치 규칙 일치: Pass.
- 보유·장착 상태 변경 및 승인된 슬롯 불변식: Pass. `PrimaryWeapon`은 후속 승인 규칙상 항상 필요하며 선택 슬롯만 해제 가능하다.
- 저장 후 보유·장착 복원과 레거시 정규화: Pass.
- 전투·인벤토리·제작·저장 회귀: Pass.
- 후속 비교 기능용 소유 ID, 장착 엔트리, 장비 정의 및 집계 능력치 조회: Pass.

## Known limits

- Unity 검증은 Editor Play Mode 기준이며 패키징된 standalone 빌드나 물리 장치 빌드는 실행하지 않았다.
- 현재 SampleScene에는 전용 장비 선택/비교 패널이 없다. HUD 표시와 `GameActionBridge`/`GameManager` 호출점까지만 본 태스크 범위이며, 실제 장비 진입점은 `equipment-comparison` 후속 태스크에서 다룬다.
- 후속 슬롯 재정립 태스크가 PrimaryWeapon 상시 장착 규칙을 승인했으므로, 초기 계획의 단일 무기 해제 시나리오는 현재 제품 규칙으로 대체됐다.

## Closure status

- 과거 런타임 P0 후속조치는 해소됐다.
- 미해결 P0/P1 QA 결함은 없다.
- 태스크는 `close` 단계로 진행할 수 있다.
