# Decisions

## Confirmed

## Rejected options

## Open questions

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
## Decisions

- Profile: `standard`. 첫 진입 사용자 여정이 바뀌지만 기존 콘솔 흐름이 기준으로 존재해 별도 브레인스토밍은 생략한다.
- OpenSpec: 로컬 CLI는 감지됐지만 현재 세션에 opsx skill이 활성화되지 않았고 프로젝트 OpenSpec 아티팩트도 없다. Personal Flow task 문서가 요구사항 기준이다.
- Design gate: 타이틀 메뉴는 기존 Unity의 단순 uGUI 패널 패턴과 콘솔의 Continue/New Game 모델을 따른다. 외부 디자인 시스템/시안이 없으므로 코드·씬 구성 검토로 대체한다.
- Save rule: `TryLoad`로 유효하게 로드된 저장만 Continue 대상으로 본다. 파일 존재만으로 Continue를 활성화하지 않는다.
- New Game rule: 새 플레이어 상태를 만들기 전에 기존 저장을 삭제한다. 자동 테스트는 이 동작을 직접 호출하지 않아 사용자 persistent data를 보존한다.
