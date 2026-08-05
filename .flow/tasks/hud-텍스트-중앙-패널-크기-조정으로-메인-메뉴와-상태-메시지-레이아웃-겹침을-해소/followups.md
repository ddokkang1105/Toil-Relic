# Follow-ups

## Remaining

| Title | Why separate | Priority | Link |
|---|---|---|---|
| BattlePanel과 우측 상단 상태 메시지의 16:9 중첩 재설계 | BattlePanel은 전투 정보와 4개 행동을 함께 표시하므로, Title/Camp의 중앙 메뉴 압축 규칙을 그대로 적용하면 정보나 조작성을 훼손할 수 있음 | Medium | [`evidence/battle-regression-1280x720.png`](evidence/battle-regression-1280x720.png) |

이 항목은 현재 태스크의 완료를 막지 않는다. Title/Camp 중앙 메뉴와 HUD·상태 영역의 겹침은 QA를 통과했고, BattlePanel 문제는 별도 레이아웃 태스크에서 자체 보호 영역·문자열 용량 계약으로 다룬다.

## Closure record — 2026-08-04

- Result: review의 P2 세 건을 모두 수정했고, QA가 Title/Camp와 상단 HUD·상태 메시지의 목표 해상도 레이아웃을 통과해 태스크를 종료한다.
- Reusable learning: `docs/solutions/design-patterns/width-first-unity-ui-virtual-layout-floor.md`.
- CE compound: Full mode; 관련 문서와 중복도가 moderate여서, 이벤트·캡처 유효성·액션 계약과 구분되는 가상 레이아웃 바닥 패턴을 새 `design_pattern`으로 기록했다.
- Session history: 최근 7일 저장 세션 탐색에서 저장소 경로에 맞는 후보가 없어 현재 태스크 산출물과 코드 근거만 사용했다.
- Grounding: frontmatter와 기계적 주장 검증이 5개 경로·5개 링크·0개 SHA를 모두 통과했다. 의미 검증은 코드 동작·수치·QA 주장을 확인해 모순을 찾지 않았고, 오래된 줄 앵커, 검증할 수 없는 비교 표현 두 곳, 버튼 범위 표현 한 곳을 수정했다.
- Vocabulary: `800x450` virtual layout floor는 프로젝트 고유 도메인 명사가 아닌 일반 Unity 레이아웃 기법이므로 `CONCEPTS.md`에 추가하지 않았다.
- Discoverability: `AGENTS.md`가 이미 `docs/solutions/`와 `CONCEPTS.md`를 안내하므로 변경하지 않았다.
- Selective refresh: 없음. 기존 HUD 이벤트, 스크린샷 유효성, 액션, 상태 보존 문서는 최신이며 새 학습에서 교차 연결했다.
- External issue search: GitHub CLI가 설치되어 있지 않아 생략했으며, 로컬 문서 전체의 중복 분석은 완료했다.
- QA evidence: Unity bootstrap exit 0, 일반 Play Mode 20 passed/0 failed/1 intentional skip, 전용 캡처 1 passed, 6개 PNG 확인, console build 0 warnings/errors.
- Remaining scope: BattlePanel과 우측 상단 상태 영역의 16:9 재설계를 위 Medium 후속 작업으로 유지한다.
- Repository integrity: close에서 변경한 것은 이 태스크의 워크플로 기록과 새 재사용 학습 문서뿐이다. 제품 코드, 테스트, 씬, Git 인덱스, 커밋, 브랜치, 원격 상태는 변경하지 않았다.
