# Learning: 직렬화된 UI 액션의 결정적 Play Mode 계약

## Context

리스너 바인딩 확인만으로는 실제 가시 버튼을 통한 액션 실행, 상태·메시지 순서·자원 변화·저장 경계·제어권 복귀를 함께 보증할 수 없었다.

## Reusable insight

리플렉션은 진단 가능한 fixture 준비에만 사용하고, 검증 대상 액션은 활성·상호작용 가능한 직렬화 버튼과 씬 `EventSystem`을 통해 실행한다. 초기값과 결과값이 같은 리셋 계약은 비기본 상태를 저장·재로드한 뒤 교체 결과를 검증해 공허한 통과를 막는다. 저장 경로, 정적 이벤트, Unity 난수 상태, 생성 객체는 테스트별로 격리하고, 대상 카테고리의 독립 프로세스 반복 실행과 전체 어셈블리 실행을 함께 남긴다.

## Evidence

- `.flow/tasks/play-mode-qa-확장/qa.md`
- `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`
- [재사용 가능한 상세 지침](../../../docs/solutions/best-practices/deterministic-unity-playmode-action-contracts.md)

## Applies when

- Unity Play Mode에서 실제 UI 액션의 성공·거부·실패·비종료 전투 분기와 저장 부작용을 결정적으로 검증할 때.
