# Decisions

## Discover 재개 기록 — 2026-07-28

- 선행 태스크 `equipment-system-foundation`이 `closed` 상태이고 콘솔·Unity 런타임 QA가 완료되어 기존 차단 조건이 해소되었다.
- 현재 12개 물리 슬롯, 카테고리 호환성, ID 기반 소유권, 슬롯별 장착 상태, 종합 공격력·방어력·피해 감소·최대 HP 계산이 콘솔과 Unity 양쪽에 구현되어 있다.
- 과거 기록의 “장비 도메인이 없다”는 판단은 현재 코드와 일치하지 않아 폐기한다.
- 콘솔에는 실제 장비 메뉴와 슬롯 우선 선택 흐름이 있지만 후보 선택 직후 장착되어 비교 프리뷰가 없다.
- Unity에는 범용 장착 API와 간결한 HUD 표시가 있지만, 실제 장비 화면 진입점과 후보 선택·비교 패널은 없다.
- 근거 점검 결과는 이번 세션의 임시 `grounding.md`에 기록했으며, 아래의 저장소 근거와 남은 결정으로 요약했다.

## 도구 및 게이트 상태

- Personal Flow 프레임워크 프로브 재실행 결과:
  - Compound Engineering과 gstack은 현재 세션에서 사용할 수 있다.
  - OpenSpec CLI는 준비되어 있으나 이 저장소에 활성 산출물이 없어 이번 discover의 주 프레임워크로 사용하지 않는다.
  - OMX는 사용할 수 없다.
- `ce-brainstorm`으로 요구사항과 제품 흐름을 좁힌 뒤, 선택된 engineering/design review 게이트를 수행한다.

## 선행 계약에서 유지하는 결정

- 장비 비교는 후보 ID와 명시적인 목적지 슬롯을 입력으로 받는다.
- 흐름은 슬롯 우선이다. 물리 슬롯을 먼저 선택하고, 그 슬롯과 호환되는 보유 장비만 후보로 보여준다.
- 비교 결과는 후보 장비, 현재 장비, 목적지 슬롯, 능력치별 변화량, 장착 후 종합 공격력·방어력·피해 감소·최대 HP를 포함한다.
- 비교 계산 자체는 읽기 전용이며 장착 상태를 바꾸지 않는다.
- 주무기는 비울 수 없고, 선택 슬롯이 비어 있으면 비어 있는 상태를 중립적인 비교 기준으로 취급한다.
- 장비 화면은 무기·방어구·장신구 그룹으로 구성하고 반지와 귀걸이는 번호로 구분한다.
- HUD는 주무기와 종합 전투 능력치만 간결하게 유지하며, 전체 슬롯과 비교 정보는 장비 화면에서 다룬다.
- 새 장비 아이템, 드롭 확률, 희귀도, 장비 밸런스는 이 태스크 범위 밖이다.

## 종합 확인에서 확정할 제품 결정

1. Unity에서 이번 태스크가 제공할 실제 진입점과 장비 화면 범위.
2. 비교 프리뷰 뒤 장착을 명시적으로 확인할지, 후보 선택으로 즉시 장착할지.
3. 첫 릴리스의 비교 행 표시 규칙과 비어 있는 슬롯 안내 문구.

## Discover 대화 기록

- 2026-07-28: Unity 장비 화면의 형태를 정하기 전 시각 스케치와 텍스트 설명 중 선택을 두 차례 제안했다. 시각 스케치에 대한 명시적 동의 없이 계속 진행 요청을 받았으므로, 시각물을 강제하지 않고 이번 결정은 텍스트 선택지로 진행한다.
- 2026-07-28: Unity 범위 선택에서 완전한 장비 흐름, 읽기 전용 화면, 비교 로직만의 세 방향을 제시했다. 이후의 반복된 계속 진행 요청을 권장 기본안인 “완전한 장비 흐름”으로 진행하라는 동의로 해석한다. 최종 Product Contract 작성 전 범위 종합 확인에서 이 해석을 다시 확인한다.
- 2026-07-28: gstack 디자인 리뷰의 대상은 별도 구현 diff가 아닌 이 태스크의 `task.md`와 `decisions.md`로 잡았다.

## 확정된 제품 흐름

- Unity Camp에 실제로 보이는 장비 진입점을 제공하고, 장비 화면을 열면 다른 Camp 행동 대신 슬롯 선택에 집중하게 한다.
- 화면은 무기·방어구·장신구 슬롯, 선택 슬롯의 호환 보유 장비, 현재/후보 비교, 장착 후 종합 결과 순으로 읽힌다.
- 후보 선택은 비교 프리뷰만 갱신한다. `장착`을 명시적으로 확인하기 전에는 상태와 저장 파일이 바뀌지 않으며, `취소` 또는 `뒤로`는 변경 없이 Camp로 돌아간다.
- 아이템 비교 행은 현재 장비나 후보 장비가 실제로 갖는 능력치의 합집합만 보여 간결하게 유지하되, 장착 후 종합 공격력·방어력·피해 감소·최대 HP는 항상 모두 보여준다.
- 변화량은 부호와 숫자를 항상 표시하고 색은 보조 신호로만 사용한다. 같은 값은 `±0`, 감소는 음수, 증가는 양수로 표현한다.
- 선택 슬롯이 비어 있으면 현재 장비를 `비어 있음`과 0 기준으로 비교한다. 호환 보유 장비가 없으면 이유를 설명하고 장착 행동을 제공하지 않는다.
- 이미 장착 중인 후보는 변화 없음으로 표시하고 장착 행동을 비활성화한다. 장착 성공 후에는 화면을 유지해 갱신된 상태를 확인할 수 있게 한다.
- 콘솔도 슬롯 → 후보 → 비교 → 명시적 확인 순서를 따르며, Unity와 같은 비교 결과를 텍스트로 보여준다.

## 디자인 게이트 대체 검토

- 정식 gstack `plan-design-review` 설치본에는 필수 보조 문서 `sections/review-sections.md`와 gstack 디자이너 바이너리가 없어 완전한 게이트를 실행할 수 없었다. 저장소 기반 텍스트 리뷰로 대체했다.
- 초기 디자인 완성도는 5/10이었다. 진입점, 화면 계층, 비교/확인 순서, 빈 상태, 동일 후보, 성공/취소 상태가 정의되지 않았기 때문이다.
- 위의 제안된 제품 흐름을 적용하면 8/10이다. 실제 시각 목업과 Unity 구현 후 레이아웃 QA는 plan/work/QA에서 확인해야 한다.
- 기존 compact HUD를 확장해 12개 슬롯을 밀어 넣지 않고, Camp 안의 전용 중앙 장비 화면으로 분리한다. 이는 이전 HUD 겹침 문제를 되풀이하지 않고 화면 계층을 분명하게 한다.
- 1280×720 기준에서 세 영역이 한 화면에 들어와야 하며, 긴 장비명은 슬롯과 비교 행동을 밀어내지 않아야 한다.

## 엔지니어링 게이트 대체 검토

- 비교는 알려진 보유 후보와 명시적 목적지 슬롯을 검증하는 순수 읽기 계약이어야 한다. 알 수 없음, 미보유, 비호환 후보는 비교 및 장착 대상으로 인정하지 않는다.
- 결과 능력치는 현재 종합치에서 목적지 슬롯의 현재 장비 기여분을 제거하고 후보 기여분을 더한 값과 기능적으로 같아야 한다.
- 비교 프리뷰와 장착 확인 사이에 상태가 달라질 수 있으므로, 실제 장착 시점에 후보와 슬롯을 다시 검증한다.
- 비교, 취소, 뒤로, 변화 없는 동일 후보는 저장을 발생시키지 않는다. 성공한 장착 또는 선택 슬롯의 유효한 해제만 기존 저장 트리거를 사용한다.
- 콘솔과 Unity는 시작 무기→보상 무기, 보상 무기→시작 무기, 동일 후보, 빈 선택 슬롯, 비호환·미보유·알 수 없는 후보에 같은 기대 결과를 갖는 계약 테스트를 가져야 한다.
- 빈 선택 슬롯 시나리오는 새 생산 아이템을 추가하지 않고 테스트 전용 정의 또는 비교 입력으로 검증한다.
- 현재 계약을 막는 구조적 문제는 발견되지 않았다. 핵심 위험은 비교 계산을 UI마다 따로 만들어 동등성이 어긋나는 것과, Unity에 실제 보이는 진입점 없이 브리지 API만 추가하는 것이다.

## Gate record

| Gate | Status | Evidence |
|---|---|---|
| CE brainstorm | 완료 | 범위 종합 뒤의 계속 진행 요청을 확정으로 받아 `docs/plans/2026-07-28-001-feat-equipment-comparison-plan.md`에 요구사항 전용 Product Contract를 작성했다. |
| gstack engineering review | 대체 검토 완료 | 필수 비교 불변식, 재검증, 저장 경계, 콘솔·Unity 계약 테스트를 저장소 기반으로 검토했다. |
| gstack design review | 대체 검토 완료 | 설치본의 필수 보조 문서와 디자이너 바이너리 부재를 기록하고 텍스트 디자인 리뷰로 대체했다. |

## Claim verification

- 별도 읽기 전용 검증으로 저장소 주장 7개를 확인했다.
- 확인 범위: 양쪽 런타임의 12슬롯·4종 종합 능력치, 콘솔 즉시 장착 흐름, Unity 범용 API와 visible 진입점 부재, compact HUD, 현재 두 개의 생산 장비, 기존 비교 계약, foundation 후속 태스크 연결.
- 반박되거나 검증 불가능한 주장은 없었다.

## Discover closure — 2026-07-28

- 범위 종합 뒤의 계속 진행 요청을 확정으로 해석했다.
- Material open question: 없음.
- Ready for planning check: Complete, Consistent, Focused, Usable by planning.
- `CONCEPTS.md`에 읽기 전용 `Equipment Comparison Preview`의 프로젝트 의미를 기록했다.

## Plan decisions — 2026-07-28 to 2026-08-03

- 최대 HP parity는 절대값 일치가 아니라 동일 장비 기여도와 교체 delta 일치로 확정했다. 콘솔과 Unity의 기존 base MaxHP scale은 유지한다.
- 저장 실패는 기존 mutation-first 경계를 유지한다. 메모리의 장비 변경은 남고 최종 저장 경고가 권위 있는 상태이며, 자동 rollback이나 retry는 추가하지 않는다.
- 비교 preview는 순수 계산으로 유지하고, 실제 Equip/Unequip 명령은 현재 상태를 다시 검증한 뒤 기존 mutation/save 경계를 사용한다.
- Unity 장비 화면은 새 `GameState`가 아니라 Camp-local UI mode로 두고 `EquipmentPanelController`가 로컬 선택·preview·refresh·reset을 소유한다.
- Unity가 소유하는 canonical parity fixture를 콘솔 테스트도 소비하며, 테스트용 catalog 변경은 snapshot/restore와 비병렬 실행으로 격리한다.
- scene bootstrap과 커밋된 `SampleScene.unity`는 하나의 계약으로 변경·검증한다.

## Plan document review — 2026-08-03

- 기존 결정과 모순된 문구 5건을 자동 수정했다: stale confirmation 상태 전이, 저장 실패 시 persistence 설명, 런타임별 Back 동작, rejected command 무이벤트 범위, 존재하지 않는 Unity Cancel 참조.
- 2차 검토에서 두 건을 추가 자동 수정했다: unequip 저장 성공/실패의 persistence 표현, U3에서 변경하지 않을 bridge/controller 파일 제거.
- 승인 대기 상태에서 동일한 plan 계속 명령이 반복되어 best-judgment 일괄 적용의 `Proceed`로 확정했다.
- unequip eligibility operation, rejected-confirm feedback/action state, focus/navigation lifecycle, production-data-only isolated console QA, 최소 control geometry/typography 수치를 모두 canonical plan에 반영했다.
- 남은 actionable review finding은 없으며 계획이 실행 가능하므로 Personal Flow 단계를 `work`로 전환한다.

## Work decisions — 2026-08-03

- 구현은 canonical plan의 U1→U2→U3→U4 순서로 진행했고, 각 단위에서 red/green 증거를 남긴 뒤 다음 단위로 이동했다.
- 비교와 해제 가능성 판단은 콘솔/Unity의 미러된 순수 evaluator가 소유한다. UI는 문자열 표시만 담당하고, 실제 명령은 확인 시점의 현재 상태로 다시 검증한다.
- Unity 장비 화면은 새 top-level `GameState`를 만들지 않고 Camp root 안의 로컬 모드로 구현했다. 고정 Camp/Back/Equip/Unequip 행동은 커밋된 씬의 persistent listener로, 런타임 슬롯/후보 행은 controller의 로컬 listener로 연결했다.
- 성공한 Unity 장비 변경은 후보 소유권이나 호환성 집합을 바꾸지 않으므로 후보 GameObject를 재생성하지 않고 상세·행동·navigation만 새로 계산한다. 외부 `PlayerChanged`는 기존 전체 refresh를 유지한다.
- 카탈로그 정렬 결과 캐싱은 적용하지 않았다. 테스트 fixture가 private catalog 사전을 scoped mutation하고 즉시 복원하는 계약과 상충하며, production catalog가 두 항목뿐이어서 현재의 동적 read-only enumeration이 더 안전하다.
- 동일 값은 제품 계약대로 `±0`을 유지했다. 인코딩 오탐으로 제안된 `0` 변경은 R4/R13과 시각 증거를 위반하므로 거절했다.
- 최종 씬과 bootstrap 계약, 두 viewport, 긴 이름, 빈 후보, 성공, 저장 실패를 자동 계약과 렌더 이미지로 함께 검증했다.
- OS 임시 디렉터리의 production-data console smoke에서 preview-cancel은 canonical save hash를 보존했고, confirm/reload는 `reward-weapon` persistence를 입증했다.
- 이미 존재하던 `equipment-system-foundation` 변경은 이 태스크 범위 밖으로 보고 보존했다. 사용자 요청이 없는 commit/push는 수행하지 않았다.
