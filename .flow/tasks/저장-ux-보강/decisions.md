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
- 성공 피드백 표현 방식을 결정하는 절차로 사용자가 텍스트 설명보다 시각 스케치 비교를 선택했다. 이때의 `A`는 시각 비교 방식 선택이며, 저장 피드백 안 자체의 선택은 아니다. 임시 visual probe에서 기존 상태문 결합, 독립 토스트, 조용한 지속형 저장 상태의 세 방향을 제시했고, 이후 정보 구조 승인으로 `C: 조용한 지속형 저장 상태`를 최종 방향으로 확정했다.
- 반복된 discovery 계속 진행 요청을 직전 추천의 승인으로 해석해, design review 대상은 이 태스크의 `task.md`와 `decisions.md`, 검토 범위는 정보 구조·상태·여정·구체성·디자인 체계·반응형/접근성·미결정 사항의 7개 영역 전체로 확정했다. 우선순위는 저장 상태의 위치와 계층, 상태 전이, 800×600 안전성이다.
- design review Pass 1 정보 구조는 상단 오른쪽 상태 스택을 `현재 게임 상태 → 주 행동·전투 결과 → 저장 상태` 순서로 구성하고, 저장 상태를 주 메시지 아래의 한 줄 전용 보조 행으로 분리하는 방향으로 확정했다. 반복된 discovery 계속 진행 요청을 직전 추천 승인으로 반영했다.
- design review Pass 2의 저장 복구 전이는 실패 후 다음 자동 저장이 성공하면 전용 저장 행을 즉시 `Save: Saved just now`로 바꾸고, 오래된 실패 경고를 더 이상 고정하지 않는 규칙으로 확정했다. 로컬 동기 저장에는 순간적인 `Saving…` 상태를 표시하지 않고 최종 결과만 표시한다.
- Title의 저장 상태는 `저장 없음`, `유효한 저장`, `저장을 읽을 수 없음`으로 구분한다. 읽기 불가 시 Continue를 비활성화하고 `Save could not be read. Start New Game to replace it.`을 안내하며, 파일은 자동 삭제하지 않고 사용자가 New Game을 선택할 때만 기존 삭제 계약에 따라 교체한다.
- 저장 실패는 행동·전투 결과를 보존한 채 지속형 경고로 알리되 플레이를 차단하지 않는다. 다음 진행 변경에서 자동 저장을 다시 시도하고, 성공하면 실패 경고를 해제해 사용자가 복구를 알아차릴 수 있게 한다.
- 플레이어에게 표시하는 저장 문구는 콘솔과 Unity 모두 기존 게임 UI에 맞춘 짧은 영어 계약으로 통일한다. 성공 행은 `Save: Saved just now`, 실패 행은 `Save: Failed`, 주 경고는 `Save failed. Progress may not be saved.`를 사용한다. 파일 경로와 원시 예외는 플레이어 상태 문구에서 제외하고 개발자 진단으로 분리한다.
- Unity의 `SaveStatusText`는 기존 상태 스택의 세 번째 `Text`로 추가하고 `LegacyRuntime.ttf`, 16px, 흰색, 우측 정렬, 자동 글자 축소 없음 규칙을 재사용한다. 작은 회색 글자나 새 장식 대신 위치와 짧은 문구로 보조 계층을 표현한다. 정식 디자인 시스템 구축은 별도 `/design-consultation` 범위로 남긴다.
- Unity 상태 컨테이너는 96px에서 120px로 늘리되 저장 행은 Camp에서 저장 결과가 있을 때만 표시한다. Title·Battle 진입 시 숨겨 800×600 BattlePanel과의 겹침을 피하고, 800×600 및 1280×720에서 메뉴·전투 패널 비겹침과 16px 가독성을 검증한다.
- 성공한 자동 저장은 중요한 행동마다 전용 저장 상태를 조용히 갱신하고, `Saved just now`에 해당하는 상태를 다음 저장 결과 또는 화면 전환까지 유지한다. 기존 행동·전투·레벨업 메시지는 덮지 않는다.
- 콘솔과 Unity는 `저장 성공`, `저장 실패`, `불러오기 성공`, `불러오기 불가`의 의미와 다음 행동을 맞추되, 콘솔은 순차 메시지이고 Unity는 지속형 상태인 매체 차이를 허용한다.
- 손상되거나 읽을 수 없는 저장은 이번 태스크의 보조 범위에 포함한다. Continue는 비활성화하고 이유와 New Game 진행 가능성을 안내하되, 자동 삭제·복구·백업 UI는 후속 범위로 남긴다.
- 명시적 수동 저장과 `Save & Quit`은 이번 태스크에서 제외하고, 자동 저장 피드백과 다음 진행 변경 시 재시도로 1차 신뢰성 문제를 해결한다. (session-settled: user-approved — 즉시 재시도 버튼 추가보다 자동 저장 흐름 보강을 선택: 새 입력·종료 계약으로 범위가 커지는 것을 피함)
- 확정된 범위 종합안을 requirements-only Product Contract `docs/plans/2026-07-22-001-feat-save-ux-reliability-plan.md`로 기록한다.
- 읽기 전용 코드 교차 검증에서 Unity의 로드 결과 혼합, 저장 성공 피드백 부재, 콘솔 자동 저장 성공 메시지 미노출, 명시적 수동 저장 흐름 부재를 모두 확인했다. 콘솔 `Quit`은 종료 직전 자동 저장을 수행하므로 기존 동작을 회귀 방지 요구사항으로 유지한다.

## Rejected options

- 기존 `main-menu-save-entry-flow` 태스크 재개: 해당 태스크는 닫혔고 진입 흐름 구현이 완료되어, 저장 피드백 보강은 별도 후속 범위로 추적한다.
- 시작 단계에서 수동 저장 버튼 또는 저장 완료 토스트를 바로 확정: 빈도와 상태 메시지 우선순위를 먼저 조사해야 한다.
- 전체 저장 경로나 원시 예외를 플레이어 상태 문구에 직접 노출: 한 줄 저장 행을 깨뜨리고 콘솔·Unity의 문구를 불일치시키므로 개발자 진단으로 분리한다.

## Open questions

- 없음. 범위 종합안 전체에 대한 최종 사용자 확인만 남아 있다.

## Design review progress

- 사전 감사: UI 범위는 Title의 저장 진입 안내와 플레이 중 HUD의 저장 상태 피드백이다. 저장소에 `DESIGN.md`, `TODOS.md`, 과거 gstack 디자인 리뷰 기록은 없고, 기존 상태 패널·Title 패턴과 800×600 QA 증거를 재사용한다.
- 도구 폴백: gstack designer와 browse 실행 파일이 없어 기존 UTF-8 HTML visual probe를 시각 근거로 사용한다.
- 초기 디자인 완성도: `6/10`. 행동 결과가 먼저 보이고 저장 상태가 독립된 보조 정보가 되며, 성공·실패·손상 상태와 800×600 제약까지 명시되면 10점에 해당한다.
- Pass 1 정보 구조: `6/10 → 9/10`. 기존 상단 오른쪽 메시지 영역은 스트레스 상태에서 이미 3줄을 사용하므로 저장 상태를 합치지 않는다. `State: Camp` 아래에 주 행동·전투 결과를 두고, 그 아래 한 줄 전용 보조 행에 `Save: Saved just now` 형식의 저장 상태를 둔다. 남은 1점은 Pass 6의 800×600 실측 제약에서 검증한다.
- Pass 2 상호작용 상태: `5/10 → 9/10`. 저장 전에는 전용 행을 숨기고, 성공 시 `Save: Saved just now`, 실패 시 저장 실패와 진행 유실 가능성을 표시하며, 다음 성공 시 실패 경고를 해제하고 성공 상태로 복구한다. 로컬 동기 저장에는 순간적인 `Saving…` 표시를 추가하지 않는다. Title은 저장 없음·유효함·읽기 불가를 구분한다. 남은 1점은 Pass 4의 최종 문구 구체화에서 검증한다.

### Pass 2 state contract

| Context | State | What the user sees | Available next action |
|---|---|---|---|
| Title | 저장 없음 | Continue 비활성, `Start a new game to begin.` | New Game |
| Title | 유효한 저장 | Continue 활성, `Save found. Continue or start a new game.` | Continue 또는 New Game |
| Title | 저장 읽기 불가 | Continue 비활성, `Save could not be read. Start New Game to replace it.` | New Game; 파일은 선택 전까지 보존 |
| Title | 기존 저장 삭제 실패 | Title 유지, `Could not clear save: …` | 원인을 확인한 뒤 재시도 또는 종료 |
| Gameplay | 아직 저장 결과 없음 | 저장 전용 행 숨김 | 정상 플레이 |
| Gameplay | 자동 저장 성공 | `Save: Saved just now` | 중단 없이 정상 플레이 |
| Gameplay | 자동 저장 실패 | 저장 행과 고우선순위 메시지에 실패 및 진행 유실 가능성 표시 | 플레이 지속; 다음 자동 저장에서 재시도 |
| Gameplay | 실패 후 저장 성공 | 실패 경고 해제, `Save: Saved just now` | 정상 플레이 |

- Pass 3 사용자 여정: `6/10 → 9/10`. Title 진입부터 자동 저장 성공·실패·복구까지 흐름을 정의했다. 실패는 행동 결과를 보존한 비차단 지속형 경고로 알리고, 다음 진행 변경의 자동 저장 성공으로 복구한다. 남은 1점은 실제 플레이에서 경고가 충분히 눈에 띄면서 방해하지 않는지 QA로 검증한다.

### Pass 3 journey storyboard

| Step | User does | User feels | UX support |
|---|---|---|---|
| 1 | Title에 진입 | 내 진행 상태를 알고 싶음 | 저장 없음·유효함·읽기 불가를 즉시 구분 |
| 2 | Continue 또는 New Game 선택 | 선택 결과를 통제하고 싶음 | 가능한 행동과 저장 교체 결과를 명시 |
| 3 | 전투·휴식·제작·장비 변경 완료 | 행동 결과가 우선 궁금함 | 주 메시지가 행동 결과를 먼저 유지 |
| 4 | 자동 저장 성공 | 진행이 보존됐다는 안도감 | 전용 보조 행이 조용히 성공 상태로 갱신 |
| 5 | 자동 저장 실패 | 진행 유실을 걱정함 | 비차단 지속형 경고와 진행 유실 가능성 안내 |
| 6 | 다음 자동 저장 성공 | 문제가 해소됐는지 확인하고 싶음 | 실패 경고를 성공 상태로 교체해 복구를 명시 |

- Pass 4 구체성/AI slop 위험: `6/10 → 9/10`. 새 토스트·카드·아이콘·장식 효과를 만들지 않고 기존 게임 HUD에 전용 한 줄만 추가한다. 플레이어 문구는 `Save: Saved just now`, `Save: Failed`, `Save failed. Progress may not be saved.`로 고정하고, 전체 경로와 원시 예외는 개발자 진단으로 분리한다. 남은 1점은 실제 화면에서 문구가 한 줄 계약을 지키는지 QA로 검증한다.
- Pass 5 디자인 시스템 정렬: `7/10 → 9/10`. 저장소에 `DESIGN.md`가 없어 정식 디자인 시스템은 별도 `/design-consultation` 대상이지만, 이 태스크는 새 시각 언어를 만들지 않는다. `SaveStatusText`는 기존 상태 스택의 세 번째 `Text`로 추가하고 `LegacyRuntime.ttf`, 16px, 흰색, 우측 정렬, 자동 축소 없음 규칙을 그대로 재사용한다. 남은 1점은 정식 디자인 시스템 부재에 따른 프로젝트 차원의 후속 항목이다.
- Pass 6 반응형/접근성: `6/10 → 9/10`. 상태 높이는 96px에서 120px로 늘리되 저장 행은 Camp에서 저장 결과가 있을 때만 표시하고 Title·Battle 진입 시 숨긴다. 이 규칙은 800×600에서 오른쪽 BattlePanel 상단과 생길 수 있는 약 16px의 시각 겹침을 피한다. 16px 흰색 문구, 텍스트 기반 성공/실패 구분, 자동 축소 금지로 색상이나 크기에만 의존하지 않으며 800×600과 1280×720에서 비겹침을 검증한다. 남은 1점은 실제 렌더 QA로 확인한다.
- Pass 7 미결정 사항: `0개`, 명시적 후속 범위 `1개`. 명시적 수동 저장과 `Save & Quit`은 새 버튼·입력 흐름·종료 계약까지 범위를 넓히므로 이번 태스크에서 제외한다. 자동 저장 피드백과 다음 진행 변경 시 재시도로 1차 신뢰성 문제를 해결한 뒤 실제 사용 증거가 있을 때 후속으로 검토한다.
- 최종 디자인 완성도: `9/10`. 남은 1점은 구현 후 실제 렌더 QA와 프로젝트 차원의 정식 디자인 시스템 부재에 해당하며, 현재 discovery 범위에 미결정 디자인 사항은 없다.

## NOT in scope

- 명시적 수동 저장 버튼과 `Save & Quit` 흐름.
- 손상 저장의 자동 삭제·복구·백업 UI.
- 저장 데이터 형식 변경, 버전 마이그레이션, 전체 HUD 또는 디자인 시스템 재설계.

## What already exists

- 콘솔과 Unity 양쪽의 저장 서비스 및 자동 저장 호출 지점.
- Unity의 `GameEvents`, `GameStatusController`, Title 진입 흐름, 런타임 씬 부트스트랩.
- 기존 저장 실패 통합 테스트와 800×600·1280×720 HUD QA 스크린샷.

## Implementation Tasks

- [ ] `P1` — 저장 불러오기 결과를 없음·성공·읽기 불가로 구분하고, 읽기 불가 파일 보존 및 기존 New Game 교체 계약을 콘솔과 Unity에 반영한다. 예상: 중간.
- [ ] `P1` — 자동 저장 성공·실패·복구 상태를 발행하고 Unity 전용 저장 상태 행에 연결한다. Title·Battle에서는 숨기고 플레이어 문구와 개발자 진단을 분리한다. 예상: 중간.
- [ ] `P2` — 콘솔 저장/불러오기 피드백을 Unity와 의미상 맞추고, 원시 경로·예외를 플레이어 문구에서 제외한다. 예상: 작음.
- [ ] `P1` — Title 3상태, 저장 성공→실패→복구, 주 메시지 보존, 800×600·1280×720 비겹침을 자동/수동 QA로 검증한다. 예상: 중간.

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
| Framework probe | 완료 | `probe-frameworks.ps1`을 프로세스 단위 `ExecutionPolicy Bypass`로 실행 |
| OpenSpec | 사용 안 함 | CLI는 준비됐지만 프로젝트 `openspec/` 산출물과 활성 `opsx` skill이 없음 |
| CE brainstorm | 완료 | 활성 `compound-engineering:ce-brainstorm`; 사용자 범위 확인 및 코드 주장 교차 검증 완료, requirements-only Product Contract 작성 |
| gstack design review | 완료 | 활성 `plan-design-review`; 7개 패스 완료, 최종 9/10, 미결정 사항 0개. 누락된 `sections/review-sections.md`는 메인 스킬에 내장된 7개 패스 정의로 대체 |
| Engineering review | 현재 생략 | 저장 형식·공개 API·마이그레이션 변경은 아직 범위가 아님 |
| OMX | 생략 | CLI 미탐지 및 독립 workstream 미확정 |

## GSTACK REVIEW REPORT

- Runs: `plan-design-review` 1회(7개 패스), 초기 `6/10` → 최종 `9/10`.
- Status: 디자인 게이트 완료. 저장 상태 계층, 상태 전이, 사용자 여정, 문구, 기존 시각 체계 재사용, 반응형·접근성 계약을 확정했다.
- Findings: 구현 전 미결정 사항은 없으며, 수동 저장과 `Save & Quit`은 명시적 후속 범위다.
- Tool fallback: `sections/review-sections.md`가 설치본에 없어 메인 스킬의 동일 패스 정의를 사용했다. `jq`와 `bun`이 없어 사용자 홈의 JSONL 작업·리뷰 로그 보조 기록은 생성하지 못했다.

NO UNRESOLVED DECISIONS
