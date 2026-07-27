# Follow-ups

| Title | Why separate | Priority | Link |
|---|---|---|---|
| `GraphicRaycaster` top-hit 계약 추가 | 현재 계약은 활성 직렬화 Button과 persistent listener를 직접 pointer dispatch로 검증한다. 실제 화면 좌표의 도달 가능성과 다른 UI가 클릭을 가로채지 않는지는 별도 공간·레이캐스트 fixture가 필요하다. | Low | [QA known limits](qa.md#known-limits) |
| 이벤트·난수 복원 직접 어설션 | 반복 실행과 전체 스위트가 간접 격리 증거를 제공하지만, disposable scope 종료 직후 구독 해제와 `Random.state` 복원을 직접 증명하려면 별도 관찰 seam이 필요하다. | Low | [QA known limits](qa.md#known-limits) |
| New Game 소유 장비 컬렉션 완전 교체 검증 | 현재 계약은 교체 전 reward weapon 장착과 교체 후 starter weapon 장착을 보증하지만, 소유 장비 컬렉션이 starter-only인지 별도로 단언하지 않는다. | Low | [QA known limits](qa.md#known-limits) |
| Play Mode fixture 분할 | 단일 fixture가 커져 탐색 비용이 증가했다. 기존 fixture 확장 결정을 유지했으므로, 공용 helper 경계를 안정화한 뒤 partial class 또는 책임별 fixture 분리를 별도 리팩터링으로 다룬다. | Low | [Review advisory](review.md#advisory-and-known-limits) |
