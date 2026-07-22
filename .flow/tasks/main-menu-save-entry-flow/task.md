# Task: Main Menu Save Entry Flow

## Goal

Unity의 첫 진입을 타이틀 메뉴로 통일하고, 유효한 저장이 있을 때만 Continue를 제공하며 New Game은 저장을 초기화한 뒤 캠프로 진입하게 한다.

## Scope

- `SampleScene` 타이틀 패널과 Continue, New Game, Quit 버튼.
- 저장 파일 감지·삭제와 GameManager의 타이틀/캠프 상태 전환.
- Play Mode에서 타이틀 진입 상태, Continue 활성화 규칙, 버튼 바인딩 검증.

## Non-goals

- 저장 슬롯 선택, 이름 입력, 저장 데이터 버전 마이그레이션 UI.
- 전투·제작 밸런스 또는 콘솔 UI의 재설계.

## Acceptance criteria

- [ ] 앱 시작 시 Camp가 아닌 Title 상태와 타이틀 패널을 표시한다.
- [ ] Continue는 유효하게 로드된 저장이 있을 때만 활성화된다.
- [ ] Continue는 저장 상태로, New Game은 기본 상태로 Camp에 진입한다.
- [ ] Title의 Continue/New Game/Quit와 기존 캠프·전투 액션 바인딩이 유지된다.
- [ ] Unity Play Mode 테스트가 통과한다.

## Constraints and risks

- 기존 작업 트리 변경은 보존한다.
- New Game은 실제 저장 파일을 삭제하므로, 자동 테스트는 사용자의 persistent data를 파괴하지 않는다.

## Profile

`standard`
