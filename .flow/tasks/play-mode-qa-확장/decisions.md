# Decisions

## Confirmed

- 범위 등급은 `standard`이며, 하나의 일관된 결과인 “Unity Play Mode 회귀 탐지력 확장”을 다룬다.
- 요구사항 산출물 형식은 별도 설정이 없어 Markdown을 사용한다.
- 현재 기준선은 `SampleSceneP0PlayModeTests.cs`의 36개 `[UnityTest]`다. 직전 전체 실행은 35개 통과와 선택적 레이아웃 캡처 1개 건너뜀으로 총 36개를 보고했다.
- discovery는 구현 세부 설계보다 실제 회귀 공백, 우선 사용자 흐름, 성공 신호를 먼저 확정한다.
- 사용자 선택 없이 계속 진행된 기본안에 따라 범위는 “대표 사용자 행동 계약 팩”으로 제한한다. 모든 런타임 분기를 소진하기보다 미검증 행동군별로 회귀 가치가 가장 높은 성공·실패 계약을 선택한다.
- 완료 기준은 균형형으로 한다. 신규 대상 테스트는 동일 조건에서 3회 연속 통과하고, 전체 Play Mode 어셈블리는 1회 통과해야 한다. 제품 UI 외형이 바뀌지 않으면 새 스크린샷은 요구하지 않는다.
- 대표 팩은 행동별 독립 계약으로 구성한다. 각 테스트는 실제 UI 입력 하나의 상태·메시지·자원·저장 효과를 짧게 검증해 실패 원인을 다른 행동과 분리한다.
- 사용자 확인을 거친 범위 종합안은 `docs/plans/2026-07-24-001-test-play-mode-qa-expansion-plan.md`의 requirements-only Product Contract로 기록한다.

## Grounding findings

- 기존 검증은 씬·버튼 바인딩, Title/저장 분류, HUD·레이아웃·상태 메시지, 전투 종료 결과에 강하게 집중되어 있다.
- 버튼의 persistent target/method는 검사하지만 실제 `Button.Click()`을 통한 사용자 입력 경로는 실행하지 않는다.
- `UsePotion`, `CraftTreasure`, 보이는 장비 장착 버튼은 바인딩만 확인하고 실제 성공·실패 UI 흐름을 호출하지 않는다. Generic 장비 해제에는 현재 UI 진입점이 없다.
- 비치명 Attack/적 턴, 실패한 Flee, 성공적인 New Game 교체, Battle 로그 수명과 반복 상태 전이는 일부 또는 전부 직접 검증되지 않는다.
- 전체 fixture는 씬 로드 전에 고유 임시 저장 경로를 설치하고 teardown에서 이전 정적 override를 복원·삭제한다. 따라서 과거에 실제 저장 삭제 위험으로 제외했던 성공 New Game 흐름도 현재는 격리 검증할 기반이 있다.
- 테스트 어셈블리는 런타임 참조 없이 reflection을 사용하며, 현재 모든 36개 테스트와 헬퍼가 한 파일에 집중되어 있다.
- 저장소에는 Unity Play Mode 실행을 자동화하는 `.github/workflows`가 없고, 명령과 결과는 태스크 QA 산출물에 기록되어 있다.

## Assumptions

- 사용자가 지목한 최근 회귀 사례는 아직 없으므로, discovery 우선순위는 현재 코드의 미호출 사용자 행동과 기존 QA의 명시적 한계를 대리 근거로 삼는다.
- 현재 maintainer를 주 사용자로 본다. 이 사용자가 얻는 결과는 수동 Play Mode 점검 전에 핵심 행동 회귀가 구체적인 계약 실패로 드러나는 것이다.
- 현재 대안은 기능별 수동 Play Mode 확인이며, 자동화하지 않을 경우 신규 기능 작업 때마다 같은 행동 흐름을 반복 확인하거나 누락 위험을 감수한다.
- Player 빌드·실기기 검증과 제품 기능 변경은 이번 Play Mode 범위 밖에 유지한다.

## Working direction

- 핵심 가치는 테스트 수 자체가 아니라, 현재 바인딩만 확인하는 버튼을 실제 사용자 입력으로 실행해 상태·메시지·자원·저장 결과를 함께 검증하는 것이다.
- 확정 범위는 성공 New Game, Potion, Craft, 보이는 장비 장착 버튼, 실패 Flee, 비치명 Attack/적 턴이다.
- fixture 격리와 기존 graphics-enabled 증적 규칙은 이 흐름을 지지하는 완료 조건으로 유지하되, 별도 테스트 프레임워크 재설계로 범위를 넓히지 않는다.
- 저장 호환성 조합 확대, CI 신설, Player 빌드 QA는 독립 가치가 있어 현재 사용자 행동 흐름과 분리 가능한 후속 후보로 둔다.
## Rejected options

- 하나의 긴 Title→Battle E2E만 추가하는 안: 한 실패가 여러 계약을 가리고 상태 준비가 길어져 원인 진단과 격리성이 떨어진다.
- 모든 미호출 런타임 분기를 이번에 매트릭스로 소진하는 안: 저장 경계와 저가치 입력 조합까지 범위가 커져 대표 회귀 탐지라는 최소 가치보다 carrying cost가 커진다.
- 짧은 사용자 여정 묶음: 상태 간 조합은 잘 보지만 앞선 행동 실패가 뒤 계약을 가려 이번 태스크의 진단성 목표에 불리하다.
- 부작용 유형별 위험 센티널만 두는 안: 유지비는 낮지만 현재 공백인 개별 UI 행동 연결과 고유 결과를 직접 보증하지 못한다.

## Open questions

- 없음.

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
| Framework probe | 완료 | `probe-frameworks.ps1`을 프로세스 한정 `ExecutionPolicy Bypass`로 실행 |
| OpenSpec | 사용 안 함 | CLI는 준비됐지만 프로젝트 `openspec/` 산출물과 활성 `opsx` skill이 없음 |
| CE brainstorm | 완료 | 36개 기준선과 실제 미호출 행동을 조사하고 대표 행동별 독립 계약, 균형형 반복 기준, 명시적 비범위를 Product Contract로 확정 |
| Claim verification | 완료 | 독립 검증에서 저장소 사실 주장 7개 확인, 반박·검증 불가 0개 |
| Ready for Planning | 통과 | Product Contract가 Complete, Consistent, Focused, Usable by planning 네 조건을 충족하고 미결정 질문 없음 |
| gstack design/CEO/office-hours | 생략 | 제품 여정·시각 체계·사업 범위를 바꾸는 태스크가 아님 |
| Engineering review | 현재 생략 | 테스트 어셈블리 의존성이나 실행 아키텍처 변경이 확인되면 discovery에서 다시 활성화 |
| gstack QA | 저장소 부적합 | 활성 `qa` skill은 웹 애플리케이션 대상이므로 Unity Test Runner 기반 동등 검증 사용 예정 |
| OMX | 생략 | CLI 미탐지이며 독립 workstream도 아직 정의되지 않음 |

## Plan-stage blocker (2026-07-24)

- 저장소와 사용자 흐름 분석 결과 `SampleScene`과 `ToilRelicSceneBootstrap`에는 장비 행동 버튼이 없다.
- `GameActionBridge.EquipStarterWeapon`과 `EquipRewardWeapon`은 런타임 브리지 메서드만 존재하므로, 테스트 전용 버튼을 만들거나 메서드를 직접 호출하면 R1/R9/AE5를 충족하지 못한다.
- 실제 장비 컨트롤을 추가하면 제품 UI 범위, Camp 레이아웃, graphics-enabled 스크린샷·레이아웃 QA가 함께 활성화된다.
- 재개 요청에 따라 추천안 A를 적용한다: 실제 장비 UI 진입점이 생길 때까지 R9/AE5를 후속 작업으로 연기하고, 제품 장비 컨트롤이나 bridge/runtime 예외를 이번 범위에 추가하지 않는다.
- 현재 코드로 확정 가능한 비차단 계획 기본값:
  - New Game은 이전 격리 저장 파일을 삭제하고 메모리 상태를 초기화하며 즉시 새 저장 파일을 쓰지는 않는다.
  - Potion, 비치명 Attack, 실패 Flee는 상태 행이 마지막 메시지만 보존하므로 누적 battle log에서 행동과 적 반응의 순서를 검증한다.
  - 신규 행동 팩에 고정 NUnit category를 부여하고 서로 다른 결과 파일을 쓰는 batch 실행을 세 번 수행한 뒤 전체 어셈블리를 한 번 실행한다.

## Plan outcome (2026-07-24)

- Canonical plan: `docs/plans/2026-07-24-001-test-play-mode-qa-expansion-plan.md`
- CE plan readiness: `implementation-ready`
- Implementation shape: 기존 단일 fixture 안에 좁은 helper와 독립 action contract를 추가하며 runtime assembly reference, production script, scene, bootstrap은 변경하지 않는다.
- Product Contract clarification: New Game 삭제 semantics, Craft 실패 저장 semantics, 동기 전투 메시지의 누적 log 관찰을 현재 코드에 맞춰 명시했다.
- Equipment UI coverage: 실제 제품 진입점이 생길 때까지 후속 작업으로 연기했다.
- Confidence check: 추가 deepening 불필요; 저장소 근거, 흐름, 위험, 단위별 시나리오, 검증 계약이 충분하다.
- Headless document review:
  - 적용: Potion 요구사항을 Battle 분류로 이동.
  - 적용: U3 요구사항 추적에서 무관한 New Game 요구사항 제거.
  - 비차단 제안: 포인터 dispatch 전에 EventSystem raycast로 버튼 hit-test 도달성까지 검증할지 사용자 판단 가능.

## Work outcome (2026-07-24)

- Created and switched to `codex/play-mode-qa-expansion` from the user-selected current HEAD.
- Implemented U1-U4 in the existing Play Mode fixture without changing production code, scene/bootstrap assets, the test asmdef, or ProjectSettings.
- Added seven independent serialized-button action contracts under the stable `PlayModeActionContracts` category.
- The post-implementation simplify pass applied four behavior-preserving improvements: one reuse finding and three quality findings; the efficiency review had no actionable finding.
- Verification passed: three final-state targeted runs at 7/7, the full assembly at 42 passed / 0 failed / 1 optional capture skipped, and the console build at 0 warnings / 0 errors.
- The optional EventSystem raycast/top-hit assertion remains deferred as a non-blocking fidelity enhancement; current dispatch still crosses the active scene EventSystem and the serialized Button pointer-click handler.
- Work is complete and the task is ready for the Personal Flow `review` stage.
