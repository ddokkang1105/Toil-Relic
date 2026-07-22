# Decisions

## Resume record — 2026-07-15

- Resumed while the task remains `blocked`: `equipment-system-foundation` is still in QA. Its console and Unity runtime scenarios cannot run in this environment because the .NET SDK and Unity Editor are unavailable.
- Framework probe: Compound Engineering is installed in the local Codex plugin cache, but its skills are not active in this Codex session, so no CE command was invoked. Reopen Codex or start a new session before attempting the CE gates.
- OpenSpec is installed but not runnable without a system Node runtime; gstack and OMX are not installed. Their gates remain fallback/manual when the prerequisite is cleared.
- Next concrete action: finish the P0 runtime QA follow-up for `equipment-system-foundation`; then resume this task at the pending brainstorm, engineering-review, and design-review gates.

## Confirmed

- 비교는 읽기 전용이며 장착 상태를 변경하지 않는다.
- 콘솔과 Unity 구현은 동일한 비교 규칙을 유지한다.
- 현재 구현에는 장비, 장착 슬롯, 장비 능력치, 장비 획득 경로가 없다. 콘솔의 `ItemType`과 Unity의 `ItemType`은 모두 잡템·보물 재료·보물·물약만 표현한다.
- 이 태스크는 비교 UI만의 작업이 아니라 최소 장비 도메인 모델과 저장·표현 변경을 포함해야 한다.
- 장비 도메인 도입은 선행 태스크 `equipment-system-foundation`으로 분리한다. 이 태스크는 그 완료 후에 브레인스토밍을 재개한다.

## Rejected options

- 기존 인벤토리 수량 아이템을 장비처럼 비교하는 방식: 장착 또는 능력치 데이터가 없어 플레이어의 장착 판단이라는 목표를 충족하지 못한다.

## Open questions

- 최소 장비 시스템을 이 태스크 범위에 포함할 것인가, 아니면 먼저 장비 시스템 태스크를 분리할 것인가?
- 첫 버전을 무기 1개 슬롯과 공격력 1개 능력치로 제한할 것인가?
- 후보 장비는 전투 보상, 테스트용 초기 지급, 제작 중 무엇으로 제공할 것인가?
- Unity에서 비교 결과를 기존 HUD/인벤토리 UI에 넣을지, 별도 패널로 표시할지?

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
| CE brainstorm | blocked pending prerequisite | CE 명령 미노출 환경에서 저장소 기반으로 수행. 장비 도메인이 아직 없음을 확인했다. |
| gstack engineering review | pending | 신규 도메인 모델, 저장 형식, 콘솔·Unity 동기화 범위를 검토한다. |
| gstack design review | pending | Unity 비교 UI 진입점과 차이 표현을 검토한다. |
