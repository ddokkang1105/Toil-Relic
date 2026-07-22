# Decisions

## Confirmed

- `standard` 프로필을 사용한다.
- 시각적 레이아웃 변경이므로 discovery 단계에서 디자인 검토를 수행한다.
- 범위가 단일 HUD 레이아웃 문제로 제한되어 CEO·엔지니어링·브레인스토밍·멀티 에이전트 게이트는 시작 시점에 제외한다.
- 실제 레이아웃 소유 지점(씬, 프리팹, 런타임 UI 코드)을 확인하기 전에는 구현 방식을 확정하지 않는다.
- 디자인 검토 대상은 현재 브랜치 전체 diff가 아니라 이 Personal Flow 태스크 문서와 관련 Unity UI 구조다.
- 레이아웃 값은 `unity/Assets/Editor/ToilRelicSceneBootstrap.cs`가 생성하고 `unity/Assets/Scenes/SampleScene.unity`가 직렬화해 함께 소유한다. 둘은 구현 시 동기화해야 한다.
- Canvas는 `800×600` 기준 해상도에서 폭 우선(`Match Width Or Height = 0`)으로 스케일된다. 따라서 16:9 화면의 가상 높이는 약 450으로 줄어든다.
- 현재 16:9 계산상 중앙 280px 패널의 상단이 상단 HUD 텍스트 영역과 약 57px, 상태 메시지 영역과 약 18px 겹친다. 4:3 `800×600`에서는 같은 계산상 겹침이 없다.
- 기존 UI 방향은 상단 왼쪽 HUD, 상단 오른쪽 상태, 중앙 활성 패널의 3영역 구조다. 이 정보 구조는 유지하고 각 영역의 점유 높이와 안전 여백을 조정한다.
- 프로젝트에 `DESIGN.md`가 없으므로 이번 검토는 기존 씬의 색상·uGUI 패턴과 보편적 가독성·반응형 원칙을 기준으로 한다.
- 디자인 검토 포커스는 사용자 선택 `1B`: 7개 영역을 모두 확인하되 정보 계층, 반응형, 가독성을 집중 검토한다.
- gstack designer 실행 파일과 기존 화면 캡처가 없어 mockup 비교 보드는 생성하지 못한다. 이번 discovery는 계산 기반 와이어프레임과 텍스트 검토로 진행하고, 구현 후 Unity 화면 캡처를 QA 증거로 요구한다.
- Pass 1 정보 구조는 사용자 선택 `2A`: Title에서도 HUD와 상태 영역을 유지하되 압축하고, 중앙 메뉴와 최소 16px의 수직 안전 여백을 확보한다.
- 화면 정보 우선순위는 `중앙 활성 패널/행동 → 상태·결과 메시지 → 지속 HUD 수치` 순으로 둔다. 상태별 표시 로직을 새로 추가하지 않고 기존 3영역을 유지한다.
- Pass 2 상태 커버리지는 사용자 선택 `3A`: 상태 메시지는 `terminal outcome + level-up + save-failure`의 최대 3줄을 보존한다.
- 상태 메시지 본문은 16px을 목표로 하고 약 60px의 본문 높이를 확보한다. 상태 제목과 본문을 포함한 전체 우상단 영역은 16:9의 가상 높이 450 안에서 중앙 패널과 최소 16px 이상 떨어져야 한다.
- Pass 3 사용자 여정은 추가 변경 없이 통과한다. 첫 5초에는 중앙 행동이 우선이고, 플레이 중에는 우상단 결과가 행동의 피드백을 제공하며, 지속 HUD는 맥락을 유지한다.
- Pass 4 의도성/AI slop 검토는 추가 변경 없이 통과한다. 현재 UI는 장식적 카드나 불필요한 시각 효과가 없는 단순 게임 유틸리티 화면이며, 이번 태스크에서도 새 장식을 추가하지 않는다.
- Pass 5 디자인 시스템 정렬은 기존 uGUI `Text`, 패널 색상, 버튼 크기·색상 패턴을 재사용한다. `DESIGN.md` 신설이나 폰트·색상 체계 재설계는 이번 결함 수정 범위를 넘으므로 하지 않는다.
- Pass 6 반응형 정책은 사용자 선택 `4A`: Canvas의 폭 우선 스케일링을 유지하고 `16:9 → 가상 800×450`을 최소 기준 화면으로 삼아 재배치한다.
- 4:3 `800×600`은 회귀 확인 대상이며, 별도 화면비 런타임 분기는 추가하지 않는다.
- 접근성 하한은 상태·HUD 본문 16px 이상, 버튼 높이 44px 이상, 흰색 텍스트와 기존 어두운 패널 간 명확한 대비다. 텍스트 자동 축소로 16px 아래로 내리는 방식은 사용하지 않는다.
- 상단 영역과 중앙 활성 패널 사이의 목표 수직 안전 여백은 최소 16px이다.
- Pass 7 범위는 사용자 선택 `5A`: 동일한 생성 함수와 3버튼 구조를 공유하는 `TitlePanel`과 `CampPanel`을 함께 조정한다.
- `TitlePanel`과 `CampPanel`은 16:9 가상 높이 450에서 상단 점유 영역과 16px 간격을 확보하도록 높이를 약 200px 수준으로 줄이고, 3개 버튼은 최소 44px 높이와 약 10px 이상의 버튼 간격을 유지한다. 정확한 최종 수치는 plan 단계에서 RectTransform 산술과 Unity 화면 검증으로 확정한다.
- `BattlePanel`은 4버튼·전투 정보 구조가 달라 이번 중앙 3버튼 패널 변경에서 제외한다. 다만 구현 후 QA에서 상단 HUD·상태 영역과의 겹침 여부는 회귀 확인한다.

```text
┌────────────────────────────────────────────────────────────┐
│ HUD 요약(좌상단)       안전한 상단 간격       상태(우상단) │
│                                                            │
│              중앙 활성 패널 / 주요 행동                    │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

## Rejected options

- OpenSpec 제안 단계: CLI는 탐지됐지만 프로젝트 OpenSpec 아티팩트와 활성 `opsx` 스킬이 없어 이번 태스크 시작에는 사용하지 않는다.
- OMX 병렬 실행: CLI가 탐지되지 않았고 작업이 독립 워크스트림으로 분리될 만큼 크지 않다.
- Title에서 HUD를 숨기는 방식: 정보 구조는 단순해지지만 상태별 표시 로직을 추가해야 하고 사용자가 선택한 기존 3영역 유지 방향과 맞지 않는다.
- Title에서 HUD와 상태를 모두 숨기는 방식: 오류·저장 상태 메시지까지 사라져 Title 진입 피드백이 약해진다.
- 상태 메시지를 2줄로 제한하는 방식: 결과나 오류의 의미를 축약해야 하고 현재 이벤트 보존 계약을 시각 계층 때문에 약화시킨다.
- 현재 48px 상태 본문 높이를 유지하는 방식: 3줄 조합에서 잘림 위험이 남아 acceptance criteria를 충족하지 못한다.
- 이번 태스크에서 신규 디자인 시스템을 만드는 방식: 단일 레이아웃 결함에 비해 범위와 변경 위험이 과도하다.
- Canvas `Match Width Or Height` 값을 0.5로 바꾸는 방식: 전체 UI 스케일과 위치에 광범위한 회귀를 일으킬 수 있다.
- 화면비별 런타임 레이아웃 분기: 현재 단일 씬·단순 UI 구조에 비해 복잡도가 과도하다.
- 텍스트 자동 축소로 공간을 확보하는 방식: 가독성 하한이 화면과 문자열에 따라 불안정해진다.
- TitlePanel만 조정하는 방식: 같은 3버튼 레이아웃인 CampPanel에 동일한 16:9 위험을 남긴다.
- BattlePanel까지 같은 크기 규칙으로 축소하는 방식: 전투 정보와 4개 행동을 수용하는 별도 구조를 훼손할 수 있다.

## Open questions

- 없음. 구현 세부 수치는 확정된 안전 여백·글자·버튼 하한을 만족하도록 plan 단계에서 계산한다.

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
| Framework probe | 완료: OpenSpec, gstack, CE 탐지; OMX 미탐지 | `probe-frameworks.ps1` (PowerShell 프로세스 한정 ExecutionPolicy Bypass) |
| Design review | 완료 | 활성 `plan-design-review`; 7개 pass 완료, 전체 디자인 완성도 5/10 → 9/10, 미해결 결정 없음 |
| gstack review log | fallback_canonical | `gstack-review-log`는 내부 JSON 검증에 필요한 `bun`이 PATH에 없어 기록하지 못함; 결과는 이 `decisions.md`와 `plan.md`의 terminal GSTACK report에 보존 |
| Design task JSONL | skipped_tool_missing | `jq`가 설치되어 있지 않아 gstack 집계용 JSONL은 생성하지 못함; 동일한 T1–T5 태스크를 `plan.md`에 기록 |
| Design learning log | skipped_tool_missing | `gstack-learnings-log`도 `bun` 부재로 실행 불가; 발견 내용은 canonical decisions에 보존 |
| Brainstorm | 생략 | 요구사항이 겹침 해소로 충분히 한정됨 |
| CE plan | 완료 | 활성 `ce-plan`; Personal Flow 규칙에 따라 `.flow/.../plan.md`를 canonical implementation-ready unified plan으로 심화 |
| Plan confidence check | 통과 | Standard-depth bounded UI fix; local bootstrap, scene, controller, and Play Mode patterns provide sufficient grounding; external research not needed |
| CE document review | 완료: 2 safe fixes | `ce-doc-review` headless 기준을 coherence, feasibility, design 렌즈로 직접 적용; 별도 에이전트 금지 지침 준수. Bounded status strings와 Battle regression wording을 명확화 |
| CE code review | 완료: 3 P2 fixed | correctness, testing, maintainability, project standards, adversarial, agent-native, learnings 렌즈 적용. 운영형 상태 문자열 용량, opt-in 캡처 신뢰성, 버튼별 액션 매핑을 수정하고 재검증 |
| Engineering review | 생략 | 공개 API·데이터·아키텍처 변경이 아님 |
| Multi-agent | 생략 | 독립 병렬 작업 필요 없음 |
