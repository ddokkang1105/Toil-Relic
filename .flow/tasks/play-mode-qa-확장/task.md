# Task: Play Mode QA 확장

## Goal

Unity `SampleScene`의 핵심 사용자 흐름과 UI 상태를 Play Mode에서 더 넓고 안정적으로 검증해, 저장·전투·캠프·레이아웃 회귀가 수동 확인 전에 명확한 실패로 드러나게 한다.

## Scope

- 현재 단일 Play Mode 테스트 어셈블리의 36개 시나리오와 헬퍼 구조를 인벤토리하고, 중복이 아닌 실제 회귀 공백을 식별한다.
- 실제 UI 입력으로 정상 New Game, Potion, Craft, 보이는 장비 버튼, 비치명 Attack, 실패 Flee의 대표 계약을 검증한다.
- 각 행동을 독립된 결정적 Play Mode 테스트로 추가하고 상태·메시지·자원·저장 결과별 실패 원인을 식별한다.
- 신규 대상 테스트 3회 연속과 전체 `ToilRelic.PlayModeTests` 어셈블리 1회를 Unity batch mode에서 검증한다.

## Non-goals

- Play Mode QA와 직접 관련 없는 게임 기능·밸런스·저장 형식 변경.
- 전체 테스트 프레임워크 교체나 기존 36개 테스트의 전면 재작성.
- discovery에서 필요성이 입증되지 않은 런타임 어셈블리 참조 추가 또는 광범위한 production 테스트 훅 도입.
- 패키지된 Player, 실제 기기, 플랫폼별 성능·그래픽 호환성의 완전한 E2E 보증.
- 저장 호환성 조합 확대, CI 신설, UI 진입점이 없는 generic 장비 해제 API 검증.

## Acceptance criteria

- [ ] 기존 36개 Play Mode 시나리오의 커버리지와 실제 회귀 공백이 상태·행동·검증 계층별로 기록된다.
- [ ] 정상 New Game, Potion 성공·대표 거부, Craft 성공·실패, 보이는 장비 버튼의 소유·미소유, 비치명 Attack, 실패 Flee가 실제 UI 입력으로 각각 독립 검증된다.
- [ ] 각 실패 메시지가 대상 행동과 깨진 상태·메시지·자원·저장 계약을 식별한다.
- [ ] 테스트는 격리된 임시 저장 경로를 사용하고 사용자의 실제 저장 파일을 읽거나 변경하지 않는다.
- [ ] 신규 대상 테스트가 동일 조건에서 3회 연속 통과하고 전체 `ToilRelic.PlayModeTests`가 1회 통과한다.
- [ ] 제품 UI 외형이 바뀔 때만 새 캡처를 요구하며, 이 경우 graphics-enabled 실행·픽셀 유효성·직접 검사를 함께 통과한다.
- [ ] 정확한 실행 명령, 결과 수, 수동 확인 항목, 알려진 한계가 `qa.md`에 기록된다.

## Constraints and risks

- Unity 기준 버전은 `6000.3.19f1`이며, 현재 Play Mode 테스트는 `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs` 한 파일에 집중되어 있다.
- `ToilRelic.PlayModeTests.asmdef`는 런타임 어셈블리 참조가 비어 있으므로 기존 reflection 기반 공개 씬 계약 검증 방식을 우선 보존한다.
- fixture setup/teardown 실패가 임시 저장 경로, 이벤트 구독, 생성 오브젝트를 다음 테스트에 누출하지 않아야 한다.
- 일반 회귀 실행의 `-nographics`와 시각 증적 캡처의 graphics-enabled batch run을 구분한다.
- 테스트 수 확대가 실행 시간과 flaky frame wait를 불필요하게 늘리지 않도록 기존 헬퍼 재사용과 상태 직접 설정의 경계를 discovery에서 결정한다.

## Profile

`standard`
