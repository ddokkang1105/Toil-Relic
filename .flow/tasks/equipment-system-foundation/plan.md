# Plan: 장비 시스템 도입

## Readiness

- CE plan fallback으로 작성했다. 브레인스토밍과 엔지니어링 리뷰 게이트는 완료됐다.
- 구현은 기존 Unity 전투 단계 및 저장 오류 처리 변경 위에 추가한다. 해당 기존 변경을 되돌리거나 재작성하지 않는다.
- 첫 버전의 정의는 두 구현에 동일하게 유지한다: `starter-weapon` (`AttackBonus` 0)과 `reward-weapon` (`AttackBonus` 2).

## Steps

1. [x] **공통 장비 계약을 콘솔과 Unity에 병렬 추가한다.**
   - `EquipmentSlot` (`Weapon`)과 `EquipmentDefinition` (`Id`, `DisplayName`, `AttackBonus`)을 추가한다.
   - 각 구현에 코드 기반 `EquipmentCatalog`를 만들고 두 장비 정의와 조회 API를 둔다.
   - 카탈로그 조회가 실패한 ID는 플레이어 상태에 노출하거나 장착하지 않는다.

2. [x] **플레이어 장비 소유·장착 상태와 저장 정규화를 추가한다.**
   - 콘솔 `Player`/`PlayerSaveData`에 보유 장비 ID 목록, 장착 무기 ID, 조회·지급·장착·해제 API를 추가한다.
   - Unity `PlayerState`에 동등한 직렬화 필드와 API를 추가한다. 기존 수량형 `inventory`는 그대로 둔다.
   - 신규 게임과 장비 초기화 필드가 없는 기존 저장에서만 기본 무기를 보장·장착한다. 이후 저장된 장착 해제 상태는 유지하고, 알 수 없거나 미보유인 장착 ID만 해제한다.
   - Unity의 기존 `InitDefaults` 조기 반환을 분리하여 아이템 인벤토리가 이미 있어도 장비 정규화가 항상 실행되게 한다. `CurrentSaveVersion`을 2로 올리되 버전 1 파일을 거부하지 않는다.

3. [x] **전투와 전리품에 장비를 연결한다.**
   - 콘솔·Unity 모두에서 기본 플레이어 공격 굴림(4–8)에 장착 무기의 `AttackBonus`를 한 번만 더한다.
   - 첫 승리 처리에서 `reward-weapon`을 중복 없이 지급하고, 실제로 새 지급됐을 때만 전리품 로그에 포함한다.
   - 보상 지급 뒤 플레이어 상태를 게시·저장하는 기존 순서를 보존한다.

4. [x] **최소 조작과 상태 표현을 추가한다.**
   - 콘솔에 장비 보유·장착 상태를 보여 주고, 보유한 무기를 장착하거나 해제하는 메뉴 흐름을 추가한다.
   - Unity `GameManager`와 `GameActionBridge`에 장착·해제용 호출점을 추가하고, `HudController`에 장착 무기 표시용 선택적 텍스트 필드를 추가한다.
   - `UNITY_SETUP.md`에 선택적 장비 텍스트와 버튼 연결 방법을 기록한다. 비교 전용 패널·차이 표시는 추가하지 않는다.

5. [ ] **양 구현의 회귀와 저장 호환성을 검증한다.**
   - 콘솔 빌드를 실행하고, 새 저장·장비 보상·장착·해제·재시작 불러오기를 수동 확인한다.
   - 장비 필드가 없는 기존 형식의 콘솔/Unity 저장을 불러와 기본 무기가 정상 정규화되는지 확인한다.
   - Unity에서 전투 승리, 장착 상태 HUD, 재시작 후 저장 상태, 기존 캠프·전투·제작·물약 흐름을 확인한다.
   - 두 구현에서 동일한 ID·공격 보너스·첫 보상 조건을 대조한다.

## Affected paths

- `src/ToilRelic/Models/Player.cs`, `PlayerSaveData.cs`, 새 장비 모델·카탈로그 파일
- `src/ToilRelic/Systems/CombatSystem.cs`, `src/ToilRelic/Game.cs`, `src/ToilRelic/Util/ConsoleUI.cs`
- `unity/Assets/Scripts/Core/PlayerState.cs`, `GameManager.cs`, 새 장비 모델·카탈로그 파일
- `unity/Assets/Scripts/Systems/CombatSystem.cs`, `unity/Assets/Scripts/UI/HudController.cs`, `GameActionBridge.cs`
- `unity/Assets/Scripts/Save/SaveService.cs`, `unity/UNITY_SETUP.md`

## Validation

- `cd src/ToilRelic && dotnet build`
- 콘솔: 새 게임, 첫 승리 보상, 장착/해제, 저장 후 재실행, 장비 필드 없는 이전 저장 불러오기
- Unity: 플레이 모드에서 같은 시나리오와 캠프·전투·제작·물약 회귀 확인
- 코드 검토: 장비 카탈로그 ID와 공격 보너스가 콘솔·Unity에서 일치하는지, 장착 ID가 항상 보유 ID인지 확인

## Rollback or migration

- 저장 변경은 추가적이다. 이전 코드가 새 장비 필드를 무시할 수 있도록 기존 필드를 제거·이름 변경하지 않는다.
- 정규화는 누락된 장비 필드에만 기본 무기를 보완하며 기존 수량형 인벤토리를 수정하지 않는다.
- 문제가 생기면 장비 기능 코드를 되돌려도 기존 저장의 추가 장비 필드는 무시된다. 릴리스 전 기존 저장 파일 사본으로 불러오기 검증을 수행한다.
