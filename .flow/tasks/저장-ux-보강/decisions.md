# Decisions

## Confirmed

- `standard` 프로필을 사용한다.
- 선행 작업인 `main-menu-save-entry-flow`는 완료된 저장 진입 계약으로 취급하며, 이번 태스크는 그 위의 사용자 피드백과 오류 복구 UX를 다룬다.
- 요구사항이 넓고 저장 성공 피드백의 빈도·수명·위치가 정해지지 않았으므로 다음 단계는 `discover`다.
- 사용자 여정과 메시지 계층이 바뀌므로 CE brainstorm과 gstack design review를 discovery 게이트로 사용한다.
- 저장 형식이나 아키텍처 변경이 발견되지 않는 한 engineering review와 OMX 병렬 작업은 사용하지 않는다.
- 저장소 grounding 결과, Unity는 유효하게 로드된 저장만 Continue를 활성화하지만 저장 없음과 손상·읽기 실패를 사용자에게 구분해 설명하지 않는다.
- Unity 자동 저장은 성공 시 `hasSavedGame`만 갱신하고 사용자 메시지를 내지 않으며, 실패 시에만 `SaveFailed`를 표시한다.
- 콘솔 `SaveSystem`은 저장 성공·실패 문구를 모두 생성하지만, 자동 저장 호출부는 실패 문구만 표시한다.
- 저장은 Unity에서 Rest, Craft, 장비 변경, 승리, 패배 후 복구 등에 연결되어 있어 매번 성공 메시지를 기존 상태 영역에 쌓으면 핵심 행동·전투 결과를 가릴 위험이 있다.
- 기존 `BattleOutcome`·`LevelUp`·`SaveFailed` 보존 계약과 실제 저장 실패 통합 테스트는 재사용해야 한다.
- 최우선 사용자 문제는 `A: 자동 저장 신뢰성`으로 확정했다. 중요한 행동 뒤 저장 완료 여부를 사용자가 알 수 있게 하는 것이 1차 목표다. (session-settled: user-directed — Continue 진단, 실패 복구, 세 문제 동시 처리보다 자동 저장 신뢰를 먼저 선택)
- 성공 피드백 표현 방식은 사용자가 시각 스케치 비교를 선택했다. 임시 visual probe에서 기존 상태문 결합, 독립 토스트, 조용한 지속형 저장 상태의 세 방향을 제시했다.

## Rejected options

- 기존 `main-menu-save-entry-flow` 태스크 재개: 해당 태스크는 닫혔고 진입 흐름 구현이 완료되어, 저장 피드백 보강은 별도 후속 범위로 추적한다.
- 시작 단계에서 수동 저장 버튼 또는 저장 완료 토스트를 바로 확정: 빈도와 상태 메시지 우선순위를 먼저 조사해야 한다.

## Open questions

- 저장 성공을 매번 표시할지, 특정 전환·종료 시점에만 표시할지.
- 저장 성공·실패 피드백을 기존 상태 메시지 영역에 둘지, 별도 짧은 표시로 분리할지.
- 유효하지 않거나 손상된 저장을 Title에서 어떤 상태와 복구 행동으로 안내할지.
- 콘솔과 Unity에서 동일한 문구·수명을 요구할지, 매체별 표현만 다르게 할지.
- 명시적 수동 저장 또는 저장 후 종료 행동이 실제 사용자 문제를 해결하는 데 필요한지.
- 저장 성공 피드백을 어떤 시각·행동 패턴으로 표시할지.
- Continue 비활성 이유와 저장 실패 복구 행동을 이번 태스크에 보조 범위로 포함할지, 후속 태스크로 분리할지.

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
| Framework probe | 완료 | `probe-frameworks.ps1`을 프로세스 단위 `ExecutionPolicy Bypass`로 실행 |
| OpenSpec | 사용 안 함 | CLI는 준비됐지만 프로젝트 `openspec/` 산출물과 활성 `opsx` skill이 없음 |
| CE brainstorm | 진행 중 | 활성 `compound-engineering:ce-brainstorm`; grounding 완료, 자동 저장 신뢰성을 최우선 목표로 확정. visual probe `http://localhost:63192`에서 A/B/C 방향 피드백 대기 |
| gstack design review | 대기 | 활성 `plan-design-review`; brainstorm에서 검토할 사용자 흐름과 상태 계약이 결정된 뒤 실행 |
| Engineering review | 현재 생략 | 저장 형식·공개 API·마이그레이션 변경은 아직 범위가 아님 |
| OMX | 생략 | CLI 미탐지 및 독립 workstream 미확정 |
