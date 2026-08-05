# Task: battle-panel-우측-상단-상태-메시지-16-9-중첩-재설계

## Goal

폭 우선 Unity Canvas의 16:9 가상 화면(`800x450`)에서 우측 상단 GameStatus 메시지와 BattlePanel의 적 정보·전투 안내가 시각적으로 겹치지 않도록 레이아웃을 재설계한다. Attack/Defend/Flee/Potion 네 행동의 가독성과 조작성, 기존 상태 메시지 의미는 그대로 유지한다.

## Scope

- `unity/Assets/Editor/ToilRelicSceneBootstrap.cs`가 생성하는 BattlePanel 및 필요한 GameStatus 배치·크기·텍스트 영역 조정.
- 생성 권위에 맞춘 `unity/Assets/Scenes/SampleScene.unity` 재생성.
- `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`의 16:9 보호 영역, 실제 텍스트 경계, 버튼 크기·간격·액션 연결 계약 보강.
- `1280x720`과 `800x600`에서 초기 전투와 대표적인 장문/다중 상태 메시지의 렌더 캡처 확인.
- 필요할 때만 BattlePanel 표시 계층의 제한적인 UI 코드 조정. 전투 규칙은 변경하지 않는다.

## Non-goals

- 전투 계산, 적 데이터, 아이템·회복약 소비, 저장 이벤트 의미 변경.
- 전역 CanvasScaler 정책 변경 또는 화면비별 런타임 분기 도입.
- Title, Camp, EquipmentPanel의 별도 시각 재설계.
- HUD나 GameStatus 정보를 숨기거나 폰트 자동 축소로 문제를 회피하는 방식.
- 콘솔 게임의 전투 UI 변경.

## Acceptance criteria

- [ ] `1280x720`(`800x450` 가상 화면)에서 GameStatus의 실제 활성 상태·메시지·저장 행과 BattlePanel의 실제 보이는 적 정보·안내 텍스트 사이에 최소 16 가상 픽셀의 간격이 있다.
- [ ] 초기 조우 문구와 실제 생성 가능한 장문/다중 상태 메시지 조합에서도 텍스트가 잘리거나 BattlePanel을 침범하지 않는다. Battle 중에는 저장 행이 숨겨지고, 이전 저장 실패 경고가 본문 두 번째 줄에 남을 수 있음을 계약에 포함한다.
- [ ] BattlePanel의 Enemy/turn/log 정보가 16px 이상의 읽을 수 있는 크기로 유지되고 Best Fit에 의존하지 않는다.
- [ ] Attack/Defend/Flee/Potion 네 버튼은 2×2 행동 그리드에서 각각 44 가상 픽셀 이상의 높이, 양의 가로·세로 간격, 정확한 persistent target/method 연결과 공간적으로 일치하는 명시적 방향키 이동을 유지한다.
- [ ] BattlePanel은 `800x450` 가상 화면의 모든 가장자리에서 최소 16 가상 픽셀 안쪽에 머문다.
- [ ] `800x600`에서도 BattlePanel, GameStatus, HUD의 기존 계층과 화면 내 배치가 회귀하지 않는다.
- [ ] bootstrap 재생성 뒤 관련 Play Mode 계약과 전체 Play Mode 회귀가 통과한다.
- [ ] 그래픽 활성화 전용 캡처에서 최소 `1280x720`과 `800x600`의 전투 화면을 생성하고 실제 픽셀을 검사한다.
- [ ] 콘솔 `dotnet build`가 0 errors로 유지된다.

## Constraints and risks

- 레이아웃 생성의 단일 권위는 `ToilRelicSceneBootstrap.cs`이며 씬만 손으로 수정하지 않는다.
- 현재 폭 우선 `800x600` CanvasScaler와 `800x450` 16:9 가상 바닥 계약을 유지한다.
- 기존 테스트의 루트 사각형 간 16px 계산만으로는 글리프 중첩을 놓칠 수 있으므로 실제 활성 텍스트 경계와 렌더 증거를 함께 사용한다.
- BattlePanel은 정보와 네 행동을 함께 담으므로 Title/Camp 압축 수치를 그대로 복사하지 않는다.
- 변경 범위가 Unity UI에 한정되더라도 생성 씬과 Play Mode 계약이 함께 변하므로 동기화 실패를 주의한다.
- `.flow/tasks/equipment-comparison/work-*`의 기존 추적되지 않은 중간 QA 산출물은 이 태스크 범위 밖이며 수정·삭제·커밋하지 않는다.

## Profile

`standard`

기존 결함과 증거는 명확하지만, 전투 정보 계층과 네 행동의 공간 배분을 다시 결정하는 시각적 변경이므로 discovery의 디자인 검토가 필요하다. 공개 API·데이터·보안·아키텍처 변경은 없어 `large`까지 확대하지 않는다.
