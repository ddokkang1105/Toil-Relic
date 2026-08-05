# Decisions

## Confirmed

- `standard` 프로필을 사용한다.
- 이전 HUD 태스크의 Medium 후속 항목과 `battle-regression-1280x720.png`를 시작 근거로 사용한다.
- 시각적 원인은 우측 상태 메시지의 실제 글리프와 BattlePanel 첫 정보 행이 같은 영역을 점유하는 것이므로, 루트 RectTransform 간격뿐 아니라 활성 텍스트 경계와 렌더 캡처를 검증한다.
- 전역 CanvasScaler의 `800x600`, width-first 정책과 `800x450` 16:9 가상 바닥은 유지한다.
- BattlePanel의 적 정보, 턴 안내, 전투 로그와 Attack/Defend/Flee/Potion 네 행동을 모두 보존한다.
- 레이아웃 권위는 `unity/Assets/Editor/ToilRelicSceneBootstrap.cs`이며 구현 후 `SampleScene.unity`를 재생성한다.
- discovery에서 활성 `plan-design-review` 스킬을 사용해 정보 우선순위, 보호 영역, 16:9/4:3 반응형 배치를 결정한다.
- 디자인 검토 범위는 사용자 선택 `B`: 새 태스크 문서와 현재 BattlePanel 화면·코드·회귀 증거를 함께 검토한다.
- 최신 기준 캡처 `equipment-comparison/qa-layout-evidence/battle-failure-1280x720.png`와 `battle-failure-800x600.png`에서는 과거의 직접 중첩이 이미 사라졌다. 따라서 이번 태스크는 과거 위치를 다시 옮기는 작업이 아니라, 현재 우연히 성립한 간격을 실제 텍스트·최악 상태·입력 계약으로 고정하고 전투 패널 자체의 세로 압박을 해소하는 후속 작업이다.
- `800x450` 가상 화면에서 현재 GameStatus는 `x=384..784`, `y=314..434`, 활성 Battle 상태 행은 State `y=412..434`와 Message `y=338..410`이다. `SaveStatusText`는 Battle 중 비활성이고, 이전 저장 실패는 Message의 두 번째 줄로 남을 수 있다.
- 현재 BattlePanel은 `x=420..740`, `y=6..322`여서 Message 사각형과 패널 루트 사이에는 정확히 16px, Message와 첫 Enemy 텍스트 사각형 사이에는 32px가 남는다. 그러나 하단 화면 여백은 6px뿐이고 PhaseText(`y=90..114`)와 BattleLogText(`y=45..95`) 사각형은 5px 겹친다.
- 디자인 완성도 초기 평가는 `6/10`이다. 외부 중첩은 완화됐지만 네 행동의 세로 적층, 내부 텍스트 사각형 중첩, 하단 여백, 실제 렌더 텍스트 검증, Battle 버튼 방향키 계약이 미완성이다.

### Design passes

1. **정보 계층 — 6/10 → 9/10.** 전투 패널의 우선순위는 `Enemy 이름/HP → 현재 턴 → 최근 전투 로그 → 행동`으로 둔다. GameStatus는 전역 최신 사건과 저장 실패를 계속 담당하고, BattleLog는 전투 맥락의 최근 두 논리 줄만 표시해 물리적 표시 용량과 의미를 맞춘다.
2. **상태 커버리지 — 5/10 → 9/10.** Battle 진입, PlayerAction, EnemyAction, Resolving, 이전 저장 실패가 남은 Battle, Camp 전환을 상태 표로 검증한다. Battle 중 SaveStatus 행은 숨겨지고 실패 경고는 Message에 합쳐진다는 실제 컨트롤러 동작을 기준으로 삼는다.
3. **사용자 여정 — 7/10 → 9/10.** 첫 5초에는 Enemy와 선택 가능한 네 행동이 먼저 읽히고, 행동 후에는 턴과 최근 로그가 갱신되며, 승리·도주·패배 후에는 BattlePanel이 사라지고 GameStatus가 결과를 이어받는다.
4. **의도성/AI slop — 8/10 → 9/10.** 기존의 장식 없는 어두운 유틸리티 UI를 유지한다. 새 카드, 그라디언트, 아이콘, 장식적 라운드 요소는 추가하지 않는다.
5. **시스템 정렬 — 8/10 → 9/10.** 기존 uGUI `Text`, 패널 색상, 버튼 색상, `CreateButton`, 4/8px 계열 간격을 재사용한다. 별도 디자인 시스템이나 새 폰트는 만들지 않는다.
6. **반응형/접근성 — 5/10 → 9/10.** 폭 우선 Canvas와 `800x450` 바닥을 유지한다. 패널을 세로 4버튼에서 2×2 행동 그리드로 압축해 상·하단 보호 여백을 늘리고, 모든 버튼은 높이 44px 이상·양의 가로/세로 간격·공간적 명시 탐색을 갖는다. 텍스트는 16px 이상이며 Best Fit을 금지한다.
7. **범위/마감 — 7/10 → 9/10.** GameStatus 위치와 전역 Canvas 정책은 유지하고 BattlePanel 생성값, 제한적인 로그 표시 용량, Play Mode 계약, 재생성 씬과 캡처만 변경한다. 미해결 제품 결정은 없다.

### Selected layout direction

- GameStatus의 폭·앵커·문구는 유지한다. 이전 HUD 태스크에서 확보한 3줄 본문 용량과 다른 화면의 균형을 다시 흔들지 않는다.
- BattlePanel은 현재 우측 열을 유지하되 높이를 약 240 가상 픽셀 수준으로 줄이고, `800x450`에서 상태 본문과 패널 루트 사이 16px 이상, 실제 상태 텍스트와 첫 Battle 텍스트 사이 24px 이상, 화면 하단 16px 이상을 확보한다. 정확한 상수는 plan 단계의 산술과 Unity 렌더 검증으로 확정한다.
- 정보 영역은 Enemy, Phase, 최근 두 줄 BattleLog가 서로 양의 간격을 갖도록 위에서 아래로 배치한다.
- 행동은 첫 행 `Attack | Defend`, 둘째 행 `Flee | Potion`의 2×2 그리드로 배치한다. 이 순서는 기존 행동 순서와 persistent action 의미를 유지한다.
- 방향키는 화면 위치를 따른다. 좌우는 같은 행, 상하는 같은 열로 이동하고 가장자리에서는 반대편으로 순환한다.
- 텍스트 검증은 루트 사각형 산술에 그치지 않는다. 활성 `Text`의 생성된 렌더 정점 경계 또는 동등한 실제 렌더 경계, `preferredWidth/Height`, 할당 사각형을 함께 검사한다.

### State matrix

| State | GameStatus | Battle information | Actions |
|---|---|---|---|
| Battle entry | State + 조우 로그, 선택적으로 저장 실패 경고 | Enemy + 초기 로그 | PlayerAction이면 활성 |
| PlayerAction | 최신 전투 사건 | Enemy + `Your turn` + 최근 2줄 | 네 버튼 활성 |
| EnemyAction | 적 공격 로그 | Enemy + enemy-turn 안내 + 최근 2줄 | 네 버튼 비활성 |
| Resolving | 결과 직전 로그 | Enemy + resolving 안내 + 최근 2줄 | 네 버튼 비활성 |
| Camp transition | 결과/레벨업/저장 피드백 | 패널 숨김·표면 초기화 | 패널 숨김 |

```text
┌────────────────────────────────────────────────────────────┐
│ HUD 요약                              State / Message      │
│                                      (보호 영역)           │
│                              ┌──────────────────────────┐  │
│                              │ Enemy / HP               │  │
│                              │ Turn                     │  │
│                              │ Recent battle log        │  │
│                              │ [ Attack ] [ Defend ]    │  │
│                              │ [ Flee   ] [ Potion ]    │  │
│                              └──────────────────────────┘  │
└────────────────────────────────────────────────────────────┘
```

## Rejected options

- OpenSpec: CLI는 준비됐지만 저장소에 프로젝트 OpenSpec 아티팩트와 활성 `opsx` 스킬이 없어 사용하지 않는다.
- OMX: 독립적인 여러 워크스트림이 아니고 CLI도 탐지되지 않아 사용하지 않는다.
- Brainstorm: 문제, 영향 화면, 증거, 주요 제약이 이미 구체적이므로 별도 요구사항 브레인스토밍을 생략한다.
- 별도의 대규모 아키텍처 검토: 공개 API·데이터·보안 경계는 바뀌지 않아 범위를 확대하지 않는다. 다만 디자인 검토가 2×2 방향 탐색과 로그 표시 용량이라는 상호작용 계약을 추가했으므로 plan 단계에서 해당 계약에 한정한 엔지니어링 검토를 수행한다.
- GameStatus/HUD 숨김, Best Fit, 전역 CanvasScaler 변경: 정보 보존·가독성·다른 화면 회귀 기준에 맞지 않는다.
- GameStatus 폭이나 위치를 다시 줄이는 방식: 이전 태스크가 확보한 다중 상태 메시지 용량과 Title/Camp/Equipment 회귀 위험을 다시 연다.
- 현재 세로 4버튼 구조를 위치만 더 내리는 방식: 패널 하단이 이미 가상 화면 아래 여백 6px까지 내려와 있어 지속 가능한 공간이 없다.
- 네 행동을 한 줄에 모두 놓는 방식: 800px 기준에서 버튼 폭과 레이블 여유가 불필요하게 줄고 방향키 이동이 단일 축으로 제한된다.
- BattleLog 스크롤 영역을 추가하는 방식: 짧은 전투 흐름에 비해 조작 비용과 UI 복잡도가 크다. 이번 범위에서는 최근 두 줄을 명확히 보여준다.
- 루트 RectTransform 간격 테스트만 유지하는 방식: 내부 Phase/Log 사각형 중첩과 실제 글자 잘림을 놓친다.

## Open questions

- 없음. GameStatus는 유지하고 BattlePanel을 2×2 행동 그리드로 압축하는 방향을 확정했다. 최종 좌표는 위의 보호 여백 계약 안에서 plan 단계에 산출한다.

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
| Framework probe | 완료: OpenSpec CLI, gstack, CE 탐지; OMX 미탐지 | `probe-frameworks.ps1` with process-scoped ExecutionPolicy Bypass |
| OpenSpec | 생략 | 프로젝트 아티팩트와 활성 `opsx` 스킬 없음 |
| Design review | 완료 | 활성 `plan-design-review`; 범위 B, 7개 pass 완료, 전체 디자인 완성도 6/10 → 9/10, 미해결 결정 없음 |
| Design evidence | 완료 | 기존 1280×720/800×600 렌더 2장, bootstrap/scene/controller/Play Mode 계약, RectTransform 산술 대조 |
| Design support files | fallback | 설치된 스킬 패키지에 명시된 `sections/review-sections.md`와 `scripts/jargon-list.json`이 없어 본문 7-pass 지침과 gstack `design-checklist.md`를 직접 적용 |
| Brainstorm | 생략 | 이전 후속 기록과 렌더 증거로 요구사항이 충분히 한정됨 |
| Engineering review | plan 단계로 선택 | 2×2 공간 탐색, 최근 2줄 로그, 생성 텍스트 경계 테스트의 구현 가능성과 회귀 전략 검증 |
| Multi-agent / OMX | 생략 | 단일 Unity 레이아웃 워크스트림이며 OMX 미탐지 |
| gstack review log | fallback_canonical | `gstack-review-log`를 호출했으나 필수 런타임 `bun`이 PATH에 없어 JSON 검증 단계에서 기록되지 않음; canonical 결과는 이 파일과 `plan.md`에 보존 |
| Design task JSONL | skipped_tool_missing | `jq`가 설치되어 있지 않아 gstack 집계용 JSONL을 생성하지 않음; 동일한 T1–T5를 `plan.md`에 기록 |
| Design learning log | fallback_canonical | 재사용 가능한 child-text overlap 발견을 기록하려 했으나 `bun` 미설치로 gstack 전역 learning 저장은 실패; 태스크 결정과 T1/T3에 직접 보존 |
