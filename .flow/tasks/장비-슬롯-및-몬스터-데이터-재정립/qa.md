# QA

## Automated checks

| Command | Result | Evidence |
|---|---|---|
| `dotnet build` in `src/ToilRelic` | pass | 0 warnings, 0 errors. |
| `dotnet run --no-build` with new-game equipment inputs | pass | All 12 positions rendered; only `Starter Weapon` was owned/equipped in `PrimaryWeapon`; primary equip completed without allowing removal. |
| Saved JSON inspection | pass | `OwnedEquipmentIds` contains only `starter-weapon`; `EquippedEquipment` contains one `PrimaryWeapon` entry. |
| `git diff --check` | pass | No whitespace errors. |

## Manual scenarios

| Scenario | Expected | Result |
|---|---|---|
| New player | Starter primary weapon only; 11 slots empty. | Pass in console run and saved data. |
| Compatibility and duplication | A definition equips only to its category-compatible slot and cannot occupy two slots. | Pass by shared `CanEquipTo` and duplicate-ID invariant in both player states. |
| Legacy load | Missing equipment fields normalize to the one starter primary weapon without populating optional positions. | Pass by direct normalization-path inspection; old single `EquippedWeaponId` is read only during migration. |
| Combat/stat aggregate | Attack aggregates equipped items; defense and reduction apply before HP loss; max HP clamps after equipment changes. | Pass by direct shared console/Unity logic inspection and console build. |
| Unity Play Mode | Save/load, equipment selection, and hunt round-trip. | Not run: Unity project/editor and scene assets were not present in the workspace. |

## Known limits

- No live armor/accessory definitions or monster drop rates were added by design; enemies only carry the optional future drop-profile ID.
- Unity runtime verification remains required once the Unity project is opened with the configured scene and UI objects.
