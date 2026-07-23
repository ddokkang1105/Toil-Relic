# Follow-ups

| Title | Why separate | Priority | Link |
|---|---|---|---|
| 원자적 저장 쓰기와 마지막 정상 저장본 보존 | 이번 태스크는 저장 결과를 정확히 전달하는 UX 계약이며, 임시 파일·원자 교체를 도입하면 플랫폼별 지속성 설계와 장애 주입 검증까지 범위가 확대된다. | High | — |
| 명시적 수동 저장 또는 Save & Quit | 현재 자동 저장과 실패 후 재시도로 1차 신뢰성 문제를 해결했다. 새 버튼·입력 흐름·종료 계약은 실제 사용 요구가 확인될 때 별도 discovery가 필요하다. | Medium | — |
| 저장 계약 경계 조합 테스트 확대 | modern-core version 0, score-only version 1/2 거부, historical version 0 필드별 누락, Unity `JsonUtility`의 present-but-wrong-kind 조합은 현재 계약을 막지 않는 추가 회귀 범위다. | Low | — |
| 패키지된 Unity Player 시각 QA | 이번 검증은 Unity Editor PlayMode 기준이다. 플랫폼별 폰트·렌더링 차이를 보증하려면 대상 Player 빌드에서 동일 뷰포트와 상태를 별도로 확인해야 한다. | Low | — |
| 저장소 지식 문서 진입점 추가 | `docs/solutions/`의 Unity QA 교훈은 현재 `AGENTS.md`에서 직접 안내되지 않는다. 저장소 전역 지침 변경은 별도 동의 후 한 줄 진입점으로 추가한다. | Low | `docs/solutions/best-practices/unity-playmode-screenshot-evidence-validity.md` |
