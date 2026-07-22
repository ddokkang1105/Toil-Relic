# Decisions

## Confirmed

- 장비 비교 기능을 가능하게 하는 선행 태스크다.
- 콘솔과 Unity 구현의 장비 규칙은 기능적으로 일치해야 한다.
- 비교 UI, 복수 슬롯, 강화·희귀도 시스템은 범위에서 제외한다.
- 첫 버전은 `Weapon` 슬롯 하나와 `AttackBonus` 능력치 하나만 지원한다.
- 장비는 무작위 옵션이 없는 정적 정의로 두고, 보유·장착 상태에는 안정적인 장비 정의 ID만 저장한다. 장비 인스턴스 ID는 도입하지 않는다.
- 새 플레이어와 기존 저장 플레이어는 기본 무기 하나를 보유·장착한 상태로 정규화한다.
- 첫 전투 승리 시 아직 보유하지 않은 후보 무기 하나를 지급한다. 후속 비교 태스크는 이 기본 무기와 후보 무기를 비교한다.
- 장착 무기의 공격 보너스는 콘솔과 Unity의 플레이어 공격 굴림에 동일하게 적용한다.
- Unity에서는 장착 상태와 최소 조작 API를 제공하되, 비교 전용 패널은 후속 태스크에서 만든다.
- 콘솔과 Unity에 각각 코드 기반 `EquipmentCatalog`를 두고, 동일한 안정 ID·표시명·공격 보너스를 정의한다. Unity ScriptableObject를 첫 버전에 도입하지 않아 수동 에셋 설정과 콘솔 구현 간 드리프트를 피한다.
- 장비 보유 상태는 기존 수량형 `Inventory`와 분리해 `ownedEquipmentIds`와 `equippedWeaponId`로 저장한다.
- 저장 불러오기 시 누락·빈·알 수 없는 장비 ID를 정규화한다. 기본 무기를 보유시키고 유효한 장착 무기가 없으면 기본 무기를 장착한다.
- Unity 저장 버전은 2로 올리되, 명시적 변환 테이블 대신 `InitDefaults`/정규화 메서드에서 누락 필드를 보완한다.
- 공격 보너스는 CombatSystem이 플레이어 상태를 소유하지 않도록 호출부에서 전달하고, 콘솔·Unity의 기존 기본 공격 범위(4–8)에 더한다.
- 장착 해제는 의도적인 상태이므로 신규·기존 저장을 처음 정규화할 때만 기본 무기를 자동 장착한다. 이후 저장된 빈 장착 상태는 유지한다.

## Rejected options

- 장비 모델 없이 비교 UI부터 구현하는 방식: 비교 대상과 장착 상태가 없어 유효한 기능을 만들 수 없다.
- 무작위 옵션을 가진 장비 인스턴스 모델: 첫 버전의 비교 요구에는 필요 없고 저장·동기화 복잡도만 늘린다.
- 장비를 기존 수량형 `ItemType`에 추가하는 방식: 보유 수량과 개별 장착 상태를 혼합해 후속 비교 API를 불명확하게 만든다.
- Unity 장비 정의를 ScriptableObject로만 두는 방식: 콘솔 구현과의 동기화, 에셋 생성·배치가 첫 버전에 불필요한 의존성을 만든다.

## Open questions

- 없음. 계획 단계에서 확정한 파일 경계와 검증 절차를 작업 항목으로 분해한다.

## Gate record

| Gate | Result | Source / fallback |
|---|---|---|
| CE brainstorm | complete (fallback) | CE 명령 미노출 환경에서 저장소 기반으로 수행. Weapon 1슬롯, AttackBonus, 정적 정의 ID, 기본 장비, 첫 승리 후보 무기를 확정했다. |
| gstack engineering review | complete (fallback) | gstack 명령 미노출 환경에서 저장소 기반으로 수행. 수량형 인벤토리 분리, 코드 기반 Catalog, 저장 정규화, 공격 보너스 전달 경계를 확정했다. |

## Engineering review

### Recommended file boundaries

| Area | Console | Unity |
|---|---|---|
| Equipment value model | `Models/EquipmentDefinition.cs`, `Models/EquipmentCatalog.cs` | `Core/EquipmentDefinition.cs`, `Core/EquipmentCatalog.cs` |
| Player-owned/equipped state | `Models/Player.cs`, `Models/PlayerSaveData.cs` | `Core/PlayerState.cs` |
| Combat integration | `Systems/CombatSystem.cs` | `Systems/CombatSystem.cs`, `Core/GameManager.cs` |
| Reward and presentation | `Game.cs`, `Util/ConsoleUI.cs` | `Core/GameManager.cs`, `UI/HudController.cs`, `UI/GameActionBridge.cs` |
| Save normalization | `Models/Player.FromSaveData` | `Core/PlayerState.InitDefaults`, `Save/SaveService.cs` |

### Required APIs and invariants

- Expose owned IDs, equipped weapon ID, `GrantEquipment`, `EquipWeapon`, `UnequipWeapon`, and equipped-definition lookup.
- Reject unknown IDs and duplicate grants; never persist an equipped ID that is not owned.
- Make default initialization idempotent so it is safe for a new game and every loaded legacy save.
- Give the candidate reward only when `GrantEquipment` succeeds; include it in the victory log.
- Add the equipped weapon's `AttackBonus` exactly once to each player attack roll.

### Risks addressed before implementation

- Unity's current `InitDefaults` returns whenever item inventory exists, so equipment initialization must be independent of that early return.
- `SaveEnvelope.version` exists but has no migration branch yet. Version 2 must rely on normalization for missing equipment fields and must not reject version-1 saves.
- Existing Unity changes introduce battle-phase and save-error handling. Keep the equipment changes additive and retain those control-flow paths.
