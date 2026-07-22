# QA

## Result

`pass`

Unity에 맞춘 직접 QA로 장면 재생성, 전체 Play Mode 계약, 전용 렌더 캡처, 해상도별 시각 확인, 콘솔 프로젝트 빌드를 독립적으로 재실행했다. 이번 태스크 범위인 Title/Camp 중앙 메뉴와 상단 HUD·상태 메시지의 겹침은 재현되지 않았다.

`gstack-qa`는 웹 브라우저와 깨끗한 작업 트리를 전제로 하므로, 진행 중인 Unity Personal Flow 작업에는 `fallback_direct`를 적용했다. 브라우저 페이지 대신 Unity가 실제로 렌더한 RenderTexture PNG를 사용자 관점 증거로 사용했다.

## Automated checks

| Command | Result | Evidence |
|---|---|---|
| `Unity.exe -batchmode -nographics -quit -projectPath "C:\Toil-Relic-main\unity" -executeMethod ToilRelic.Unity.Editor.ToilRelicSceneBootstrap.ConfigureSampleScene -logFile "<task>\qa-bootstrap.log"` | 통과, exit 0 | `qa-bootstrap.log`; `SampleScene.unity` 재생성 후 batch mode 정상 종료 |
| `Unity.exe -batchmode -nographics -projectPath "C:\Toil-Relic-main\unity" -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testResults "<task>\qa-playmode-results.xml" -logFile "<task>\qa-playmode.log"` | 통과: 20 passed, 0 failed, 1 skipped | `qa-playmode-results.xml`; skip은 환경 변수가 없을 때 명시적으로 제외되는 opt-in 캡처 테스트 |
| `$env:TOIL_RELIC_LAYOUT_EVIDENCE_DIR="<task>\qa-evidence"; Unity.exe -batchmode -projectPath "C:\Toil-Relic-main\unity" -runTests -testPlatform PlayMode -assemblyNames ToilRelic.PlayModeTests -testFilter "ToilRelic.PlayModeTests.SampleSceneP0PlayModeTests.P0_CaptureLayoutEvidenceWhenRequested" -testResults "<task>\qa-capture-results.xml" -logFile "<task>\qa-capture.log"` | 통과: 1 passed, 0 failed, 0 skipped | `qa-capture-results.xml`; 6개 PNG 생성 및 비어 있지 않은 파일 검증 |
| `dotnet build` in `C:\Toil-Relic-main\src\ToilRelic` | 통과: 0 warnings, 0 errors | 콘솔 구현 기준선 유지 |
| `System.Drawing.Image.FromFile(...)`로 `qa-evidence\*.png` 크기 확인 | 통과 | 1280×720 4개, 800×600 2개가 파일명 계약과 일치 |

## Manual scenarios

| Scenario | Expected | Result |
|---|---|---|
| Title, 1280×720 | 좌측 HUD와 우측 상태 메시지가 중앙 3버튼 메뉴를 침범하지 않음 | 통과: `qa-evidence/title-1280x720.png` |
| Title, 800×600 | 메뉴가 중앙에 유지되고 상단 두 정보 영역과 분리됨 | 통과: `qa-evidence/title-800x600.png` |
| Camp, 1280×720 | Hunt/Rest/Craft Treasure가 읽기 쉽고 HUD·상태와 겹치지 않음 | 통과: `qa-evidence/camp-1280x720.png` |
| Camp, 800×600 | 4:3에서도 버튼 크기·간격과 상단 여백이 유지됨 | 통과: `qa-evidence/camp-800x600.png` |
| 생산 형식 상태 메시지 스트레스, 1280×720 | 승리 보상, 레벨업, 저장 실패 3개 메시지가 잘리지 않고 중앙 메뉴를 침범하지 않음 | 통과: `qa-evidence/status-stress-1280x720.png`; 승리 문구는 2줄로 감싸지고 나머지 메시지도 모두 표시됨 |
| Battle 회귀, 1280×720 | 기존 전투 정보와 Attack/Defend/Flee/Potion 조작이 유지됨 | 통과(범위 내): `qa-evidence/battle-regression-1280x720.png`; 전투 조작은 모두 사용 가능한 상태 |

## Known limits

- BattlePanel의 우측 상단 상태/전투 정보 겹침은 기존 문제이며 이번 Title/Camp 3버튼 패널 조정 범위 밖이다. `followups.md`에 별도 Medium 후속 작업으로 유지한다.
- 캡처 테스트는 일반 Play Mode 실행에서는 의도적으로 skip된다. 레이아웃 증거가 필요할 때 `TOIL_RELIC_LAYOUT_EVIDENCE_DIR`를 설정한 전용 명령을 함께 실행해야 한다.
- Unity 로그에 로컬 LicenseClient handshake/access-token 경고와 D3D12 info-queue 경고가 있었지만, 두 테스트 실행 모두 exit 0으로 완료됐고 XML 결과 및 PNG 생성에는 영향을 주지 않았다.
- Unity Editor에서 사람이 직접 마우스로 전체 게임 루프를 조작하는 세션은 이번 자동 batch QA에 포함하지 않았다. 버튼별 persistent action 매핑과 Battle 조작 계약은 Play Mode 테스트가 검증한다.
